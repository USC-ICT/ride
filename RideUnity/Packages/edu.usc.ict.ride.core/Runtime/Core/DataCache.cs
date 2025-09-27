using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VHAssets;

namespace Ride
{
    /// <summary>
    /// DataCache is a lock-free, single-threaded caching system for local files, web URLs, and S3 content.
    /// It supports downloading or copying a file or folder asynchronously and saving it to a local cache for future use.
    /// This can be useful even for local sources (e.g., caching from spindle disk to SSD).
    /// 
    /// The system uses a simple sequential queue: only one file load or copy is active at a time. Requests are queued and processed in order,
    /// with optional priority support. This design avoids concurrency complexity and ensures safe, deterministic behavior on Unity’s main thread.
    /// </summary>
    public class DataCache : RideSystemMonoBehaviour, IDataCache
    {
        enum EncryptionMethod
        {
            NONE,
            AES,
            XOR
        }

        class FileLoadRequest
        {
            public string path;
            public UInt32 crc32c;
            public Action<string, byte []> callback;
            public bool checkCacheFirst;

            public FileLoadRequest(string path, UInt32 crc32c, Action<string, byte []> callback, bool checkCacheFirst) { this.path = path; this.crc32c = crc32c; this.callback = callback; this.checkCacheFirst = checkCacheFirst; }
        }

        ConfigurationSystemUnity m_configSystem;
        AWS.AWSFileStorageS3System m_awsFileStorageS3System;

        EncryptionMethod m_encryptionMethod = EncryptionMethod.AES;

        LinkedList<FileLoadRequest> m_jobQueue = new();

        // File i/O
        CancellationTokenSource m_copyCancelToken;
        Task m_copyTask;

        UnityEngine.Networking.UnityWebRequestAsyncOperation m_webRequest;

        public string CachePath { get; set; }
        public bool LoadInProgress => m_copyTask != null || m_webRequest != null;
        public int LoadQueueSize => m_jobQueue.Count;

        /// <inheritdoc/>
        public override void SystemAwake()
        {
            base.SystemAwake();

            SetDefaultCachePath();
        }

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            m_configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_awsFileStorageS3System = Globals.api.GetSystem<AWS.AWSFileStorageS3System>();
        }

        #region IDataCache

        /// <inheritdoc/>
        public void LoadFile(string path, UInt32 crc32c, Action<string, byte []> callback, bool checkCacheFirst = true) => 
            EnqueueOrLoad(path, crc32c, callback, checkCacheFirst, false);

        /// <inheritdoc/>
        public void LoadFilePriority(string path, UInt32 crc32c, Action<string, byte []> callback, bool checkCacheFirst = true) => 
            EnqueueOrLoad(path, crc32c, callback, checkCacheFirst, true);

        /// <inheritdoc/>
        public void Cancel()
        {
            m_copyCancelToken?.Cancel();
            m_webRequest?.webRequest.Abort();
        }

        /// <inheritdoc/>
        public void CancelAll()
        {
            while (m_jobQueue.Count > 0)
            {
                var queueItem = m_jobQueue.First;
                m_jobQueue.RemoveFirst();
                queueItem.Value.callback("The copy task was cancelled.", null);
            }

            Cancel();
        }

        /// <inheritdoc/>
        public void ClearCache() => ClearCacheInternal(CachePath);

        /// <inheritdoc/>
        public void ClearCache(string sourcePath) => ClearCacheInternal(CreateDestinationPath(sourcePath));

        /// <inheritdoc/>
        public (int, Int64) ComputeCacheSize() => ComputeCacheSizeInternal(CachePath);

        /// <inheritdoc/>
        public (int, Int64) ComputeCacheSize(string sourcePath) => ComputeCacheSizeInternal(CreateDestinationPath(sourcePath));

        #endregion

        public static bool IsError(string error) => !string.IsNullOrEmpty(error);

        public static void ClearDefaultCache() => ClearCacheInternal(GetDefaultCachePath());

        public bool FileExistsInCache(string path, UInt32 crc32c, out byte [] contents)
        {
            contents = null;

            // WebGL doesn't support reading/writing to the filesystem
            if (VHUtils.IsWebGL())
                return false;

            string destinationPath = CreateDestinationPath(path);

            if (!File.Exists(destinationPath))
                return false;

            byte [] encrypted = File.ReadAllBytes(destinationPath);

            // decrypt
            contents = m_encryptionMethod switch
                {
                    EncryptionMethod.NONE => encrypted,
                    EncryptionMethod.AES  => DecryptAES(encrypted),
                    EncryptionMethod.XOR  => DecryptXOR(encrypted),
                    _                     => null
                };

            if (contents == null)
                return false;

            if (crc32c == 0)
            {
                // If no crc was supplied, we can only do filesystem checks to verify if it's the same.
                // otherwise we assume that it is the same.

                if (!IsSourceFromWeb(path) && !IsSourceFromS3(path))
                {
                    try
                    {
                        // If no checksum was provided, verify cache validity by comparing file size and last modified timestamp.
                        // This ensures the cached file matches the original file on disk.
                        var sourceInfo = new FileInfo(path);
                        var destinationInfo = new FileInfo(destinationPath);
                        return sourceInfo.Length == contents.Length &&
                               sourceInfo.LastWriteTimeUtc == destinationInfo.LastWriteTimeUtc;
                    }
                    catch
                    {
                        return false;
                    }
                }

                return true; // can't compare timestamps, assume it's valid
            }

            // Compute the CRC32C checksum of the decrypted contents and compare it to the expected value.
            // This ensures content integrity and detects corruption or mismatch.
            UInt32 actualCrc = Force.Crc32.Crc32CAlgorithm.Compute(contents);
            return actualCrc == crc32c;
        }

        private static string GetDefaultCachePath() => Path.Combine(Application.persistentDataPath, "datacache");

        private void SetDefaultCachePath() => CachePath = GetDefaultCachePath();

        private void EnqueueOrLoad(string path, UInt32 crc32c, Action<string, byte[]> callback, bool checkCacheFirst, bool priority)
        {
            if (LoadInProgress)
            {
                var item = new FileLoadRequest(path, crc32c, callback, checkCacheFirst);
                if (priority)
                    m_jobQueue.AddFirst(item);
                else
                    m_jobQueue.AddLast(item);
            }
            else
            {
                LoadFileInternal(path, crc32c, callback, checkCacheFirst);
            }
        }

        private void LoadFileInternal(string path, UInt32 crc32c, Action<string, byte []> callback, bool checkCacheFirst)
        {
            // Check if already exists in cache
            if (checkCacheFirst)
            {
                if (FileExistsInCache(path, crc32c, out byte[] contents))
                {
                    FinalizeCurrentJob(callback, null, contents);
                    return;
                }
            }

            // Otherwise load from source

            if (IsSourceFromWeb(path))
            {
                // Download via webrequest
                m_webRequest = ReadAndEncryptFileWeb(path, callback);
            }
            else if (IsSourceFromS3(path))
            {
                // Download via aws s3
                ReadAndEncryptFileS3(path, callback);
            }
            else
            {
                // Filesystem load
                m_copyTask = ReadAndEncryptFileAsync(path, callback);
            }
        }

        private async Task ReadAndEncryptFileAsync(string path, Action<string, byte []> callback)
        {
            try
            {
                m_copyCancelToken = new CancellationTokenSource();
                await ReadAndEncryptFileAsync(path, callback, m_copyCancelToken.Token);
            }
            catch (OperationCanceledException ex)
            {
                string error = $"DataCache.ReadAndEncryptFileAsync() - operation cancelled - {ex}";
                Debug.LogWarning(error);
                FinalizeCurrentJob(callback, error, null);
            }
            catch (Exception ex)
            {
                string error = $"DataCache.ReadAndEncryptFileAsync() - operation error - {ex}";
                Debug.LogWarning(error);
                FinalizeCurrentJob(callback, error, null);
            }
        }

        private async Task ReadAndEncryptFileAsync(string path, Action<string, byte []> callback, CancellationToken cancellationToken)
        {
            //Debug.LogFormat("ReadAndEncryptFileAsync() - start - {0}", path);

            const FileOptions fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            const int bufferSize = 81920;  // https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.copytoasync?view=netframework-4.8

            byte[] contents;
            var file = new FileInfo(path);
            string sourceFullName = file.FullName;

            using (var sourceStream = new FileStream(sourceFullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))
            using (var memoryStream = new MemoryStream((int)file.Length))
            {
                //Debug.LogFormat("ReadAndEncryptFileAsync() {0} - {1}", path, file.Name);

                await sourceStream.CopyToAsync(memoryStream, bufferSize, cancellationToken);
                contents = memoryStream.ToArray();
            }

            EncryptAndWriteToCache(path, contents, file.LastWriteTimeUtc);

            //Debug.LogFormat("ReadAndEncryptFileAsync() - done - {0}", path);

            FinalizeCurrentJob(callback, null, contents);
        }

        private UnityWebRequestAsyncOperation ReadAndEncryptFileWeb(string path, Action<string, byte []> callback)
        {
            using var request = new UnityWebRequest(path, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string error = $"ReadAndEncryptFileWeb() - error - {path} - {request.error}";
                    Debug.LogWarning(error);
                    FinalizeCurrentJob(callback, error, null);
                }
                else
                {
                    byte [] contents = request.downloadHandler.data ?? Array.Empty<byte>();

                    EncryptAndWriteToCache(path, contents);

                    //Debug.LogFormat("ReadAndEncryptFileWeb() - done - {0} - {1}", path, destinationPath);

                    FinalizeCurrentJob(callback, null, contents);
                }
            };

            return asyncOp;
        }

        private void ReadAndEncryptFileS3(string path, Action<string, byte []> callback)
        {
            string cognitoIdentityPoolId = m_configSystem.GetTerrainKey();
            string region = m_configSystem.GetTerrainKeyRegion();

            string pathStripped = path.Replace("s3://", "");
            string bucketName = pathStripped.Substring(0, pathStripped.IndexOf("/"));
            string tilePath = pathStripped.Remove(0, pathStripped.IndexOf("/") + 1);

            m_awsFileStorageS3System.m_cognitoIdentityPoolId = cognitoIdentityPoolId;
            m_awsFileStorageS3System.m_regionName = region;
            m_awsFileStorageS3System.GetSignedURL(bucketName, tilePath, (url) =>
            {
                if (string.IsNullOrEmpty(url))
                {
                    string error = $"CopyFileS3() - error - {path} - unable to get url";
                    Debug.LogWarning(error);
                    FinalizeCurrentJob(callback, error, null);
                    return;
                }

                var loadTiles = FindAnyObjectByType<SystemAccessSystem>();
                loadTiles.StartCoroutine(ReadAndEncryptFileS3Internal(path, url, callback));
            });
        }

        private IEnumerator ReadAndEncryptFileS3Internal(string path, string url, Action<string, byte []> callback)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"ReadAndEncryptFileS3Internal() - error - {path} - {request.uri} - {request.error}";
                Debug.LogWarning(error);
                FinalizeCurrentJob(callback, error, null);
                yield break;
            }

            byte[] contents = request.downloadHandler.data ?? Array.Empty<byte>();

            EncryptAndWriteToCache(path, contents);

            //Debug.LogFormat("ReadAndEncryptFileS3Internal() - done - {0} - {1}", path, destinationPath);

            FinalizeCurrentJob(callback, null, contents);
        }

        private void EncryptAndWriteToCache(string sourcePath, byte[] contents, DateTime? sourceTimestamp = null)
        {
            if (VHUtils.IsWebGL())
                return;

            byte[] encrypted = m_encryptionMethod switch
            {
                EncryptionMethod.NONE => contents,
                EncryptionMethod.AES  => EncryptAES(contents),
                EncryptionMethod.XOR  => EncryptXOR(contents),
                _                     => contents
            };

            string destinationPath = CreateDestinationPath(sourcePath);

            // This is inefficient on folders with lots of files, depending on how slow this function is if the folder already exists
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            // This should be done with an async function, but taking a shortcut here assuming writing to disk is not a bottleneck
            File.WriteAllBytes(destinationPath, encrypted);

            // Optionally set file timestamp to match source if provided
            if (sourceTimestamp.HasValue)
                File.SetLastWriteTimeUtc(destinationPath, sourceTimestamp.Value);
        }

        private string CreateDestinationPath(string sourcePath)
        {
            if (CachePath == null)
                SetDefaultCachePath();

            string sanitizedFolder;
            string sanitizedFilename;

            if (IsSourceFromWeb(sourcePath) || IsSourceFromS3(sourcePath))
            {
                // Try to create something unique based on the url, but not something that overruns the path length
                // Also, filter out characters that shouldn't be in paths

                // Use URI segments to extract folder/filename safely
                var uri = new Uri(sourcePath);
                sanitizedFilename = uri.Segments[uri.Segments.Length - 1];
                string folderPart = sourcePath.Replace(sanitizedFilename, "");

                sanitizedFolder = string.Join("_", folderPart.Split(Path.GetInvalidFileNameChars()));
                if (VHUtils.IsOSX())
                    sanitizedFolder = sanitizedFolder.Replace(":", "_");  // apparently on OSX, ':' is a valid path character

                sanitizedFolder = sanitizedFolder.TrimEnd('_');
            }
            else
            {
                // Filter characters unsupported by file system, ref
                // https://stackoverflow.com/questions/146134/how-to-remove-illegal-characters-from-path-and-filenames

                sanitizedFolder = string.Join("_", Path.GetDirectoryName(sourcePath).Split(Path.GetInvalidPathChars()));
                sanitizedFilename = string.Join("_", Path.GetFileName(sourcePath).Split(Path.GetInvalidFileNameChars()));

                // UNC support: remove double leading slashes (\\)
                if (sourcePath.StartsWith(@"\\"))
                    sanitizedFolder = sanitizedFolder.TrimStart('\\');
            }

            string relativePath = Path.Combine(sanitizedFolder, sanitizedFilename);

            relativePath = relativePath.Replace(":", "_"); // catch leftover colon issues

            var destinationPath = Path.Combine(CachePath, relativePath);

            //Debug.LogFormat("CreateDestinationPath() - destinationPath: {0}", destinationPath);

            return destinationPath;
        }

        private void FinalizeCurrentJob(Action<string, byte []> callback, string error, byte [] contents)
        {
            callback(error, contents);

            m_copyTask = null;
            m_copyCancelToken = null;
            m_webRequest = null;

            // Check the queue and start the next copy
            if (m_jobQueue.Count > 0)
            {
                var queueItem = m_jobQueue.First;
                m_jobQueue.RemoveFirst();
                LoadFileInternal(queueItem.Value.path, queueItem.Value.crc32c, queueItem.Value.callback, queueItem.Value.checkCacheFirst);
            }
        }


        private static bool IsSourceFromWeb(string sourcePath)
        {
            if (sourcePath.StartsWith("http:") || sourcePath.StartsWith("https:") || sourcePath.StartsWith("ftp:"))
                return true;

            return false;
        }

        private static bool IsSourceFromS3(string sourcePath)
        {
            if (sourcePath.StartsWith("s3:"))
                return true;

            return false;
        }

        private static void ClearCacheInternal(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        private static (int, Int64) ComputeCacheSizeInternal(string path)
        {
            // Returns <number of files, bytes>

            // TODO - move to RideIO (ComputeDirectorySize()?)

            try
            {
                int fileCount = 0;
                Int64 totalBytes = 0;

                var directoryInfo = new DirectoryInfo(path);
                var files = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    fileCount++;
                    totalBytes += file.Length;
                }

                return (fileCount, totalBytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ComputeCacheSizeInternal() - error reading directory '{path}': {ex}");
                return (0, 0);
            }
        }

        private static readonly byte [] AesKey = new byte[] { 21, 181, 221, 47, 136, 229, 30, 172, 228, 245, 146, 46, 241, 98, 102, 169, 85, 165, 145, 227, 181, 198, 187, 7, 204, 29, 223, 226, 187, 70, 0, 233 };
        private static readonly byte [] AesIV = new byte[] { 131, 115, 211, 120, 128, 141, 38, 235, 45, 121, 197, 75, 232, 4, 195, 220 };

        private static byte [] EncryptAES(byte [] contents)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIV;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(contents, 0, contents.Length);
        }

        private static byte [] DecryptAES(byte [] encrypted)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIV;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        private static byte[] EncryptXOR(byte[] contents) => XorTransform(contents);
        private static byte[] DecryptXOR(byte[] encrypted) => XorTransform(encrypted);

        private const UInt64 XorKey = 3123135234;

        private static byte[] XorTransform(byte[] input)
        {
            var output = new byte[input.Length];

            using var inputStream = new MemoryStream(input);
            using var reader = new BinaryReader(inputStream);
            using var outputStream = new MemoryStream(output);
            using var writer = new BinaryWriter(outputStream);

            while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
                writer.Write(reader.ReadUInt64() ^ XorKey);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
                writer.Write((byte)(reader.ReadByte() ^ (byte)(XorKey & 0xFF)));

            return output;
        }
    }
}

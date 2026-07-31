using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.Audio
{
    /// <inheritdoc cref="IAudioSystem"/>
    public class AudioSystemUnity : RideSystemMonoBehaviour, IAudioSystem
    {
        class RideAudioSource
        {
            public AudioSource source;
        }

        public int m_numAudioSourcesToCreate = 5;

        ResourceLoaderSystem m_resourceLoader;

        Dictionary<RideID, RideAudioSource> m_sources = new Dictionary<RideID, RideAudioSource>();

        GameObject m_genericSourceParent;
        List<RideID> m_genericSources = new List<RideID>();

        public override void SystemInit()
        {
            base.SystemInit();

            m_resourceLoader = Globals.api.GetSystem<ResourceLoaderSystem>();

            m_genericSourceParent = new GameObject("AudioSourceParent");
            m_genericSourceParent.transform.SetParent(this.transform);

            for (int i = 0; i < m_numAudioSourcesToCreate; i++)
            {
                GameObject sourceObj = new GameObject("GenericAudioSource" + i.ToString(), typeof(AudioSource));
                sourceObj.transform.SetParent(m_genericSourceParent.transform);
                RideID genericSourceId = IdentityFactory.CreateId();
                m_genericSources.Add(genericSourceId);
                m_sources.Add(genericSourceId, new RideAudioSource { source = sourceObj.GetComponent<AudioSource>() });
            }

            FindAllAudioSources();
        }

        public void FindAllAudioSources()
        {
            AudioSource[] sources = RideUtils.FindObjectsByType<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                RideAudioSource source = m_sources.Values.FirstOrDefault(s => s.source == sources[i]);
                if (source == null)
                {
                    // we don't already have this one cached, add it
                    m_sources.Add(IdentityFactory.CreateId(), new RideAudioSource { source = sources[i] });
                }
            }
        }

        public override void SystemShutdown()
        {
            base.SystemShutdown();

            GameObject.Destroy(m_genericSourceParent);
        }

        RideID GetAvailableGenericSource()
        {
            for (int i = 0; i < m_genericSources.Count; i++)
            {
                if (!GetSource(m_genericSources[i]).source.isPlaying)
                {
                    return m_genericSources[i];
                }
            }

            RideLog.LogError("There are no available generic audio sources");
            return RideID.Null;
        }

        RideAudioSource GetSource(RideID source)
        {
            if (m_sources.ContainsKey(source))
            {
                return m_sources[source];
            }
            RideLog.LogError($"Failed to find source {source}");
            return null;
        }

        public RideID Play(string clip)
        {
            RideID source = GetAvailableGenericSource();
            return Play(source, clip);
        }

        public RideID Play(RideID source, string clip)
        {
            return PlayInternal(source, clip);
        }

        RideID PlayInternal(RideID source, string clip)
        {
            AudioClip c = m_resourceLoader.GetAudioClip(clip);
            if (c != null)
            {
                RideAudioSource src = GetSource(source);
                src?.source.PlayOneShot(c, 1);
            }
            return source;
        }

        public void PlayAtPosition(string clip, RideVector3 pos)
        {
            AudioClip c = m_resourceLoader.GetAudioClip(clip);
            if (c != null)
            {
                AudioSource.PlayClipAtPoint(c, pos);
            }
        }

        public void Stop(RideID source)
        {
            RideAudioSource src = GetSource(source);
            src?.source.Stop();
        }

        public bool IsPlaying(RideID source)
        {
            RideAudioSource src = GetSource(source);
            if (src != null)
            {
                return src.source.isPlaying;
            }
            else {
                RideLog.LogError($"Failed to find source {source}");
                return false;
            };
        }

        public void LoadAudioFile(string pathOrUrl, Action<AudioClip> onComplete)
        {
            StartCoroutine(LoadAudioFileCoroutine(pathOrUrl, onComplete));
        }

        /// <summary>
        /// Loads an audio file from either a local file path or a remote URL and returns an <see cref="AudioClip"/> via callback.
        /// </summary>
        /// <param name="pathOrUrl">
        /// The path to the local audio file (e.g., C:/path/to/file.mp3) or a remote HTTP/HTTPS URL.
        /// Supported extensions include .mp3, .ogg, and .wav.
        /// For local files, <c>file://</c> is automatically prepended on platforms that require it.
        /// </param>
        /// <param name="onComplete">
        /// Callback invoked when the operation completes. If successful, the resulting <see cref="AudioClip"/> is passed; otherwise, <c>null</c> is passed on failure.
        /// </param>
        /// <remarks>
        /// This coroutine-compatible version is used for WebGL and other Unity platforms that do not support <c>async/await</c>.
        /// Local file access is supported on platforms that allow file I/O (e.g., Windows, macOS, Android, iOS).
        /// WebGL builds can only load audio from remote URLs.
        /// </remarks>
        /// <example>
        /// <code>
        /// StartCoroutine(LoadAudioFileCoroutine("https://example.com/audio.mp3", (clip) =>
        /// {
        ///     if (clip != null)
        ///     {
        ///         audioSource.clip = clip;
        ///         audioSource.Play();
        ///     }
        /// }));
        /// </code>
        /// </example>
        public IEnumerator LoadAudioFileCoroutine(string pathOrUrl, Action<AudioClip> onComplete)
        {
            if (string.IsNullOrEmpty(pathOrUrl))
            {
                Debug.LogError("LoadAudioFile() - pathOrUrl is null or empty");
                onComplete?.Invoke(null);
                yield break;
            }

            if (TryGetAudioDataUriAudioType(pathOrUrl, out AudioType dataUriAudioType))
            {
                if (dataUriAudioType == AudioType.WAV)
                {
                    onComplete?.Invoke(LoadWavDataUri(pathOrUrl));
                    yield break;
                }

                using (var dataUriRequest = UnityWebRequestMultimedia.GetAudioClip(pathOrUrl, dataUriAudioType))
                {
                    yield return dataUriRequest.SendWebRequest();

                    if (dataUriRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to load audio data URI: {dataUriRequest.error}");
                        onComplete?.Invoke(null);
                        yield break;
                    }

                    onComplete?.Invoke(DownloadHandlerAudioClip.GetContent(dataUriRequest));
                    yield break;
                }
            }

            var audioType = GetAudioTypeFromPath(pathOrUrl);
            string finalUrl;

            if (pathOrUrl.StartsWith("http://") ||
                pathOrUrl.StartsWith("https://") ||
                pathOrUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            {
                finalUrl = pathOrUrl;  // Remote or browser-managed URL
            }
            else
            {
                // local file path
                if (!File.Exists(pathOrUrl))
                {
                    Debug.LogError($"Audio file not found: {pathOrUrl}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                // Normalize path and prepend file://
                string normalizedPath = pathOrUrl.Replace("\\", "/");
                finalUrl = normalizedPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                        ? normalizedPath
                        : "file://" + normalizedPath;
            }

            var downloadHandler = new DownloadHandlerAudioClip(finalUrl, audioType)
            {
                compressed = true
            };

            using (var www = new UnityWebRequest(finalUrl, UnityWebRequest.kHttpVerbGET, downloadHandler, null))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load audio: {www.error}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);
                onComplete?.Invoke(clip);
            }
        }

        private static AudioType GetAudioTypeFromPath(string pathOrUrl)
        {
            string extension = Path.GetExtension(pathOrUrl).ToLowerInvariant();
            switch (extension)
            {
                case ".mp3": return AudioType.MPEG;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".wav": return AudioType.WAV;
                default: return AudioType.WAV;
            }
        }

        private static bool TryGetAudioDataUriAudioType(string pathOrUrl, out AudioType audioType)
        {
            const string dataUriPrefix = "data:audio/";
            const string base64Marker = ";base64,";

            audioType = AudioType.UNKNOWN;

            if (string.IsNullOrEmpty(pathOrUrl) ||
                !pathOrUrl.StartsWith(dataUriPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int mimeStart = dataUriPrefix.Length;
            int mimeEnd = pathOrUrl.IndexOf(base64Marker, mimeStart, StringComparison.OrdinalIgnoreCase);
            if (mimeEnd < 0)
            {
                return false;
            }

            string mimeSubtype = pathOrUrl.Substring(mimeStart, mimeEnd - mimeStart);
            switch (mimeSubtype.ToLowerInvariant())
            {
                case "wav":
                case "x-wav":
                case "wave":
                    audioType = AudioType.WAV;
                    return true;
                case "mpeg":
                case "mp3":
                    audioType = AudioType.MPEG;
                    return true;
                case "ogg":
                case "oggvorbis":
                    audioType = AudioType.OGGVORBIS;
                    return true;
                default:
                    Debug.LogError($"Unsupported audio data URI type: {mimeSubtype}");
                    return false;
            }
        }

        private static AudioClip LoadWavDataUri(string dataUri)
        {
            try
            {
                const string prefix = "data:audio/wav;base64,";
                byte[] wavBytes = Convert.FromBase64String(dataUri.Substring(prefix.Length));
                return CreatePcm16WavAudioClip(wavBytes, "DataUriWav");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to load WAV data URI: {exception.Message}");
                return null;
            }
        }

        private static AudioClip CreatePcm16WavAudioClip(byte[] wavBytes, string clipName)
        {
            if (wavBytes == null || wavBytes.Length < 44)
                throw new InvalidDataException("WAV data is too short.");

            int channels = BitConverter.ToInt16(wavBytes, 22);
            int sampleRate = BitConverter.ToInt32(wavBytes, 24);
            short bitsPerSample = BitConverter.ToInt16(wavBytes, 34);

            if (channels <= 0 || sampleRate <= 0 || bitsPerSample != 16)
                throw new InvalidDataException($"Unsupported WAV format. channels={channels}, sampleRate={sampleRate}, bitsPerSample={bitsPerSample}");

            int dataOffset = FindWavDataChunkOffset(wavBytes, out int dataSize);
            int sampleCount = dataSize / 2;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(wavBytes, dataOffset + i * 2);
                samples[i] = sample / 32768f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static int FindWavDataChunkOffset(byte[] wavBytes, out int dataSize)
        {
            int offset = 12;
            while (offset + 8 <= wavBytes.Length)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavBytes, offset, 4);
                int chunkSize = BitConverter.ToInt32(wavBytes, offset + 4);
                int dataOffset = offset + 8;

                if (string.Equals(chunkId, "data", StringComparison.Ordinal))
                {
                    dataSize = Math.Min(chunkSize, wavBytes.Length - dataOffset);
                    return dataOffset;
                }

                offset = dataOffset + chunkSize + (chunkSize % 2);
            }

            throw new InvalidDataException("WAV data chunk was not found.");
        }
    }
}

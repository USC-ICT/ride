using System;

namespace Ride
{
    /// <summary>
    /// Provides an interface for a system that handles asynchronous file caching and retrieval.
    /// This system supports loading files from local paths, web URLs, or S3 sources, storing
    /// encrypted copies in a persistent local cache, and managing a single active transfer queue.
    /// 
    /// Only one file operation may run at a time. Additional requests are queued and processed in order.
    /// </summary>
    public interface IDataCache : IRideSystem
    {
        /// <summary>
        /// Queues a request to load a file from a local path, web, or S3 location. The file will be copied to a local cache if necessary.
        /// If a file is already being loaded, this request will be enqueued to run afterward.
        /// </summary>
        /// <param name="path">The source path or URL to load from. May be a local path, http(s), or s3 URI.</param>
        /// <param name="crc32c">The CRC32C checksum of the file for validation. If zero, timestamp/size comparison is used.</param>
        /// <param name="callback">The callback to invoke when the file is available. The first parameter is error (null on success), the second is the file contents.</param>
        /// <param name="checkCacheFirst">If true, the cache will be checked before triggering a new copy.</param>
        void LoadFile(string path, UInt32 crc32c, Action<string, byte[]> callback, bool checkCacheFirst = true);

        /// <summary>
        /// Same as <see cref="LoadFile"/>, but this request will be inserted at the front of the queue to run before other pending requests.
        /// </summary>
        /// <param name="path">The source path or URL to load from. May be a local path, http(s), or s3 URI.</param>
        /// <param name="crc32c">The CRC32C checksum of the file for validation. If zero, timestamp/size comparison is used.</param>
        /// <param name="callback">The callback to invoke when the file is available. The first parameter is error (null on success), the second is the file contents.</param>
        /// <param name="checkCacheFirst">If true, the cache will be checked before triggering a new copy.</param>
        void LoadFilePriority(string path, UInt32 crc32c, Action<string, byte[]> callback, bool checkCacheFirst = true);

        /// <summary>
        /// Cancels the currently executing file copy operation, if any.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Cancels the currently executing operation and clears the pending job queue.
        /// All queued callbacks will be invoked with an error message and null contents.
        /// </summary>
        void CancelAll();

        /// <summary>
        /// Deletes all cached files in the default cache path.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Deletes the cached version of a specific source file, if present.
        /// </summary>
        /// <param name="sourcePath">The original source path or URL of the file to remove from cache.</param>
        void ClearCache(string sourcePath);

        /// <summary>
        /// Computes the total number of files and bytes currently stored in the default cache.
        /// </summary>
        /// <returns>A tuple representing the number of files and total size in bytes.</returns>
        (int, Int64) ComputeCacheSize();

        /// <summary>
        /// Computes the total number of files and bytes currently stored in the cache path for a specific source file.
        /// </summary>
        /// <param name="sourcePath">The source path or URL of the file whose cache area should be analyzed.</param>
        /// <returns>A tuple representing the number of files and total size in bytes.</returns>
        (int, Int64) ComputeCacheSize(string sourcePath);
    }
}

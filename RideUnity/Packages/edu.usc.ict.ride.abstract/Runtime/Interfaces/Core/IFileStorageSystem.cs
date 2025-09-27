using System;

namespace Ride
{
    /// <summary>
    /// Interface for loading, saving, and managing persistent file data.
    /// This abstraction allows for different implementations such as local disk, AWS S3, or other cloud storage backends.
    /// It provides both absolute path methods and "preferred path" helpers using a configured base prefix.
    /// </summary>
    public interface IFileStorageSystem : IRideSystem
    {
        /// <summary>
        /// A base directory or prefix used when the caller provides only a filename.
        /// Implementations should resolve this into a full storage path (e.g., "ride/terrain/").
        /// </summary>
        string preferredPath { get; set; }

        /// <summary>
        /// Loads the content of a file at the specified full path.
        /// </summary>
        /// <param name="path">The full path to the file in storage.</param>
        /// <param name="onComplete">Callback invoked with the result of the load operation.</param>
        void Load(string path, Action<StorageLoadResponse> onComplete);

        /// <summary>
        /// Loads the content of a file located under the configured <see cref="preferredPath"/>.
        /// </summary>
        /// <param name="filename">The file name (with extension) relative to the preferred path.</param>
        /// <param name="onComplete">Callback invoked with the result of the load operation.</param>
        void LoadUsingPreferredPath(string filename, Action<StorageLoadResponse> onComplete);

        /// <summary>
        /// Saves data to the specified full path.
        /// </summary>
        /// <param name="path">The full destination path in storage.</param>
        /// <param name="data">The data to save.</param>
        /// <param name="onComplete">Callback invoked upon completion of the save operation.</param>
        void Save(string path, string data, Action<StorageSaveResponse> onComplete);

        /// <summary>
        /// Saves data under the configured <see cref="preferredPath"/>.
        /// </summary>
        /// <param name="filename">The file name (with extension) relative to the preferred path.</param>
        /// <param name="data">The data to save.</param>
        /// <param name="onComplete">Callback invoked upon completion of the save operation.</param>
        void SaveUsingPreferredPath(string filename, string data, Action<StorageSaveResponse> onComplete);

        /// <summary>
        /// Copies a file from one location to another. Intended for local file paths only.
        /// </summary>
        /// <param name="src">The source file path.</param>
        /// <param name="dst">The destination file path.</param>
        /// <param name="onComplete">Callback invoked upon completion of the copy operation.</param>
        void Copy(string src, string dst, Action<StorageSaveResponse> onComplete);

        /// <summary>
        /// Retrieves a signed URL for temporary access to a file located at the given full path.
        /// </summary>
        /// <param name="path">The full path to the file in storage.</param>
        /// <param name="filename">The filename portion of the request (used for display or naming).</param>
        /// <param name="onComplete">
        /// Callback invoked with a time-limited signed URL.
        /// If the file cannot be accessed or signed, the callback receives an error instead.
        /// </param>
        void GetSignedURL(string path, string filename, Action<string> onComplete);

        /// <summary>
        /// Retrieves a signed URL for temporary access to a file located under the configured <see cref="preferredPath"/>.
        /// </summary>
        /// <param name="filename">The file name (with extension) relative to the preferred path.</param>
        /// <param name="onComplete">
        /// Callback invoked with a time-limited signed URL.
        /// If the file cannot be accessed or signed, the callback receives an error instead.
        /// </param>
        void GetSignedURLUsingPreferredPath(string filename, Action<string> onComplete);
    }
}

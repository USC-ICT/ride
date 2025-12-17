using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Represents an asynchronous operation for loading assets, exposing progress reporting,
    /// completion notification, and error handling in a task-like but coroutine-compatible form.
    /// </summary>
    /// <typeparam name="T">The type of result the operation will produce upon successful completion.</typeparam>
    public class LoadOperation<T>
    {
        /// <summary>Event invoked when the operation completes successfully with a result of type <typeparamref name="T"/>.</summary>
        public event Action<T> Completed;

        /// <summary>Event invoked if the operation fails, passing a descriptive error message.</summary>
        public event Action<string> Failed;

        /// <summary>
        /// Gets the current progress of the operation, clamped between 0 and 1.
        /// A value of 1.0 indicates completion.
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>Indicates whether the operation has been manually cancelled.</summary>
        public bool IsCancelled { get; private set; }

        /// <summary>Indicates whether the operation has completed (either successfully or with failure).</summary>
        public bool IsCompleted { get; private set; }

        public T Result { get; private set; }
        public string FailureMessage { get; private set; }

        /// <summary>
        /// Sets the current progress of the operation.
        /// This value is clamped between 0 and 1.
        /// </summary>
        /// <param name="value">A float representing current progress (0.0 to 1.0).</param>
        public void SetProgress(float value)
        {
            Progress = RideMath.Clamp01(value);
        }

        /// <summary>
        /// Marks the operation as successfully completed and invokes the <see cref="Completed"/> event.
        /// </summary>
        /// <param name="result">The result of the operation.</param>
        public void SetCompleted(T result)
        {
            if (IsCancelled || IsCompleted) return;
            IsCompleted = true;
            Progress = 1f;
            Result = result;
            Completed?.Invoke(result);
        }

        /// <summary>
        /// Marks the operation as failed and invokes the <see cref="Failed"/> event with an error message.
        /// </summary>
        /// <param name="error">A message describing the reason for failure.</param>
        public void SetFailed(string error)
        {
            if (IsCancelled || IsCompleted) return;
            IsCompleted = true;
            FailureMessage = error;
            Failed?.Invoke(error);
        }

        /// <summary>Cancels the operation and marks it as failed with a "Cancelled" message.</summary>
        public void Cancel()
        {
            if (IsCompleted) return;
            IsCancelled = true;
            SetFailed("Cancelled");
        }

        /// <summary>
        /// Registers a callback to be invoked when the operation completes successfully.
        /// </summary>
        /// <param name="onComplete">An action to execute with the result.</param>
        /// <returns>The current operation for chaining.</returns>
        public LoadOperation<T> Then(Action<T> onComplete)
        {
            if (IsCompleted && !IsCancelled)
                onComplete?.Invoke(Result);
            else
                Completed += onComplete;

            return this;
        }

        /// <summary>
        /// Registers a callback to be invoked when the operation fails.
        /// </summary>
        /// <param name="onFail">An action to execute with the error message.</param>
        /// <returns>The current operation for chaining.</returns>
        public LoadOperation<T> Catch(Action<string> onFail)
        {
            if (IsCompleted && !string.IsNullOrEmpty(FailureMessage))
                onFail?.Invoke(FailureMessage);
            else
                Failed += onFail;

            return this;
        }
    }

    /// <summary>
    /// Interface for an asset loading system capable of loading catalog-based asset bundles.
    /// Provides methods for loading assets by name or label, inspecting catalog entries, and managing loaded assets/cache.
    /// </summary>
    public interface IAssetLoadingSystem
    {
        /// <summary>
        /// Loads an asset catalog from the given <see cref="CatalogLoadInfo"/>.
        /// </summary>
        /// <param name="catalog">The catalog info containing path or asset reference.</param>
        /// <returns>A task that completes with true if the catalog was loaded successfully.</returns>
        LoadOperation<bool> LoadCatalog(CatalogLoadInfo catalog);

        /// <summary>
        /// Loads an asset from any loaded catalog using its unique name.
        /// </summary>
        /// <param name="assetName">The name of the asset to load. Only one object of any given name is allowed per catalog.</param>
        /// <returns>A task that resolves to the loaded asset, or null if not found.</returns>
        LoadOperation<object> LoadAssetByName(string assetName);

        /// <summary>
        /// Loads any asset from any loaded catalog that matches the provided list of labels.
        /// </summary>
        /// <param name="labels">A list of labels to match against.</param>
        /// <returns>A task that resolves to the first matching asset, or null if none match.</returns>
        LoadOperation<object> LoadAnyAssetByLabels(List<string> labels);

        /// <summary>
        /// Gets a list of all catalog entry names and their associated labels.
        /// </summary>
        /// <returns>A list of tuples, each containing the asset name and a list of its labels.</returns>
        List<(string assetName, List<string> labels)> GetEntryInfoList();

        /// <summary>
        /// Gets the total number of asset entries currently loaded from all catalogs.
        /// </summary>
        /// <returns>The number of entries.</returns>
        int GetEntryCount();
    }
}

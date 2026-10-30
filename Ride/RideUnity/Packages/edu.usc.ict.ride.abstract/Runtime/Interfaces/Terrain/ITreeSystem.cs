using System.Collections;
using System.Collections.Generic;

namespace Ride.Terrain
{
    /// <summary>Provides catalog-based tree selection and REST-request display services for terrain tooling.</summary>
    /// <remarks>The current implementation is used by internal tree REST examples to select a tree asset from descriptive search parameters.</remarks>
    public interface ITreeSystem : IRideSystem
    {
        /// <summary>Starts an asynchronous search for the catalog tree that best matches the supplied attributes.</summary>
        /// <param name="searchParamDict">Search attributes such as color, height, width, and tree shape.</param>
        void GetBestTree(Dictionary<string, string> searchParamDict);

        /// <summary>Displays the REST request URL used by the tree service.</summary>
        /// <param name="url">The URL to display.</param>
        void DisplayString(string url);
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    /// <summary>
    /// Interface that provides the ability to select an arbitrary type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISelector<T> : IRideSystem
    {
        /// <summary>
        /// Returns true if the selector is currently doing selecting
        /// </summary>
        bool isSelecting { get; }

        /// <summary>
        /// The input used to start and stop selecting
        /// </summary>
        bool isFinishedSelecting { get; }

        /// <summary>
        /// Allows the use of overriding the main Camera for selecting functionality
        /// </summary>
        Camera overrideCamera { get; set; }

        /// <summary>
        /// Select T's
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> PerformSelection();

        /// <summary>
        /// Selects T's using the givne point
        /// </summary>
        /// <param name="selectionPoint"></param>
        /// <returns></returns>
        IEnumerable<T> PerformSelection(RideVector3 selectionPoint);

        /// <summary>
        /// Selects T's in the given 2d selectionArea
        /// </summary>
        /// <param name="selectionArea"></param>
        /// <returns>T's in the given selection area</returns>
        IEnumerable<T> PerformSelection(Rect selectionArea);

        void SelectEntities(RideID[] entities);

        bool enabled { get; set; }
    }
}

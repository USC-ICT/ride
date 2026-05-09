using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    [Flags]
    public enum ViewSystemConfigFlags
    {
        None = 0,
        Unit_Selection = 1 << 0,

        All = Unit_Selection
    }

    [Serializable]
    public class ViewSystemParams
    {
        public ViewSystemConfigFlags flags = ViewSystemConfigFlags.All;
    }

    /// <summary>
    /// Interface for accessing and manipulating the UI
    /// </summary>
    public interface IViewSystem : IRideSystem
    {
        /// <summary>
        /// Returns all selected agents
        /// </summary>
        IEnumerable<RideID> selectedEntities { get; }

        /// <summary>
        /// The configuration for how this system functions
        /// </summary>
        ViewSystemParams config { get; set; }

        /// <summary>
        /// The View System Menu for showing built-in functionality
        /// </summary>
        IViewSystemMenu viewSystemMenu { get; set; }

        /// <summary>
        /// Returns the ISelector
        /// </summary>
        ISelector<RideID> entitySelector { get; set; }
    }
}

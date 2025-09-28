using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    [Flags]
    public enum ViewSystemConfigFlags
    {
        None,
        Unit_Selection = 1,

        Last,
        All = (Last << 1) - 1
    }

    public class ViewSystemParams
    {
        public ViewSystemConfigFlags flags;

        public static readonly ViewSystemParams Default = new ViewSystemParams()
        {
            flags = ViewSystemConfigFlags.All
        };
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
        /// Returns the deprecated ISelector
        /// </summary>
        ISelector<RideID> agentSelector { get; set; }

        /// <summary>
        /// Returns the ISelector
        /// </summary>
        ISelector<RideID> entitySelector { get; set; }
    }
}

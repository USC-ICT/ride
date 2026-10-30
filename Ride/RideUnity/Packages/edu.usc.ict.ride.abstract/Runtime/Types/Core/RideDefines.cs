using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Defines constants that are used in multiple systems across Ride
    /// </summary>
    public static class RideDefines
    {
        // Military Unit Member Names
        /// <summary>The standard fire-team group name.</summary>
        public const string FireTeam = "FireTeam";
        /// <summary>The standard team-leader role name.</summary>
        public const string TeamLeader = "Team Leader";
        /// <summary>The standard squad-leader role name.</summary>
        public const string SquadLeader = "Team Leader";
        /// <summary>The standard rifleman role name.</summary>
        public const string Rifleman = "Rifleman";
        /// <summary>The standard grenadier role name.</summary>
        public const string Grenadier = "Grenadier";
        /// <summary>The standard automatic-rifleman role name.</summary>
        public const string AutomaticRifleman = "Automatic Rifleman";
        /// <summary>The standard platoon-leader role name.</summary>
        public const string PlatoonLeader = "Platoon Leader";
        /// <summary>The standard company-commander role name.</summary>
        public const string CompanyCommander = "Company Commander";
        /// <summary>The standard company executive-officer role name.</summary>
        public const string CompanyXO = "Company XO";

        // Properties
        /// <summary>The stored preference key for inverted mouse input.</summary>
        public const string InvertedMousePref = "InvertedMousePref";
        /// <summary>The shared property name used for network latency.</summary>
        public const string Ping = "Ping";
    }
}

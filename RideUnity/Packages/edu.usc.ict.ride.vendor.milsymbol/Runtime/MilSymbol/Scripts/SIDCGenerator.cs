using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace edu.usc.ict.milsymbol
{

    public enum Context { REALITY = 0, EXERCISE = 1, SIMULATION = 2 };

    public enum StandardIdentity { PENDING = 0, UNKNOWN = 1, ASSUMED_FRIEND = 2, FRIEND = 3, NEUTRAL = 4, SUSPECT = 5, HOSTILE = 6};

    /***
     * A.5.3.3 Symbol Set (ref Table A-III, pg 48).
     ***/
    public enum SymbolSet
    {
        UNKNOWN = 0,
        AIR = 1,
        AIR_MISSILE = 2,
        SPACE = 5,
        SPACE_MISSILE = 6,
        LAND_UNIT = 10,
        LAND_CIVILIAN_UNIT = 11,
        LAND_EQUIPMENT = 15,
        LAND_INSTALLATION = 20,
        CONTROL_MEASURE = 25,
        SEA_SURFACE = 30,
        SEA_SUBSURFACE = 35,
        MINE_WARFARE = 36,
        ACTIVITIES = 40,
        ATMOSPHERIC = 45,
        OCEANOGRAPHIC = 46,
        METEOROLOGICAL_SPACE = 47,
        SIGINT_SPACE = 50,
        SIGINT_AIR = 42,
        SIGINT_SURFACE = 53,
        SIGING_SUBSURF = 54,
        CYBERSPACE = 60,
        VERSION_EXTENSION = 99
    }

    /***
     * Ref A.5.3.4 Status, Table A-IV, pg 48
     ***/
    public enum Status
    {
        PRESENT = 0,
        PLANNED = 1,
        PRESENT_FULLY_CAPABLE = 2,
        PRESENT_DAMAGED = 3,
        PRESENT_DESTROYED = 4,
        PRESENT_FULL = 5,
        VERSION_EXTENSION = 9
    }

    public enum HQ
    {
        UNKNOWN = 0,
        FEINT_DUMMY = 1,
        HEADQUARTERS = 2,
        FEINT_DUMMY_HEADQUARTERS = 3,
        TASK_FORCE = 4,
        FEINT_DUMMY_TASK_FORCE = 5,
        TASK_FORCE_HEADQUARTERS = 6,
        FEINT_DUMMY_TASK_FORCE_HEADQUARTERS = 7,
        VERSION_EXTENSION = 9
    }

    public enum AMPLIFIER
    {
        UNKNOWN = 00,
        TEAM_CREW = 11,
        SQUAD = 12,
        SECTION = 13,
        PLATOON = 14,
        COMPANY = 15,
        BATTALION = 16,
        REGIMENT = 17,
        BRIGADE = 18,
        DIVISION = 21,
        CORPS = 22,
        ARMY = 23,
        ARMY_GROUP = 24,
        REGION = 25,
        COMMAND = 26,
        WHEELED_LIMITED_XC = 31,
        WHEELED_XC = 32,
        TRACKED = 33,
        WHEELED_AND_TRACKED = 34,
        TOWED = 35,
        RAIL = 36,
        PACK_ANIMALS = 37,
        SNOW_PRIME_MOVER = 41,
        SLED = 42,
        BARGE = 51,
        AMPHIBIOUS = 52
    }

    public class SIDCGenerator : MonoBehaviour
    {

        /***
         * As per MIL-2525D pg 47
         * http://www.dtic.mil/doctrine/doctrine/other/ms_2525D.pdf
         * 
         * SIDC (Symbol IDentification Code)  
         * A.5.2.1 Set A - First ten digits.
         *   Version              (2 digits) 
         *   Standard identity    (2 digits)  
         *   Symbol set           (2 digits)
         *   Status               (1 digit)
         *   HQ/Task Force/Dummy  (1 digit)
         *   Amplifier/Descriptor (2 digits)
         *   
         * A.5.2.2 Set B - Second ten digits.
         *   Entity               (2 digits)
         *   Entity type          (2 digits)          
         *   Entity subtype       (2 digits)
         *   Sector 1 modifier    (2 digits)
         *   Sector 2 modifier    (2 digits)
         * 
         * Version  is always 10 (11-39 reserved)
         * Standard Identity first digit
         *   0 = reality
         *   1 = exercise
         *   2 = simulation
         * 
         * Standard Identity second digit
         *   0 = pending
         *   1 = Unknown
         *   2 = Assumed Friend
         *   3 = Friend
         *   4 = Assumed Friend
         *   5 = Suspect/Joker
         *   6 = Hostile/Faker
         *   
         * 
         ***/

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

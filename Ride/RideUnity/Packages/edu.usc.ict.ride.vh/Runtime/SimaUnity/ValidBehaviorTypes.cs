using System.Collections.Generic;

/// <summary>
/// This class defines valid behaviors and the mapping from gesture types to the corresponding animations
/// </summary>
public static class ValidBehaviorTypes
{
    // Maps GestureType enum (as string) to animation names
    public static readonly Dictionary<string, string> ValidGestureTypes = new()
    {
        { "Besides", "IdleStandingUpright01_BesidesLf01" },
        { "Approximation", "IdleStandingUpright01_Approximation01" },
        { "Negation", "IdleStandingUpright01_NegativeLf01" },
        { "Offer", "IdleStandingUpright01_OfferRt01" },
        { "Include", "IdleStandingUpright01_InclusivityPosBt01" }, 
        { "Cycle", "IdleStandingUpright01_ProcessEvolve01" },
        { "Container_big", "IdleStandingUpright01_SurroundBt01" },
        { "Container_small", "IdleStandingUpright01_SurroundBt02" },
        { "However", "IdleStandingUpright01_HoweverLf01" },
        { "You", "IdleStandingUpright01_YouLf01" },
        { "Me", "ChrGenericMleAdult@IdleStandingUpright01_MeLf01" },
        { "Beat_high", "IdleStandingUpright01_BeatHighLf01" },
        { "Beat_mid", "IdleStandingUpright01_BeatMidLf01" },
        { "Beat_low", "IdleStandingUpright01_BeatLowLf01" },
        { "Stop", "IdleStandingUpright01_StopRt01" },
        { "Greeting", "IdleStandingUpright01_Greeting01" }
    };

    // Valid HeadType enum values (already checked via enum)
    public static readonly HashSet<string> ValidHeadTypes = new()
    {
        "Nod", "Shake", "Toss"
    };

    // Valid Facial Action Units (already defined in FacialAU enum)
    public static readonly HashSet<string> ValidFacsTypes = new()
    {
        "au1", "au2", "au5", "au6", "au7"
    };
}

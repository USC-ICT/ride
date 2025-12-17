namespace Ride
{
/// <summary>
/// Enumerations describing supported nonverbal behavior categories and subtypes.
/// It defines possible allowed values for BehaviorKind, GestureType, HeadType, FacialAU
/// These enums are used by <c>Behavior</c> to strongly-type the behaviors.
/// </summary>

/// <summary>
/// This enum restricts the nonverbal behaviors to gestures, headmovements, and facial action units
/// </summary>
public enum BehaviorKind
{
    Gesture,
    Head,
    Facial
}

/// <summary>
/// This enum identifies supported gestures types. This current list is handpicked by Parisa. 
/// New gestures should be added here. Names are similar to the keys in <c>ValidBehaviorTypes.ValidGestureTypes</c>.
/// </summary>
public enum GestureType
{
    Besides,
    Approximation,
    Negation,
    Offer,
    Include,
    Cycle,
    Container_big,
    Container_small,
    However,
    You,
    Me,
    Beat_high,
    Beat_mid,
    Beat_low,
    Stop,
    Greeting
}

/// <summary>
/// This enum identifies supported head movement types. 
/// New head movements should be added here. Names are similar to the keys in <c>ValidBehaviorTypes.ValidHeadTypes</c>.
/// </summary>
public enum HeadType
{
    Nod,
    Shake,
    Toss
}

/// This enum identifies supported facial action units types. 
/// New facial action units should be added here. Names are similar to the keys in <c>ValidBehaviorTypes.ValidFacsTypes</c>.
/// </summary>
public enum FacialAU
{
    au1,
    au2,
    au5,
    au6,
    au7
}
}

namespace Ride
{
/// <summary>
/// Represents a nonverbal behavior within an utterance, including its category (kind)
/// its associated phrase in the utterance (phrase), and its timing marker (marker)
/// A behavior can be either a gesture, head movement, or facial action unit.
/// </summary>
public class Behavior
{
    /// <summary>
    /// The general category of the behavior (possible categories: gesture, head movement, facial action unit).
    /// </summary>
    public BehaviorKind Kind { get; set; }

    /// <summary>
    /// The segment of the utterance where the behavior occurs.
    /// </summary>
    public string Phrase { get; set; }
    
    /// <summary>
    /// The timing marker(the wrod itself) of the behavior, such as the start, stroke, or relax point (depending on the prompt)
    /// </summary> 
    public string Marker { get; set; }

    /// <summary>
    /// The timing marker(the index of the word, e.g., T5) of the behavior, such as the start, stroke, or relax point (depending on the prompt)
    /// </summary> 
    public string TimingMarker { get; set; }

    /// <summary>
    /// The gesture type, if the behavior is a gesture; otherwise, null.
    /// </summary>
    public GestureType? Gesture { get; set; }

    /// <summary>
    /// The head movement type, if the behavior is a head movement; otherwise, null.
    /// </summary>
    public HeadType? Head { get; set; }

    /// <summary>
    /// The facial action unit (AU), if the behavior is a facial expression; otherwise, null.
    /// </summary>
    public FacialAU? Facial { get; set; }
}
}

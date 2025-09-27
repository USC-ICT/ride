using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Interface for Interpretation System.
    /// Implementations of the Interpretation System provide the following interrelated functionality:<br/>
    /// 1. pattern matching for World Events, enabling Observation Scenario Conditions<br/>
    /// 2. pattern matching for interpretations of World Events, enabling Interpretation Scenario Conditions<br/>
    /// 3. generation of textual narratives corresponding to the interpretation of World Events
    /// </summary>
    public interface IInterpretationSystem : IRideSystem
    {
        /// <summary>
        /// Determine if a given pattern, described as a string, matches the representation of any observed World Event
        /// </summary>
        /// <param name="pattern">A string representation of a pattern, e.g. "(behaviourWander' ?e ?b ?a)"</param>
        /// <returns>A dictionary that maps variables in the pattern to RideID values in a matching event, or null when there is no match</returns>
        Dictionary<string,RideID> CheckObservationPattern(string pattern);

        /// <summary>
        /// Add a new interpreter to the interpretation system
        /// </summary>
        /// <param name="knowledgebase">Text of the knowledgebase axioms to be used by this interpreter</param>
        /// <param name="focus">An array of RideIDs that specify the focus of this new interpreter, e.g. groups of interest</param>
        /// <returns>The RideID of the new interpreter</returns>
        RideID AddInterpreter(string knowledgebase, RideID[] focus);

        /// <summary>
        /// Get all of the interpreters in this interpretation system, identified by thier RideID
        /// </summary>
        /// <returns></returns>
        IEnumerable<RideID> GetInterpreters();

        /// <summary>
        /// Determine if a given pattern, described as a string, matches the representation of the current interpretation of a given interpreter
        /// </summary>
        /// <param name="interpreter">The RideID of the interpreter to use</param>
        /// <param name="pattern">A string representation of a pattern, e.g. "(behaviourAmbush' ?e ?b ?g)"</param>
        /// <returns>A dictionary that maps variables in the pattern to RideID values in a matching interpretation, or null when there is no match</returns>
        Dictionary<string,RideID> CheckInterpretationPattern(RideID interpreter, string pattern);
       
        /// <summary>
        /// Add a new narrator to the interpretation system
        /// </summary>
        /// <param name="templatesSource">Text of the knowledgebase axioms to be used as templates for text generation</param>
        /// <param name="interpreter">The RideID of the interpreter whose interpretations are used for narration</param>
        /// <returns></returns>
        RideID AddNarrator(string templatesSource, RideID interpreter);

        /// <summary>
        /// Generate a textual narrative
        /// </summary>
        /// <param name="narrator">The RideID of the narrator</param>
        /// <param name="method">A string that identifies the method to use to generate the narrative, e.g. "default"</param>
        /// <returns></returns>
        string Narrate(RideID narrator, string method);

        /// <summary>
        /// Specify a proper noun to use for a given entity in narrations
        /// </summary>
        /// <param name="entity">The RideID of the entity</param>
        /// <param name="noun">A string representing a proper noun, e.g. "SGT Smith" or "2nd Platoon"</param>
        void AddProperNoun(RideID entity, string noun);

        /// <summary>
        /// Specify a common noun to use for a given entity in narrations
        /// </summary>
        /// <param name="entity">The RideID of the entity</param>
        /// <param name="noun">A string representing a common noun. Prefer lowercase, e..g "soldier" or "market district"</param>
        void AddCommonNoun(RideID entity, string noun);

        /// <summary>
        /// Specify type of prounous to use for a given entity in narrations
        /// </summary>
        /// <param name="entity">The RideID of the entity</param>
        /// <param name="type">A string representing the type of prounouns to use. Types are system-specific, but expected values are "Masculine", "Feminine", "Neuter", and "Plural"</param>
        void SetPronouns(RideID entity, string type);

        /// <summary>
        /// Creates a string representation of a logical constant symbol for a RIDE entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        string Symbolize(RideID entity);
    }
}

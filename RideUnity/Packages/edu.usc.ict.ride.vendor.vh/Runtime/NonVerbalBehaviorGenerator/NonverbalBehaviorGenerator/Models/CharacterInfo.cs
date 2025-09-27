namespace NonverbalBehaviorGenerator.Models
{
    /// <remarks>
    /// Simulate NVBG.CharacterInfo
    /// </remarks>
    internal sealed class CharacterInfo
    {
        public string CharacterId { get; }
        public string Emotion { get; set; } = "neutral";
        /// <remarks>Refactor of CharacterInfo.Posture</remarks>
        public string PostureId { get; set; } = "HandsAtSide";
        public string Personality { get; set; } = "";
        public string NegotiationStance { get; set; } = "none";
        public string ConversationRole { get; set; } = "";
        public string ParticipationGoal { get; set; } = "0";
        public string ComprehensionGoal { get; set; } = "0";
        public string ParticipationStatus { get; set; } = "0";
        public string ComprehensionStatus { get; set; } = "0";
        public string Culture { get; set; } = "general";
        public CharacterStatus Status { get; set; } = CharacterStatus.Present;
        public string Role { get; set; } = "overhearer";
        public bool HasSpoken { get; set; } = false;

        public CharacterInfo(string characterId)
        {
            CharacterId = characterId;
        }
    }
}

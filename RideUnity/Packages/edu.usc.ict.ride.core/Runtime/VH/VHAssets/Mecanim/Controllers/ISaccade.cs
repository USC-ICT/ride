namespace VHAssets
{
public interface ISaccade : ICharacterFunctionality
{
    void PerformSaccade();
    void SetMode(CharacterDefines.SaccadeType mode);
    bool IsPerformingSaccade { get; }
}
}

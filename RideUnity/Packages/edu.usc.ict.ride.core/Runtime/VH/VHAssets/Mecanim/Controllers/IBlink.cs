namespace VHAssets
{
public interface IBlink : ICharacterFunctionality
{
    void Blink();
    bool IsBlinking { get; }
}
}

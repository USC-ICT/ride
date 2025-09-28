namespace VHAssets
{
/// <summary>
/// A controller created with the purpose of:
/// 1) Coordinating the functionality of one or more functionality controllers.
/// 2) Decorating the functionality of the controllers that it manages with additional rules based on application requirements
/// Manager controllers always require at least 1 Functionality controller to manager.Example: The Eye controller coordinates the Blink and Saccade controllers to make sure that blinking doesn't occur when a saccade is being performed
/// </summary>
public class ManagerController : VHCharacterController
{
    #region Variables

    #endregion

    #region Functions
    public override void Start()
    {
        base.Start();
    }

    public override void Update()
    {
        base.Update();
    }
    #endregion
}
}

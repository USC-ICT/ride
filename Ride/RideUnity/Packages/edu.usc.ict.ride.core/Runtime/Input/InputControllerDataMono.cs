namespace Ride.IO
{
    public interface IInputControllerData
    {
        IInputController InstallControllerComponent(RideID id);
    }

    public class InputControllerDataMono : RideDataUnityBootstrap
    {
        public IInputControllerData data;

        public override object GetData()
        {
            return data;
        }
    }
}

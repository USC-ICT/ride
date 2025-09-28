namespace Ride
{
    /// <summary>
    /// Interface for controlling animations on an entity
    /// </summary>
    public interface IAnimator
    {
        void Play(string animation);
        void SetParameter(string param, float data);
        void SetParameter(int param, float data);
        void SetParameter(string param, int data);
        void SetParameter(int param, int data);
        void SetParameter(string param, bool data);
        void SetParameter(int param, bool data);
        float GetParameterFloat(string param);
        int GetParameterInt(string param);
        bool GetParameterBool(string param);
        float GetParameterFloat(int param);
        int GetParameterInt(int param);
        bool GetParameterBool(int param);
        void SetTrigger(string param);
        void SetTrigger(int param);
    }
}


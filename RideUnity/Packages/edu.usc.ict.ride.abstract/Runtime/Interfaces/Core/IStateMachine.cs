namespace Ride
{
    public interface IStateMachine<TState, TTrigger, TParameter>
    {
        int id { get; }
        string name { get; set; }
        TState currentState { get; }
        IStateConfiguration<TState, TTrigger, TParameter> Configure(TState state);
        IStateConfiguration<TState, TTrigger, TParameter> GetState(TState state);
        void Fire(TTrigger trigger);
        void Fire<TArg0>(TTrigger trigger, TArg0 arg0);
        void Fire<TArg0, TArg1>(TTrigger trigger, TArg0 arg0, TArg1 arg1);
        void Fire<TArg0, TArg1, TArg2>(TTrigger trigger, TArg0 arg0, TArg1 arg1, TArg2 arg2);
        void SetParameter<T>(TParameter p, T data);
        T GetParameter<T>(TParameter id);
        bool HasParameter(TParameter id);
        void Update();
        void SetDefault(TState state);
        
    }
}

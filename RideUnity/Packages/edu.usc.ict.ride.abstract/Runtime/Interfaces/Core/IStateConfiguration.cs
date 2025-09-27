using System.Collections.Generic;

namespace Ride
{
    public delegate bool IsRequirementMet();
    public delegate bool IsRequirementMet<TArg0>(TArg0 arg0);
    public delegate bool IsRequirementMet<TArg0, TArg1>(TArg0 arg0, TArg1 arg1);
    public delegate bool IsRequirementMet<TArg0, TArg1, TArg2>(TArg0 arg0, TArg1 arg1, TArg2 arg2);
    public delegate void OnTransition();
    
    public delegate void OnUpdate();
    public delegate void OnTimeInterval();

    public partial interface IStateConfiguration<TState, TTrigger, TParameter>
    {
        TState id { get; }
        bool hasParent { get; }
        TState parent { get; }
        IStateConfiguration<TState, TTrigger, TParameter> OnEnter(OnTransition onEnter);
        IStateConfiguration<TState, TTrigger, TParameter> OnEnterFrom(TState enteringFrom, OnTransition onEnter);
        IStateConfiguration<TState, TTrigger, TParameter> OnExit(OnTransition onExit);
        IStateConfiguration<TState, TTrigger, TParameter> OnExitTo(TState exitingTo, OnTransition onExit);
        IStateConfiguration<TState, TTrigger, TParameter> OnUpdate(OnUpdate onUpdate);
        IStateConfiguration<TState, TTrigger, TParameter> OnTimeInvervalPassed(float interval, OnTimeInterval onInterval);
        IStateConfiguration<TState, TTrigger, TParameter> Permit(TTrigger trigger, TState toState);
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf(TTrigger trigger, TState toState, IsRequirementMet requirement);
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf<TArg0>(TTrigger trigger, TState toState, IsRequirementMet<TArg0> requirement);
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf<TArg0, TArg1>(TTrigger trigger, TState toState, IsRequirementMet<TArg0, TArg1> requirement);
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf<TArg0, TArg1, TArg2>(TTrigger trigger, TState toState, IsRequirementMet<TArg0, TArg1, TArg2> requirement);
        IStateConfiguration<TState, TTrigger, TParameter> SetParameter<T>(TParameter id, T data);
        IStateConfiguration<TState, TTrigger, TParameter> SubstateOf(TState parent);        
        T As<T>() where T : class, IStateConfiguration<TState, TTrigger, TParameter>;
        T GetParameter<T>(TParameter id);
        void Update();
        void HandleEnterFrom(TState from);
        void HandleExitTo(TState to);
        bool IsPermitted(TTrigger trigger, params object[] args);
        bool HasTransition(TState state);
        TState GetTransition(TTrigger trigger);
        IEnumerable<TState> substates { get; }
        void AddSubstate(TState substate);
        bool HasSubstate(TState substate);
    }
}


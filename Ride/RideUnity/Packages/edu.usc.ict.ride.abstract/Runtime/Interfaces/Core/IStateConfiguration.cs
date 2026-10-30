using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Determines whether a guarded state transition may occur.
    /// </summary>
    /// <returns><c>true</c> to permit the transition; otherwise, <c>false</c>.</returns>
    public delegate bool IsRequirementMet();

    /// <summary>
    /// Determines whether a guarded state transition may occur using one trigger argument.
    /// </summary>
    /// <typeparam name="TArg0">The type of the trigger argument.</typeparam>
    /// <param name="arg0">The argument supplied when the trigger is fired.</param>
    /// <returns><c>true</c> to permit the transition; otherwise, <c>false</c>.</returns>
    public delegate bool IsRequirementMet<TArg0>(TArg0 arg0);

    /// <summary>
    /// Determines whether a guarded state transition may occur using two trigger arguments.
    /// </summary>
    /// <typeparam name="TArg0">The type of the first trigger argument.</typeparam>
    /// <typeparam name="TArg1">The type of the second trigger argument.</typeparam>
    /// <param name="arg0">The first argument supplied when the trigger is fired.</param>
    /// <param name="arg1">The second argument supplied when the trigger is fired.</param>
    /// <returns><c>true</c> to permit the transition; otherwise, <c>false</c>.</returns>
    public delegate bool IsRequirementMet<TArg0, TArg1>(TArg0 arg0, TArg1 arg1);

    /// <summary>
    /// Determines whether a guarded state transition may occur using three trigger arguments.
    /// </summary>
    /// <typeparam name="TArg0">The type of the first trigger argument.</typeparam>
    /// <typeparam name="TArg1">The type of the second trigger argument.</typeparam>
    /// <typeparam name="TArg2">The type of the third trigger argument.</typeparam>
    /// <param name="arg0">The first argument supplied when the trigger is fired.</param>
    /// <param name="arg1">The second argument supplied when the trigger is fired.</param>
    /// <param name="arg2">The third argument supplied when the trigger is fired.</param>
    /// <returns><c>true</c> to permit the transition; otherwise, <c>false</c>.</returns>
    public delegate bool IsRequirementMet<TArg0, TArg1, TArg2>(TArg0 arg0, TArg1 arg1, TArg2 arg2);

    /// <summary>Represents an action performed when a state transition occurs.</summary>
    public delegate void OnTransition();

    /// <summary>Represents an action performed while a state is active.</summary>
    public delegate void OnUpdate();

    /// <summary>Represents an action performed after a configured interval elapses.</summary>
    public delegate void OnTimeInterval();

    /// <summary>
    /// Configures transitions, callbacks, parameters, and hierarchy for one state in a RIDE state machine.
    /// This fluent interface is used primarily to define agent and behavior state machines.
    /// </summary>
    /// <typeparam name="TState">The type used to identify states.</typeparam>
    /// <typeparam name="TTrigger">The type used to identify transition triggers.</typeparam>
    /// <typeparam name="TParameter">The type used to identify parameter values.</typeparam>
    public partial interface IStateConfiguration<TState, TTrigger, TParameter>
    {
        /// <summary>Gets the identifier of the configured state.</summary>
        TState id { get; }

        /// <summary>Gets whether this state is configured as a substate.</summary>
        bool hasParent { get; }

        /// <summary>Gets the parent state identifier, or its default value when <see cref="hasParent"/> is <c>false</c>.</summary>
        TState parent { get; }

        /// <summary>
        /// Registers an action to perform whenever this state is entered.
        /// </summary>
        /// <param name="onEnter">The action to invoke.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> OnEnter(OnTransition onEnter);

        /// <summary>
        /// Registers an action to perform when this state is entered from a specific state.
        /// </summary>
        /// <param name="enteringFrom">The state from which the transition originates.</param>
        /// <param name="onEnter">The action to invoke.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> OnEnterFrom(TState enteringFrom, OnTransition onEnter);

        /// <summary>
        /// Registers an action to perform whenever this state is exited.
        /// </summary>
        /// <param name="onExit">The action to invoke.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> OnExit(OnTransition onExit);

        /// <summary>
        /// Registers an action to perform when this state is exited for a specific state.
        /// </summary>
        /// <param name="exitingTo">The state to which the transition leads.</param>
        /// <param name="onExit">The action to invoke.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> OnExitTo(TState exitingTo, OnTransition onExit);

        /// <summary>
        /// Registers an action to perform each time the active state is updated.
        /// </summary>
        /// <param name="onUpdate">The action to invoke.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> OnUpdate(OnUpdate onUpdate);

        /// <summary>
        /// Registers a recurring action while this state is active.
        /// </summary>
        /// <param name="interval">The elapsed time, in seconds, between invocations.</param>
        /// <param name="onInterval">The action to invoke after each interval.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> OnTimeInvervalPassed(float interval, OnTimeInterval onInterval);

        /// <summary>
        /// Permits an unconditional transition for a trigger.
        /// </summary>
        /// <param name="trigger">The trigger that requests the transition.</param>
        /// <param name="toState">The destination state.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> Permit(TTrigger trigger, TState toState);

        /// <summary>
        /// Permits a transition when a parameterless requirement is met.
        /// </summary>
        /// <param name="trigger">The trigger that requests the transition.</param>
        /// <param name="toState">The destination state.</param>
        /// <param name="requirement">The condition that must return <c>true</c>.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf(TTrigger trigger, TState toState, IsRequirementMet requirement);

        /// <summary>
        /// Permits a transition when a requirement accepting one trigger argument is met.
        /// </summary>
        /// <typeparam name="TArg0">The type of the trigger argument.</typeparam>
        /// <param name="trigger">The trigger that requests the transition.</param>
        /// <param name="toState">The destination state.</param>
        /// <param name="requirement">The condition that must return <c>true</c>.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf<TArg0>(TTrigger trigger, TState toState, IsRequirementMet<TArg0> requirement);

        /// <summary>
        /// Permits a transition when a requirement accepting two trigger arguments is met.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">The type of the second trigger argument.</typeparam>
        /// <param name="trigger">The trigger that requests the transition.</param>
        /// <param name="toState">The destination state.</param>
        /// <param name="requirement">The condition that must return <c>true</c>.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf<TArg0, TArg1>(TTrigger trigger, TState toState, IsRequirementMet<TArg0, TArg1> requirement);

        /// <summary>
        /// Permits a transition when a requirement accepting three trigger arguments is met.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">The type of the second trigger argument.</typeparam>
        /// <typeparam name="TArg2">The type of the third trigger argument.</typeparam>
        /// <param name="trigger">The trigger that requests the transition.</param>
        /// <param name="toState">The destination state.</param>
        /// <param name="requirement">The condition that must return <c>true</c>.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> PermitIf<TArg0, TArg1, TArg2>(TTrigger trigger, TState toState, IsRequirementMet<TArg0, TArg1, TArg2> requirement);

        /// <summary>
        /// Sets a parameter value local to this state configuration.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="id">The parameter identifier.</param>
        /// <param name="data">The value to store.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> SetParameter<T>(TParameter id, T data);

        /// <summary>
        /// Makes this state a child of an existing configured state.
        /// Parent enter actions run before child enter actions, and parent exit actions run after child exit actions.
        /// </summary>
        /// <param name="parent">The identifier of the parent state.</param>
        /// <returns>This state configuration for fluent configuration.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> SubstateOf(TState parent);

        /// <summary>
        /// Casts this configuration to an implementation-specific configuration type.
        /// </summary>
        /// <typeparam name="T">The configuration type to return.</typeparam>
        /// <returns>The cast configuration, or <c>null</c> when the configuration is not compatible with <typeparamref name="T"/>.</returns>
        T As<T>() where T : class, IStateConfiguration<TState, TTrigger, TParameter>;

        /// <summary>
        /// Gets a parameter value, preferring a machine-level value over the state-local value.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="id">The parameter identifier.</param>
        /// <returns>The parameter value, or the default value of <typeparamref name="T"/> when no value is found.</returns>
        T GetParameter<T>(TParameter id);

        /// <summary>Invokes the update and interval actions registered for this state.</summary>
        void Update();

        /// <summary>
        /// Invokes the enter actions for this state and its ancestors.
        /// </summary>
        /// <param name="from">The state from which the transition originates.</param>
        void HandleEnterFrom(TState from);

        /// <summary>
        /// Invokes the exit actions for this state and its ancestors.
        /// </summary>
        /// <param name="to">The state to which the transition leads.</param>
        void HandleExitTo(TState to);

        /// <summary>
        /// Determines whether a trigger has a configured transition whose requirement is met by the supplied arguments.
        /// </summary>
        /// <param name="trigger">The trigger to evaluate.</param>
        /// <param name="args">The arguments passed to the transition requirement.</param>
        /// <returns><c>true</c> when the transition exists and its requirement is met; otherwise, <c>false</c>.</returns>
        bool IsPermitted(TTrigger trigger, params object[] args);

        /// <summary>
        /// Determines whether this state has a transition to a destination state.
        /// </summary>
        /// <param name="state">The destination state to find.</param>
        /// <returns><c>true</c> when a transition to the destination exists; otherwise, <c>false</c>.</returns>
        bool HasTransition(TState state);

        /// <summary>
        /// Gets the destination state configured for a trigger.
        /// </summary>
        /// <param name="trigger">The trigger whose destination is requested.</param>
        /// <returns>The destination state, or the default value of <typeparamref name="TState"/> when none is configured.</returns>
        TState GetTransition(TTrigger trigger);

        /// <summary>Gets the direct child states configured beneath this state.</summary>
        IEnumerable<TState> substates { get; }

        /// <summary>
        /// Adds a direct child state to this configuration.
        /// </summary>
        /// <param name="substate">The child state identifier.</param>
        void AddSubstate(TState substate);

        /// <summary>
        /// Determines whether a state is a direct child of this configuration.
        /// </summary>
        /// <param name="substate">The child state identifier to find.</param>
        /// <returns><c>true</c> when the state is a direct child; otherwise, <c>false</c>.</returns>
        bool HasSubstate(TState substate);
    }
}


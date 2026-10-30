namespace Ride
{
    /// <summary>
    /// Defines a configurable finite-state machine with guarded triggers, hierarchical states, and shared parameters.
    /// RIDE agent and behavior systems use this interface to expose and drive their runtime state machines.
    /// </summary>
    /// <typeparam name="TState">The type used to identify states.</typeparam>
    /// <typeparam name="TTrigger">The type used to identify transition triggers.</typeparam>
    /// <typeparam name="TParameter">The type used to identify parameter values.</typeparam>
    public interface IStateMachine<TState, TTrigger, TParameter>
    {
        /// <summary>Gets the implementation-defined state machine identifier.</summary>
        int id { get; }

        /// <summary>Gets or sets the descriptive name of the state machine.</summary>
        string name { get; set; }

        /// <summary>Gets the currently active state.</summary>
        TState currentState { get; }

        /// <summary>
        /// Creates or retrieves the configuration for a state.
        /// </summary>
        /// <param name="state">The state identifier to configure.</param>
        /// <returns>The configuration associated with the state.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> Configure(TState state);

        /// <summary>
        /// Gets an existing state configuration.
        /// </summary>
        /// <param name="state">The state identifier to find.</param>
        /// <returns>The state configuration, or <c>null</c> when the state has not been configured.</returns>
        IStateConfiguration<TState, TTrigger, TParameter> GetState(TState state);

        /// <summary>
        /// Fires a trigger without arguments and transitions when its requirement is met.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        void Fire(TTrigger trigger);

        /// <summary>
        /// Fires a trigger with one argument and transitions when its requirement is met.
        /// </summary>
        /// <typeparam name="TArg0">The type of the trigger argument.</typeparam>
        /// <param name="trigger">The trigger to fire.</param>
        /// <param name="arg0">The argument passed to the transition requirement.</param>
        void Fire<TArg0>(TTrigger trigger, TArg0 arg0);

        /// <summary>
        /// Fires a trigger with two arguments and transitions when its requirement is met.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">The type of the second trigger argument.</typeparam>
        /// <param name="trigger">The trigger to fire.</param>
        /// <param name="arg0">The first argument passed to the transition requirement.</param>
        /// <param name="arg1">The second argument passed to the transition requirement.</param>
        void Fire<TArg0, TArg1>(TTrigger trigger, TArg0 arg0, TArg1 arg1);

        /// <summary>
        /// Fires a trigger with three arguments and transitions when its requirement is met.
        /// </summary>
        /// <typeparam name="TArg0">The type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">The type of the second trigger argument.</typeparam>
        /// <typeparam name="TArg2">The type of the third trigger argument.</typeparam>
        /// <param name="trigger">The trigger to fire.</param>
        /// <param name="arg0">The first argument passed to the transition requirement.</param>
        /// <param name="arg1">The second argument passed to the transition requirement.</param>
        /// <param name="arg2">The third argument passed to the transition requirement.</param>
        void Fire<TArg0, TArg1, TArg2>(TTrigger trigger, TArg0 arg0, TArg1 arg1, TArg2 arg2);

        /// <summary>
        /// Sets a parameter value shared by the state machine.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="p">The parameter identifier.</param>
        /// <param name="data">The value to store.</param>
        void SetParameter<T>(TParameter p, T data);

        /// <summary>
        /// Gets a parameter value shared by the state machine.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="id">The parameter identifier.</param>
        /// <returns>The parameter value, or the default value of <typeparamref name="T"/> when no value is found.</returns>
        T GetParameter<T>(TParameter id);

        /// <summary>
        /// Determines whether the state machine contains a parameter.
        /// </summary>
        /// <param name="id">The parameter identifier.</param>
        /// <returns><c>true</c> when the parameter exists; otherwise, <c>false</c>.</returns>
        bool HasParameter(TParameter id);

        /// <summary>Updates the callbacks and intervals of the active state.</summary>
        void Update();

        /// <summary>
        /// Sets the active state without invoking transition callbacks.
        /// </summary>
        /// <param name="state">The state to make active.</param>
        void SetDefault(TState state);
    }
}

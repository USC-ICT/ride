namespace Ride.Animations
{
    /// <summary>Collects the movement and state values used to update an agent's Animator parameters.</summary>
    /// <remarks><c>AnimationSystemUnity</c> creates this value each update before applying it through an <see cref="IAnimationController"/>.</remarks>
    public struct RideAgentAnimationParams
    {
        /// <summary>The agent state name supplied by the simulation.</summary>
        public string agentState;

        /// <summary>The forward and lateral movement velocity.</summary>
        public RideVector2 movementVelocity;

        /// <summary>The movement speed relative to the agent's configured maximum.</summary>
        public float relativeSpeed;

        /// <summary>The agent's rotational velocity.</summary>
        public float rotationVelocity;

        /// <summary>Whether the agent is sprinting.</summary>
        public bool isSprinting;

        /// <summary>Whether the agent is jumping.</summary>
        public bool isJumping;

        /// <summary>Whether the agent should use military animation behavior.</summary>
        public bool isMilitary;

        /// <summary>Initializes a complete set of agent animation parameters.</summary>
        /// <param name="state">The current simulation state name.</param>
        /// <param name="movement">The forward and lateral movement velocity.</param>
        /// <param name="relSpeed">The movement speed relative to the configured maximum.</param>
        /// <param name="rotation">The rotational velocity.</param>
        /// <param name="sprinting">Whether the agent is sprinting.</param>
        /// <param name="jumping">Whether the agent is jumping.</param>
        /// <param name="military">Whether to use military animation behavior.</param>
        public RideAgentAnimationParams(string state, RideVector2 movement, float relSpeed, float rotation, bool sprinting, bool jumping, bool military)
        {
            agentState = state;
            movementVelocity = movement;
            relativeSpeed = relSpeed;
            rotationVelocity = rotation;
            isSprinting = sprinting;
            isJumping = jumping;
            isMilitary = military;
        }
    }

    /// <summary>Adapts a Unity <see cref="UnityEngine.Animator"/> to the backend-independent <see cref="IAnimationController"/> interface.</summary>
    /// <remarks>
    /// Primarily created and driven by <c>AnimationSystemUnity</c>. Animator parameter names are shared through
    /// <see cref="StateMachineConstants"/> and its legacy <see cref="StateMachineGlobals"/> aliases.
    /// See <see href="https://docs.unity3d.com/ScriptReference/Animator.html"/> for Unity Animator behavior.
    /// </remarks>
    [UnityEngine.RequireComponent(typeof(UnityEngine.Animator))]
    public class RideAgentAnimationController : RideMonoBehaviour, IAnimationController
    {
        /// <summary>The Unity Animator controlled by this adapter.</summary>
        UnityEngine.Animator animator;

        /// <summary>Caches the required Animator component.</summary>
        private void Awake()
        {
            animator = GetComponent<UnityEngine.Animator>();
        }

        /// <inheritdoc/>
        public void SetTrigger(string name)
        {
            animator.SetTrigger(name);
        }

        /// <inheritdoc/>
        public void SetBool(string name, bool value)
        {
            animator.SetBool(name, value);
        }

        /// <inheritdoc/>
        public bool GetBool(string name)
        {
            return animator.GetBool(name);
        }

        /// <inheritdoc/>
        public void SetInteger(string name, int value)
        {
            animator.SetInteger(name, value);
        }

        /// <inheritdoc/>
        public int GetInteger(string name)
        {
            return animator.GetInteger(name);
        }

        /// <inheritdoc/>
        public void SetFloat(string name, float value)
        {
            animator.SetFloat(name, value);
        }

        /// <inheritdoc/>
        public float GetFloat(string name)
        {
            return animator.GetFloat(name);
        }

        /// <summary>Dispatches the world-state event that marks the animated throw release point.</summary>
        /// <param name="s">The optional string payload supplied by a Unity Animation Event; it is not currently used.</param>
        public void AnimationThrowPoint(string s)
        {
            Globals.api.worldStateSystem.DispatchEvent<WorldState.AgentEvent>(WorldState.WorldEvent.agentEndThrowObject, new WorldState.AgentEvent(id));
        }
    }
}

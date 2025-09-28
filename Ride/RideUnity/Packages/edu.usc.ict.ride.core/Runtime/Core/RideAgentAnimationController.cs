namespace Ride.Animations
{
    public struct RideAgentAnimationParams
    {
        public string agentState;
        public RideVector2 movementVelocity;
        public float relativeSpeed;
        public float rotationVelocity;
        public bool isSprinting;
        public bool isJumping;
        public bool isMilitary;

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

    [UnityEngine.RequireComponent(typeof(UnityEngine.Animator))]
    public class RideAgentAnimationController : RideMonoBehaviour, IAnimationController
    {
        UnityEngine.Animator animator;

        private void Awake()
        {
            animator = GetComponent<UnityEngine.Animator>();
        }

        public void SetTrigger(string name)
        {
            animator.SetTrigger(name);
        }

        public void SetBool(string name, bool value)
        {
            animator.SetBool(name, value);
        }

        public bool GetBool(string name)
        {
            return animator.GetBool(name);
        }

        public void SetInteger(string name, int value)
        {
            animator.SetInteger(name, value);
        }

        public int GetInteger(string name)
        {
            return animator.GetInteger(name);
        }

        public void SetFloat(string name, float value)
        {
            animator.SetFloat(name, value);
        }

        public float GetFloat(string name)
        {
            return animator.GetFloat(name);
        }

        public void AnimationThrowPoint(string s)
        {
            Globals.api.worldStateSystem.DispatchEvent<WorldState.AgentEvent>(WorldState.WorldEvent.agentEndThrowObject, new WorldState.AgentEvent(id));
        }
    }
}
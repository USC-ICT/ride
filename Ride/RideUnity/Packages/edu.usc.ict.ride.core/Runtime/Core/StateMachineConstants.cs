
namespace Ride
{
    /// <summary>Defines shared agent state names and Animator parameter paths used by RIDE simulation animation systems.</summary>
    /// <remarks>
    /// <c>AnimationSystemUnity</c>, <c>PlayerAnimationController</c>, and agent state handlers use these values to keep
    /// simulation events aligned with the parameters configured in Unity Animator controllers.
    /// </remarks>
    public static class StateMachineConstants
    {
        #region State Names
        /// <summary>The idle agent state name.</summary>
        public const string IdleState = "Idle";
        /// <summary>The moving agent state name.</summary>
        public const string MoveState = "Move";
        /// <summary>The dead agent state name.</summary>
        public const string DeadState = "Dead";
        /// <summary>The crouched agent state name.</summary>
        public const string CrouchState = "Crouch";
        /// <summary>The stationary combat-engagement state name.</summary>
        public const string CombatEngageState = "CombatEngage";
        /// <summary>The moving combat-engagement state name.</summary>
        public const string CombatEngageMoveState = "CombatEngageMove";
        /// <summary>The firearm-discharge state name.</summary>
        public const string FireGunState = "FireGunState";
        /// <summary>The jumping agent state name.</summary>
        public const string JumpState = "Jump";
        /// <summary>The grounded state name used after landing.</summary>
        public const string LandState = "IsGrounded";
        #endregion

        #region Trigger
        /// <summary>Identifies transitions understood by RIDE agent state machines.</summary>
        public enum Trigger
        {
            /// <summary>Transition to the idle state.</summary>
            Idle,
            /// <summary>Transition to the moving state.</summary>
            Move,
            /// <summary>Transition to the dead state.</summary>
            Die,
        }
        #endregion

        #region Animator Parameter Names
        /// <summary>The forward movement velocity parameter path.</summary>
        public const string MoveParam = "Base/Velocity";
        /// <summary>The rotational velocity parameter path.</summary>
        public const string RotateParam = "Base/Rotation";
        /// <summary>The turning-state parameter path.</summary>
        public const string TurnParam = "Base/IsTurning";
        /// <summary>The dead-state parameter path.</summary>
        public const string DeadParam = "Base/IsDead";
        /// <summary>The posture or stance parameter path.</summary>
        public const string StanceParam = "Base/Stance";
        /// <summary>The combat-engagement parameter path.</summary>
        public const string EngagedParam = "Base/IsEngaged";
        /// <summary>The firearm trigger parameter path.</summary>
        public const string FireParam = "Base/DoFire";
        /// <summary>The lateral movement parameter path.</summary>
        public const string StrafeParam = "Base/Strafe";
        /// <summary>The sprinting-state parameter path.</summary>
        public const string SprintParam = "Base/IsSprinting";
        /// <summary>The jump trigger parameter path.</summary>
        public const string JumpParam = "Base/DoJump";
        /// <summary>The grounded-state parameter path.</summary>
        public const string LandParam = "Base/IsGrounded";
        /// <summary>The upper-body throw trigger parameter path.</summary>
        public const string ThrowParam = "UpperBody/Throw";
        /// <summary>The movement animation speed multiplier parameter path.</summary>
        public const string MoveSpeedParam = "Base/SpeedMultiplier";
        /// <summary>The parameter selecting military animation behavior.</summary>
        public const string IsMilitaryParam = "isMilitary";
        /// <summary>The trigger used after the agent class changes.</summary>
        public const string ClassChangedParam = "ClassChanged";
        /// <summary>The idle-emote selection parameter path.</summary>
        public const string IdleEmoteParam = "Base/IdleEmote";
        /// <summary>The reload trigger parameter path.</summary>
        public const string ReloadParam = "Base/DoReload";
        /// <summary>The in-cover state parameter path.</summary>
        public const string IsInCoverParam = "Base/IsInCover";
        /// <summary>The continuous cover-position parameter path.</summary>
        public const string CoverFloatParam = "Base/CoverFloat";
        #endregion
    }

    /// <summary>Provides compatibility aliases for code that predates <see cref="StateMachineConstants"/>.</summary>
    /// <remarks>New code should use <see cref="StateMachineConstants"/> directly.</remarks>
    // TODO - intent is to deprecate this usage, in preference to the above
    public static class StateMachineGlobals
    {
        /// <inheritdoc cref="StateMachineConstants.IdleState"/>
        public const string IdleState = StateMachineConstants.IdleState;
        /// <inheritdoc cref="StateMachineConstants.MoveState"/>
        public const string MoveState = StateMachineConstants.MoveState;
        /// <inheritdoc cref="StateMachineConstants.DeadState"/>
        public const string DeadState = StateMachineConstants.DeadState;
        /// <inheritdoc cref="StateMachineConstants.CrouchState"/>
        public const string CrouchState = StateMachineConstants.CrouchState;
        /// <inheritdoc cref="StateMachineConstants.CombatEngageState"/>
        public const string CombatEngageState = StateMachineConstants.CombatEngageState;
        /// <inheritdoc cref="StateMachineConstants.CombatEngageMoveState"/>
        public const string CombatEngageMoveState = StateMachineConstants.CombatEngageMoveState;
        /// <inheritdoc cref="StateMachineConstants.FireGunState"/>
        public const string FireGunState = StateMachineConstants.FireGunState;
        /// <inheritdoc cref="StateMachineConstants.JumpState"/>
        public const string JumpState = StateMachineConstants.JumpState;
        /// <inheritdoc cref="StateMachineConstants.LandState"/>
        public const string LandState = StateMachineConstants.LandState;

        /// <inheritdoc cref="StateMachineConstants.MoveParam"/>
        public const string MoveParam = StateMachineConstants.MoveParam;
        /// <inheritdoc cref="StateMachineConstants.RotateParam"/>
        public const string RotateParam = StateMachineConstants.RotateParam;
        /// <inheritdoc cref="StateMachineConstants.TurnParam"/>
        public const string TurnParam = StateMachineConstants.TurnParam;
        /// <inheritdoc cref="StateMachineConstants.DeadParam"/>
        public const string DeadParam = StateMachineConstants.DeadParam;
        /// <inheritdoc cref="StateMachineConstants.StanceParam"/>
        public const string StanceParam = StateMachineConstants.StanceParam;
        /// <inheritdoc cref="StateMachineConstants.EngagedParam"/>
        public const string EngagedParam = StateMachineConstants.EngagedParam;
        /// <inheritdoc cref="StateMachineConstants.FireParam"/>
        public const string FireParam = StateMachineConstants.FireParam;
        /// <inheritdoc cref="StateMachineConstants.StrafeParam"/>
        public const string StrafeParam = StateMachineConstants.StrafeParam;
        /// <inheritdoc cref="StateMachineConstants.SprintParam"/>
        public const string SprintParam = StateMachineConstants.SprintParam;
        /// <inheritdoc cref="StateMachineConstants.JumpParam"/>
        public const string JumpParam = StateMachineConstants.JumpParam;
        /// <inheritdoc cref="StateMachineConstants.LandParam"/>
        public const string LandParam = StateMachineConstants.LandParam;
        /// <inheritdoc cref="StateMachineConstants.ThrowParam"/>
        public const string ThrowParam = StateMachineConstants.ThrowParam;
        /// <inheritdoc cref="StateMachineConstants.MoveSpeedParam"/>
        public const string MoveSpeedParam = StateMachineConstants.MoveSpeedParam;
        /// <inheritdoc cref="StateMachineConstants.IsMilitaryParam"/>
        public const string IsMilitaryParam = StateMachineConstants.IsMilitaryParam;
        /// <inheritdoc cref="StateMachineConstants.ClassChangedParam"/>
        public const string ClassChangedParam = StateMachineConstants.ClassChangedParam;
        /// <inheritdoc cref="StateMachineConstants.IdleEmoteParam"/>
        public const string IdleEmoteParam = StateMachineConstants.IdleEmoteParam;
        /// <inheritdoc cref="StateMachineConstants.ReloadParam"/>
        public const string ReloadParam = StateMachineConstants.ReloadParam;
        /// <inheritdoc cref="StateMachineConstants.IsInCoverParam"/>
        public const string IsInCoverParam = StateMachineConstants.IsInCoverParam;
        /// <inheritdoc cref="StateMachineConstants.CoverFloatParam"/>
        public const string CoverFloatParam = StateMachineConstants.CoverFloatParam;
    }
}

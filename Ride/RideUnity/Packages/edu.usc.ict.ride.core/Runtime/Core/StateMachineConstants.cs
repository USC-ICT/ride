
namespace Ride
{
    public static class StateMachineConstants
    {
        #region State Names
        public const string IdleState = "Idle";
        public const string MoveState = "Move";
        public const string DeadState = "Dead";
        public const string CrouchState = "Crouch";
        public const string CombatEngageState = "CombatEngage";
        public const string CombatEngageMoveState = "CombatEngageMove";
        public const string FireGunState = "FireGunState";
        public const string JumpState = "Jump";
        public const string LandState = "IsGrounded";
        #endregion

        #region Trigger
        public enum Trigger
        {
            Idle,
            Move,
            Die,
        }
        #endregion

        #region Animator Parameter Names
        public const string MoveParam = "Base/Velocity";
        public const string RotateParam = "Base/Rotation";
        public const string TurnParam = "Base/IsTurning";
        public const string DeadParam = "Base/IsDead";
        public const string StanceParam = "Base/Stance";
        public const string EngagedParam = "Base/IsEngaged";
        public const string FireParam = "Base/DoFire";
        public const string StrafeParam = "Base/Strafe";
        public const string SprintParam = "Base/IsSprinting";
        public const string JumpParam = "Base/DoJump";
        public const string LandParam = "Base/IsGrounded";
        public const string ThrowParam = "UpperBody/Throw";
        public const string MoveSpeedParam = "Base/SpeedMultiplier";
        public const string IsMilitaryParam = "isMilitary";
        public const string ClassChangedParam = "ClassChanged";
        public const string IdleEmoteParam = "Base/IdleEmote";
        public const string ReloadParam = "Base/DoReload";
        public const string IsInCoverParam = "Base/IsInCover";
        public const string CoverFloatParam = "Base/CoverFloat";
        #endregion
    }

    // TODO - intent is to deprecate this usage, in preference to the above
    public static class StateMachineGlobals
    {
        public const string IdleState = StateMachineConstants.IdleState;
        public const string MoveState = StateMachineConstants.MoveState;
        public const string DeadState = StateMachineConstants.DeadState;
        public const string CrouchState = StateMachineConstants.CrouchState;
        public const string CombatEngageState = StateMachineConstants.CombatEngageState;
        public const string CombatEngageMoveState = StateMachineConstants.CombatEngageMoveState;
        public const string FireGunState = StateMachineConstants.FireGunState;
        public const string JumpState = StateMachineConstants.JumpState;
        public const string LandState = StateMachineConstants.LandState;

        public const string MoveParam = StateMachineConstants.MoveParam;
        public const string RotateParam = StateMachineConstants.RotateParam;
        public const string TurnParam = StateMachineConstants.TurnParam;
        public const string DeadParam = StateMachineConstants.DeadParam;
        public const string StanceParam = StateMachineConstants.StanceParam;
        public const string EngagedParam = StateMachineConstants.EngagedParam;
        public const string FireParam = StateMachineConstants.FireParam;
        public const string StrafeParam = StateMachineConstants.StrafeParam;
        public const string SprintParam = StateMachineConstants.SprintParam;
        public const string JumpParam = StateMachineConstants.JumpParam;
        public const string LandParam = StateMachineConstants.LandParam;
        public const string ThrowParam = StateMachineConstants.ThrowParam;
        public const string MoveSpeedParam = StateMachineConstants.MoveSpeedParam;
        public const string IsMilitaryParam = StateMachineConstants.IsMilitaryParam;
        public const string ClassChangedParam = StateMachineConstants.ClassChangedParam;
        public const string IdleEmoteParam = StateMachineConstants.IdleEmoteParam;
        public const string ReloadParam = StateMachineConstants.ReloadParam;
        public const string IsInCoverParam = StateMachineConstants.IsInCoverParam;
        public const string CoverFloatParam = StateMachineConstants.CoverFloatParam;
    }
}

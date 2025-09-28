namespace Ride
{
    /// <summary>
    /// Base MonoBehaviour for all RIDE systems.
    /// Registers with <see cref="SystemAccessSystem"/> during <c>Awake()</c>, and dispatches lifecycle events to overridable methods.
    /// Systems derived from this class should override the <c>System*</c> methods to implement custom logic.
    /// </summary>
    public class RideSystemMonoBehaviour : RideMonoBehaviour, IRideSystem
    {
        /// <inheritdoc />
        public bool SystemAwakeCalled { get; protected set; }
        /// <inheritdoc />
        public bool SystemInitCalled { get; protected set; }
        /// <inheritdoc />
        public bool IsActive { get; protected set; }

        protected virtual void Awake()
        {
            //UnityEngine.Debug.LogFormat("RideSystemMonoBehaviour.Awake() - {0}", GetType());

            Systems.Access.AddSystem(this);
            SystemAwake();
        }

        protected override void Start()
        {
            //UnityEngine.Debug.LogFormat("RideSystemMonoBehaviour.Start() - {0}", GetType());

            base.Start();
            SystemInit();
        }

        protected override void Update()
        {
            base.Update();

            float dt = RideUtils.GetDeltaTime();
            SystemUpdate(dt);
        }

        void LateUpdate() => SystemLateUpdate(RideUtils.GetDeltaTime());
        void FixedUpdate() => SystemFixedUpdate(RideUtils.GetDeltaTime());
        void OnDestroy() => SystemShutdown();

        /// <inheritdoc />
        public virtual void SystemAwake()
        {
            //RideLogSystem.Log("SystemAwake " + GetType().ToString());

            SystemAwakeCalled = true;
        }

        /// <inheritdoc />
        public virtual void SystemInit()
        {
            //RideLogSystem.Log("SystemInit " + GetType().ToString());

            SystemInitCalled = true;
        }

        /// <inheritdoc />
        public virtual void SystemShutdown()
        {
            //RideLogSystem.Log("SystemShutdown " + GetType().ToString());
        }

        /// <inheritdoc />
        public virtual void SystemUpdate(float dt) { }

        /// <inheritdoc />
        public virtual void SystemLateUpdate(float dt) { }

        /// <inheritdoc />
        public virtual void SystemFixedUpdate(float dt) { }

        /// <inheritdoc />
        public virtual void Activate() => IsActive = true;

        /// <inheritdoc />
        public virtual void Deactivate() => IsActive = false;
    }
}

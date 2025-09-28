using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Converts a Unity scene object into a RIDE-registered object.
    /// Attach this to any GameObject that should be registered automatically with the Ride GameObject System.
    /// </summary>
    [DisallowMultipleComponent]
    public class ConvertToRide : RideMonoBehaviour
    {
        /// <summary>
        /// Whether this object has already been converted into the Ride system.
        /// </summary>
        public bool Converted { get; private set; }


        /// <summary>
        /// Called automatically by Unity. Starts the conversion process when Ride is initialized.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            StartCoroutine(ConvertObjectAtStart());
        }

        /// <summary>
        /// Coroutine that waits for the Ride API to initialize,
        /// then converts this object if not already registered.
        /// </summary>
        IEnumerator ConvertObjectAtStart()
        {
            yield return new WaitUntil(() => Systems.Access != null);

            if (!Systems.GameObject.Exists(id))
                Convert();
        }

        /// <summary>
        /// Attempts to convert this GameObject into the Ride GameObject System.
        /// Safe to call multiple times; will no-op if already converted.
        /// </summary>
        public void Convert()
        {
            if (Converted)
                return;

            id = Systems.GameObject.AddExistingObject(gameObject);

            var rideComponents = GetComponents<RideMonoBehaviour>();
            foreach (var component in rideComponents)
                component.ConvertToRide(id);

            Converted = true;
        }
    }
}

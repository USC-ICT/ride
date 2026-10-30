using UnityEngine;
using UnityEditor;

namespace Ride
{
    /// <summary>Adds Addressables catalog-label controls to the <see cref="AddressableSystem"/> inspector.</summary>
    /// <remarks>
    /// The Load Asset Labels button invokes <see cref="AddressableSystem.EditorLoadCatalogs"/> so
    /// <see cref="RideAddressableEditor"/> and other editor tooling can present the labels available
    /// in the active Addressables catalogs.
    /// </remarks>
    [CustomEditor(typeof(AddressableSystem))]
    public class AddressableSystemEditor : Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            AddressableSystem m_rideAddressableSystem = (AddressableSystem)target;

            // Refresh the cached label list used by RideAddressable inspectors.
            if (GUILayout.Button("Load Asset Labels"))
                m_rideAddressableSystem.EditorLoadCatalogs();

            EditorUtility.SetDirty(m_rideAddressableSystem);
        }
    }
}

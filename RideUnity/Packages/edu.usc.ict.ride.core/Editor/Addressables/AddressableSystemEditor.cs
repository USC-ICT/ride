using UnityEngine;
using UnityEditor;

namespace Ride
{
    [CustomEditor(typeof(AddressableSystem))]
    public class AddressableSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            AddressableSystem m_rideAddressableSystem = (AddressableSystem)target;
            if (GUILayout.Button("Load Asset Labels"))
                m_rideAddressableSystem.EditorLoadCatalogs();
            EditorUtility.SetDirty(m_rideAddressableSystem);
        }
    }
}

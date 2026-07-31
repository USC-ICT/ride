using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ride
{
    [CustomEditor(typeof(RideAddressable))]
    public class RideAddressableEditor : Editor
    {
        private AddressableSystem m_rideAddressableSystem;
        private SerializedProperty m_labelToLoadProperty;
        private SerializedProperty m_onAssetLoadedProperty;
        private SerializedProperty m_assetLabelsOfLoadedObjectProperty;


        private void OnEnable()
        {
            m_labelToLoadProperty = serializedObject.FindProperty("m_assetLabelToLoad");
            m_onAssetLoadedProperty = serializedObject.FindProperty("m_onAssetLoaded");
            m_assetLabelsOfLoadedObjectProperty = serializedObject.FindProperty("m_assetLabelsOfLoadedObject");
            m_rideAddressableSystem = FindAnyObjectByType<AddressableSystem>();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (m_rideAddressableSystem != null)
            {
                //default to None
                List<string> availableLabels = new(){ "None" };
                availableLabels.AddRange(m_rideAddressableSystem.AvailableLabels);

                if (availableLabels.Count > 1)
                {
                    //cache previous selection if still exists
                    int selectedIndex = availableLabels.IndexOf(m_labelToLoadProperty.stringValue);
                    if (selectedIndex == -1) selectedIndex = 0;

                    selectedIndex = EditorGUILayout.Popup("Label to Load", selectedIndex, availableLabels.ToArray());
                    m_labelToLoadProperty.stringValue = selectedIndex > 0 ? availableLabels[selectedIndex] : "";
                    EditorGUILayout.PropertyField(m_onAssetLoadedProperty);
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(m_assetLabelsOfLoadedObjectProperty);
                    GUI.enabled = true;
                }
                else
                    EditorGUILayout.HelpBox("No labels available. Load them using the RideAddressableSystem.", MessageType.Warning);
            }
            else
                EditorGUILayout.HelpBox("RideAddressableSystem not found in scene.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
        }
    }
}


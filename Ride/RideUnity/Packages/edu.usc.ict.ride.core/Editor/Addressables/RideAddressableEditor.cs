using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ride
{
    /// <summary>Provides the custom inspector used to configure a <see cref="RideAddressable"/> component.</summary>
    /// <remarks>
    /// The inspector obtains label choices from the scene's <see cref="AddressableSystem"/> and
    /// exposes the selected load label, asset-loaded callback, and read-only labels reported for
    /// the instantiated asset.
    /// </remarks>
    [CustomEditor(typeof(RideAddressable))]
    public class RideAddressableEditor : Editor
    {
        /// <summary>Addressables system that supplies the available catalog labels.</summary>
        private AddressableSystem m_rideAddressableSystem;

        /// <summary>Serialized label selected for loading.</summary>
        private SerializedProperty m_labelToLoadProperty;

        /// <summary>Serialized event invoked after the requested asset is loaded.</summary>
        private SerializedProperty m_onAssetLoadedProperty;

        /// <summary>Serialized labels associated with the currently loaded object.</summary>
        private SerializedProperty m_assetLabelsOfLoadedObjectProperty;


        /// <summary>Caches the serialized properties and locates the scene's Addressables system.</summary>
        private void OnEnable()
        {
            m_labelToLoadProperty = serializedObject.FindProperty("m_assetLabelToLoad");
            m_onAssetLoadedProperty = serializedObject.FindProperty("m_onAssetLoaded");
            m_assetLabelsOfLoadedObjectProperty = serializedObject.FindProperty("m_assetLabelsOfLoadedObject");
            m_rideAddressableSystem = FindAnyObjectByType<AddressableSystem>();
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (m_rideAddressableSystem != null)
            {
                // Include a None choice so the component can be configured not to load a label.
                List<string> availableLabels = new(){ "None" };
                availableLabels.AddRange(m_rideAddressableSystem.AvailableLabels);

                if (availableLabels.Count > 1)
                {
                    // Preserve the previous selection when it still exists in the refreshed catalog.
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


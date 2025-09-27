using UnityEditor;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="RideCatalogAsset"/> components.
    /// Allows configuration of asset loading behavior by name or label and provides validation hints.
    /// </summary>
    [CustomEditor(typeof(RideCatalogAsset))]
    public class RideCatalogAssetEditor : Editor
    {
        private SerializedProperty loadTypeProp;
        private SerializedProperty assetNameProp;
        private SerializedProperty labelsToLoadProp;
        private SerializedProperty loadOnStartProp;
        private SerializedProperty onAssetLoadedProp;
        private SerializedProperty placeholderObjectProp;


        private void OnEnable()
        {
            loadTypeProp = serializedObject.FindProperty("loadType");
            assetNameProp = serializedObject.FindProperty("assetNameToLoad");
            labelsToLoadProp = serializedObject.FindProperty("labelsToLoad");
            loadOnStartProp = serializedObject.FindProperty("m_loadOnStart");
            onAssetLoadedProp = serializedObject.FindProperty("m_onAssetLoaded");
            placeholderObjectProp = serializedObject.FindProperty("m_placeholderObject");
        }

        /// <summary>
        /// Renders the custom inspector interface for configuring RideCatalogAsset loading settings.
        /// Displays warnings if required fields are left empty based on selected load type.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(loadTypeProp, new GUIContent("Load Type"));
            bool showWarning = false;
            string warningText = string.Empty;
            if ((RideCatalogAsset.LoadType)loadTypeProp.enumValueIndex == RideCatalogAsset.LoadType.Name)
            {
                EditorGUILayout.PropertyField(assetNameProp, new GUIContent("Asset Name"));
                if (string.IsNullOrEmpty(assetNameProp.stringValue))
                {
                    showWarning = true;
                    warningText = "Please specify a valid asset name.";
                }
            }
            else
            {
                EditorGUILayout.PropertyField(labelsToLoadProp, new GUIContent("Labels"), true);
                if (labelsToLoadProp.arraySize == 0)
                {
                    showWarning = true;
                    warningText = "Please specify at least one label.";
                }
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(placeholderObjectProp, new GUIContent("Placeholder Object"));
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(onAssetLoadedProp, new GUIContent("On Asset Loaded"));
            EditorGUILayout.PropertyField(loadOnStartProp, new GUIContent("Load On Start"));
            if (showWarning)
                EditorGUILayout.HelpBox(warningText, MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Custom inspector for <see cref="AssetLoadingSystemAssetBundles"/> that supports drag-and-drop catalog assignment
    /// and editing of load settings (local path, remote flag).
    /// </summary>
    [CustomEditor(typeof(AssetLoadingSystemAssetBundles))]
    public class AssetLoadingSystemAssetBundlesEditor : Editor
    {
        private SerializedProperty m_catalogsToLoad;
        private AssetLoadingSystemAssetBundles m_target;

        private void OnEnable()
        {
            m_target = (AssetLoadingSystemAssetBundles)target;
            m_catalogsToLoad = serializedObject.FindProperty("m_catalogsToLoad");
        }

        /// <summary>
        /// Renders the custom inspector GUI for managing catalogs to load.
        /// Allows for dragging, dropping and verification of json files to track new catalogs.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Label("Catalogs to Load", EditorStyles.boldLabel);
            Rect dropArea = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag and drop catalog.json files here", EditorStyles.helpBox);
            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObj in DragAndDrop.objectReferences)
                        {
                            if (draggedObj is TextAsset textAsset)
                            {
                                if (IsValidCatalogJson(textAsset))
                                {
                                    CatalogLoadInfoUnity newCatalog = new CatalogLoadInfoUnity { catalogJsonFile = textAsset };
                                    m_target.m_catalogsToLoad.Add(newCatalog);
                                }
                                else
                                    Debug.LogWarning($"Skipped file '{textAsset.name}': Not a valid AssetCatalogData format.");
                            }
                        }
                        EditorUtility.SetDirty(m_target);
                        serializedObject.Update();
                        evt.Use();
                    }
                }
            }
            GUILayout.Space(10);

            for (int i = 0; i < m_catalogsToLoad.arraySize; i++)
            {
                SerializedProperty element = m_catalogsToLoad.GetArrayElementAtIndex(i);
                SerializedProperty catalogJsonFile = element.FindPropertyRelative("catalogJsonFile");
                SerializedProperty catalogPath = element.FindPropertyRelative("catalogPath");
                SerializedProperty isRemote = element.FindPropertyRelative("isRemote");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Catalog {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    m_catalogsToLoad.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(catalogJsonFile, new GUIContent("Catalog JSON File"));
                if (catalogJsonFile.objectReferenceValue == null)
                {
                    EditorGUILayout.PropertyField(catalogPath, new GUIContent("Catalog Path"));
                    EditorGUILayout.PropertyField(isRemote, new GUIContent("Is Remote?"));
                }
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Add Empty Catalog"))
            {
                CatalogLoadInfoUnity newCatalog = new CatalogLoadInfoUnity();
                m_target.m_catalogsToLoad.Add(newCatalog);
                EditorUtility.SetDirty(m_target);
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Verifies whether the given TextAsset is a valid catalog JSON file by attempting to parse it.
        /// </summary>
        /// <param name="textAsset">The TextAsset to validate.</param>
        /// <returns>True if valid and parsable as <see cref="AssetCatalogData"/>; otherwise false.</returns>
        private bool IsValidCatalogJson(TextAsset textAsset)
        {
            if (textAsset == null)
                return false;
            try
            {
                AssetCatalogData testData = JsonUtility.FromJson<AssetCatalogData>(textAsset.text);
                return testData != null && testData.entries != null;
            }
            catch { return false; }
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Ride.Samples.Editor
{
    [CustomEditor(typeof(SamplesCoreRideAssetBundle))]
    public class SamplesCoreRideAssetBundleEditor : UnityEditor.Editor
    {
        private SerializedProperty m_catalogsToLoad;
        private SerializedProperty m_assetsToLoad;

        private void OnEnable()
        {
            m_catalogsToLoad = serializedObject.FindProperty("m_catalogsToLoad");
            m_assetsToLoad = serializedObject.FindProperty("m_assetsToLoad");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Catalogs To Load", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            DrawCatalogsSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(m_assetsToLoad, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCatalogsSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 60.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag catalog .json files here", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);

            EditorGUILayout.Space(5);

            for (int i = 0; i < m_catalogsToLoad.arraySize; i++)
            {
                var element = m_catalogsToLoad.GetArrayElementAtIndex(i);
                var catalogJsonFile = element.FindPropertyRelative("catalogJsonFile");
                var catalogPath = element.FindPropertyRelative("catalogPath");
                var isRemote = element.FindPropertyRelative("isRemote");

                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Catalog " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    m_catalogsToLoad.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.PropertyField(catalogJsonFile, new GUIContent("Catalog JSON File"));

                if (catalogJsonFile.objectReferenceValue == null)
                {
                    EditorGUILayout.PropertyField(catalogPath, new GUIContent("Catalog Path"));
                    EditorGUILayout.PropertyField(isRemote, new GUIContent("Is Remote"));
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (GUILayout.Button("Add Empty Catalog"))
            {
                m_catalogsToLoad.arraySize++;
                var newElement = m_catalogsToLoad.GetArrayElementAtIndex(m_catalogsToLoad.arraySize - 1);
                newElement.FindPropertyRelative("catalogJsonFile").objectReferenceValue = null;
                newElement.FindPropertyRelative("catalogPath").stringValue = "";
                newElement.FindPropertyRelative("isRemote").boolValue = false;
            }
            EditorGUILayout.EndVertical();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is TextAsset textAsset)
                        {
                            try
                            {
                                JsonUtility.FromJson<AssetCatalogData>(textAsset.text);

                                m_catalogsToLoad.arraySize++;
                                var newElement = m_catalogsToLoad.GetArrayElementAtIndex(m_catalogsToLoad.arraySize - 1);
                                newElement.FindPropertyRelative("catalogJsonFile").objectReferenceValue = textAsset;
                                newElement.FindPropertyRelative("catalogPath").stringValue = "";
                                newElement.FindPropertyRelative("isRemote").boolValue = false;
                            }
                            catch
                            {
                                Debug.LogWarning($"Skipped file (invalid catalog structure): {textAsset.name}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Skipped file (not a TextAsset): " + draggedObject.name);
                        }
                    }

                    evt.Use();
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    /// <summary>
    /// Displays a set of toggles for spawnable unit prefabs and emits an event when the user places one in the world.
    /// </summary>
    public class SpawnAgentUI : MenuUnity
    {
        /// <summary>
        /// Represents a callback invoked when the user chooses a unit prefab and a spawn position.
        /// </summary>
        /// <param name="unitPrefab">The prefab to spawn.</param>
        /// <param name="spawnPos">The world-space spawn position.</param>
        public delegate void SpawnUnit(GameObject unitPrefab, RideVector3 spawnPos);

        /// <summary>Raised when a spawnable unit has been selected and placed in the world.</summary>
        public static event SpawnUnit spawnUnitEvent;

        [Tooltip("Toggle group that manages the mutually exclusive spawn options.")]
        [SerializeField]
        ToggleGroup togGroup = null;

        [Tooltip("Template toggle used to create one spawn option per available unit prefab.")]
        [SerializeField]
        RideToggle firstTog = null;

        Dictionary<RideToggle, Component> toggleToUnitMap = new();

        bool setup = false;


        /// <summary>Validates the required toggle references and hides the template toggle until options are generated.</summary>
        void Awake()
        {
            if (togGroup == null || firstTog == null)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                firstTog.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Builds one toggle button for each prefab that can be spawned from this UI.
        /// </summary>
        /// <typeparam name="T">The component type used to identify the spawnable prefabs.</typeparam>
        /// <param name="prefabList">The prefabs to expose as spawn options.</param>
        public void SetSpawnUnitButtons<T>(T[] prefabList) where T : Component
        {
            if (setup)
                return;

            setup = true;

            if (prefabList != null)
            {
                var unitPrefabs = new List<T>(prefabList);

                float togHeight = firstTog.GetComponent<RectTransform>().rect.height;
                for (int i = 0; i < unitPrefabs.Count; i++)
                {
                    //RideToggle currTog = (i == 0) ? firstTog : Instantiate(firstTog, firstTog.transform.parent);
                    RideToggle currTog = Instantiate(firstTog, firstTog.transform.parent);
                    currTog.gameObject.SetActive(true);
                    currTog.transform.GetComponentInChildren<RideText>().text = unitPrefabs[i].name;
                    currTog.transform.localPosition = new Vector3(
                        currTog.transform.localPosition.x, 
                        currTog.transform.localPosition.y - (togHeight * i), 
                        currTog.transform.localPosition.z);
                    toggleToUnitMap.Add(currTog, unitPrefabs[i]);
                }
            }
        }

        /// <summary>
        /// Detects placement clicks and raises the spawn event for the currently selected unit prefab.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (Input.GetMouseButtonUp(0) && !RideUtils.IsMouseOverUI() && togGroup != null)
            {
                // Handle Spawn Unit
                Component prefab = GetToggledUnitPrefab<Component>();

                if (prefab == null)
                    return;

                togGroup.SetAllTogglesOff();

                RideRay ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Systems.Terrain != null)
                {
                    if (Systems.Terrain.RaycastTerrain(ray, out RideRaycastHit hit))
                        spawnUnitEvent?.Invoke(prefab.gameObject, hit.point);
                }
                else
                {
                    if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, Systems.Terrain.GetTerrainMask().value))
                        spawnUnitEvent?.Invoke(prefab.gameObject, hitInfo.point);
                }
            }
        }

        /// <summary>
        /// Returns the prefab associated with the currently toggled spawn option.
        /// </summary>
        /// <typeparam name="T">The component type expected from the stored prefab mapping.</typeparam>
        /// <returns>The selected prefab component, or <c>null</c> if nothing is selected.</returns>
        T GetToggledUnitPrefab<T>() where T : Component
        {
            foreach (RideToggle tog in toggleToUnitMap.Keys)
            {
                if (tog.isOn)
                    return (T)toggleToUnitMap[tog];
            }

            return null;
        }
    }
}

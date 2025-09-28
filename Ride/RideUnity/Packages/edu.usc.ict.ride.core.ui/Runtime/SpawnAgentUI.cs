using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    public class SpawnAgentUI : MenuMono
    {
        public delegate void SpawnUnit(GameObject unitPrefab, RideVector3 spawnPos);

        public static event SpawnUnit spawnUnitEvent;

        [SerializeField]
        ToggleGroup togGroup = null;

        [SerializeField]
        RideToggle firstTog = null;

        Dictionary<RideToggle, Component> toggleToUnitMap = new Dictionary<RideToggle, Component>();

        bool setup = false;

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

        public void SetSpawnUnitButtons<T>(T[] prefabList) where T : Component
        {
            if (setup)
                return;
            setup = true;

            if (prefabList != null)
            {
                List<T> unitPrefabs = new List<T>(prefabList);

                float togHeight = firstTog.GetComponent<RectTransform>().rect.height;
                for (int i = 0; i < unitPrefabs.Count; i++)
                {
                    //RideToggle currTog = (i == 0) ? firstTog : Instantiate(firstTog, firstTog.transform.parent);
                    RideToggle currTog = Instantiate(firstTog, firstTog.transform.parent);
                    currTog.gameObject.SetActive(true);
                    currTog.transform.GetComponentInChildren<RideText>().text = unitPrefabs[i].name;
                    currTog.transform.localPosition = new Vector3(currTog.transform.localPosition.x, currTog.transform.localPosition.y - (togHeight * i), currTog.transform.localPosition.z);
                    toggleToUnitMap.Add(currTog, unitPrefabs[i]);
                }
            }
        }

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
                if (Globals.api != null)
                {
                    if (Globals.api.terrainSystem.RaycastTerrain(ray, out RideRaycastHit hit))
                    {
                        spawnUnitEvent?.Invoke(prefab.gameObject, hit.point);
                    }
                }
                else
                {
                    if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, Globals.api.terrainSystem.GetTerrainMask().value))
                    {
                        spawnUnitEvent?.Invoke(prefab.gameObject, hitInfo.point);
                    }
                }
            }
        }

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

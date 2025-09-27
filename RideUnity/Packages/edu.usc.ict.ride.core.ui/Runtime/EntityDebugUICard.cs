using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    public class EntityDebugUICard : RideScreenUIElement
    {
        public override bool isInteractable { get => gameObject.activeSelf; set => gameObject.SetActive(value); }

        public RideID attachedRideObjId { get; set; } = RideID.Null;

        [SerializeField] RectTransform unitDataPoint;

        Dictionary<string, RectTransform> unitDataPointList = new Dictionary<string, RectTransform>();

        protected override void Start()
        {
            base.Start();

            unitDataPoint.gameObject.SetActive(false);
        }

        public void UpdateDataPoint(string category, string value)
        {
            RectTransform dataPoint = null;
            if (unitDataPointList.ContainsKey(category))
                dataPoint = unitDataPointList[category];
            else
            {
                dataPoint = Instantiate(unitDataPoint, unitDataPoint.parent);
                dataPoint.gameObject.SetActive(true);
                unitDataPointList.Add(category, dataPoint);
            }

            dataPoint.transform.Find("Category").GetComponent<Text>().text = category + ":";
            dataPoint.transform.Find("Value").GetComponent<Text>().text = value;
        }

        void ClearDataPointList()
        {
            foreach(string dataPointCategory in unitDataPointList.Keys)
            {
                if (unitDataPointList[dataPointCategory] != unitDataPoint)
                    Destroy(unitDataPointList[dataPointCategory].gameObject);
            }

            unitDataPointList.Clear();
        }

        protected override void Update()
        {
            if (attachedRideObjId != RideID.Null)
                MoveScreenElementToWorldPosition(Globals.api.GetSystem<ITransformSystem>().GetPosition(attachedRideObjId));
            else
                base.Update();
        }
    }
}
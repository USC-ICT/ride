using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    public class RideScreenUIElement : RideUIElement
    {
        public override bool isInteractable { get => gameObject.activeSelf; set => gameObject.SetActive(value); }

        protected GameObject attachedGameObject = null;
        protected ICameraSystem cameraSystem;
        protected RectTransform elementUITransform;
        protected Canvas myCnv = null;

        public GameObject AttachedGameObject { get => attachedGameObject; set => attachedGameObject = value; }

        protected override void Start()
        {
            base.Start();

            cameraSystem = Globals.api.cameraSystem;
            elementUITransform = GetComponent<RectTransform>();
        }

        public virtual void MoveScreenElementToWorldPosition(RideVector3 worldPos)
        {
            RideVector3 screenPos = cameraSystem.WorldToScreenPoint(worldPos);
            RideVector2 screenSize = new RideVector2(Screen.width, Screen.height);
            RideVector2 screenScaledPos = new RideVector2(screenPos.x / screenSize.x, screenPos.y / screenSize.y);
            Transform cnvTransform = transform.parent;
            Canvas cnv = cnvTransform.GetComponent<Canvas>();
            while (cnvTransform != null && cnv == null)
            {
                cnvTransform = transform.parent;
                cnv = cnvTransform.GetComponent<Canvas>();
            }

            if (cnv != null)
            {
                RideVector2 canvasSize = new RideVector2(cnv.GetComponent<RectTransform>().rect.width, cnv.GetComponent<RectTransform>().rect.height);
                RideVector2 canvasPos = new RideVector2(canvasSize.x * screenScaledPos.x, canvasSize.y * screenScaledPos.y);
                elementUITransform.anchoredPosition = new Vector2(canvasPos.x, canvasPos.y);

                if (myCnv == null)
                    myCnv = cnv;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (attachedGameObject != null)
                MoveScreenElementToWorldPosition(new RideVector3(attachedGameObject.transform.position));
        }
    }
}
using System;
using UnityEngine;

namespace Ride.UI
{
    public class GameObjectDraggable : RideUIElement, IDraggable
    {
        private bool interactable = true;

        public override bool isInteractable { get => interactable; set => interactable = value; }
        public bool IsDragging { get; set; } = false;

        public event EventHandler onDrag;
        public event EventHandler onDrop;
        public event EventHandler onDragging;

        public RideVector3 Position { get { return gameObject.transform.position; } set { gameObject.transform.position = value; } }

        protected override void Update()
        {
            if (interactable)
            {
                if (Globals.api != null && Globals.api.inputSystem != null)
                {
                    if (Globals.api.inputSystem.GetMouseButtonDown(0))
                    {
                        RideRay mouseRay = new RideRay(Camera.main.ScreenPointToRay(Globals.api.inputSystem.mousePosition.ToVector2()));
                        RideLayerMask mask = new RideLayerMask();
                        mask.value = RideLayerMask.GetMask("UI");
                        RideRaycastHit hitInfo = RideMath.GetRaycastHit(mouseRay.origin, mouseRay.direction, mask);
                        if (hitInfo.isHit)
                        {
                            if (ChildBelongToDraggable(hitInfo.transform))
                            {
                                IsDragging = true;
                                onDrag?.Invoke(this, new DragAndDropEventArgs(new RideVector3(hitInfo.point)));
                            }
                        }
                    }
                    else if (Globals.api.inputSystem.GetMouseButtonUp(0) && IsDragging)
                    {
                        RideVector3 droppedPosition = RideVector3.zero;
                        RideRay mouseRay = new RideRay(Camera.main.ScreenPointToRay(Globals.api.inputSystem.mousePosition.ToVector2()));
                        RideLayerMask mask = new RideLayerMask();
                        mask.value = ~RideLayerMask.GetMask("UI");
                        RideRaycastHit hitInfo = RideMath.GetRaycastHit(mouseRay.origin, mouseRay.direction, mask);
                        if (hitInfo.isHit)
                            droppedPosition = hitInfo.point;
                        IsDragging = false;
                        onDrop?.Invoke(this, new DragAndDropEventArgs(droppedPosition));
                    }
                }

                HandleDragging();
            }
        }

        void HandleDragging()
        {
            if (IsDragging)
            {
                RideRay mouseRay = new RideRay(Camera.main.ScreenPointToRay(Globals.api.inputSystem.mousePosition.ToVector2()));
                RideLayerMask mask = new RideLayerMask();
                mask.value = ~RideLayerMask.GetMask("UI");
                RideRaycastHit hitInfo = RideMath.GetRaycastHit(mouseRay.origin, mouseRay.direction, mask);
                if (hitInfo.isHit)
                    onDragging?.Invoke(this, new DragAndDropEventArgs(new RideVector3(hitInfo.point)));
            }
        }

        bool ChildBelongToDraggable(Transform child)
        {
            if (child.GetComponent<GameObjectDraggable>() == this)
                return true;
            else if (child.parent != null)
                return ChildBelongToDraggable(child.parent);

            return false;
        }
    }
}
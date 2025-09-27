using Ride;
using Ride.UI;
using Ride.WorldState;
using UnityEngine;

namespace Ride.UI
{
    public class BillboardIconToggle : RideToggle, IToggle, IBillboardIconToggle
    {
        public BillboardIcon billboardIcon;

        private bool interactable = true;

        public override bool isOn { get => (billboardIcon != null) ? billboardIcon.IsHighlighted : false; set => billboardIcon.IsHighlighted = value; }
        public override bool isInteractable { get => interactable; set => interactable = value; }

        /// <summary>
        /// 0: no hit
        /// 1: hit inside
        /// -1: hit outside
        /// </summary>
        int hitThisFrame = 0;
        public int HitThisFrame { get { return hitThisFrame; } }

        protected override void Update()
        {
            if (interactable)
            {
                if (Globals.api != null && Globals.api.inputSystem != null && Globals.api.inputSystem.GetMouseButtonUp(0))
                {
                    RideRay mouseRay = new RideRay(Camera.main.ScreenPointToRay(Globals.api.inputSystem.mousePosition.ToVector2()));
                    RideRaycastHit hitInfo = RideMath.GetRaycastHit(mouseRay.origin, mouseRay.direction, RideLayerMask.AllLayers);
                    if (hitInfo.isHit)
                    {
                        if (ChildBelongToToggle(hitInfo.transform)) {
                            isOn = !isOn;
                            hitThisFrame = 1;
                            Globals.api.worldStateSystem.DispatchEvent(WorldEvent.billboardSelected, new BillboardSelectedEvent(id));

                        }
                        else{
                            isOn = false;
                            hitThisFrame = -1;
                            Globals.api.worldStateSystem.DispatchEvent(WorldEvent.billboardUnselected, new BillboardUnselectedEvent(id));
                        }
                    }
                    else {
                        isOn = false;
                        hitThisFrame = -1;
                        Globals.api.worldStateSystem.DispatchEvent(WorldEvent.billboardUnselected, new BillboardUnselectedEvent(id));
                    }
                }
                else {
                    hitThisFrame = 0;
                }
            }
        }

        bool ChildBelongToToggle(Transform child)
        {
            if (child.GetComponent<BillboardIconToggle>() == this)
                return true;
            else if (child.parent != null)
                return ChildBelongToToggle(child.parent);

            return false;
        }
    }
}

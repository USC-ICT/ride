using UnityEngine;
using Ride.WorldState;

namespace Ride.UI
{
    /// <summary>
    /// Implements a world-space toggle backed by a <see cref="BillboardIcon"/> so users can select
    /// and unselect billboards with pointer input.
    /// </summary>
    public class BillboardIconToggle : RideToggle, IToggle, IBillboardIconToggle
    {
        [Tooltip("The billboard icon whose highlighted state is driven by this toggle.")]
        public BillboardIcon billboardIcon;

        private bool interactable = true;

        /// <summary>
        /// Tracks the click result for the current frame.
        /// 0 means no hit, 1 means the billboard was hit, and -1 means the click landed outside the billboard.
        /// </summary>
        private int hitThisFrame = 0;


        public override bool isOn { get => (billboardIcon != null) ? billboardIcon.IsHighlighted : false; set => billboardIcon.IsHighlighted = value; }
        public override bool isInteractable { get => interactable; set => interactable = value; }

        /// <summary>Gets the click result recorded for the current frame.</summary>
        public int HitThisFrame => hitThisFrame;


        /// <summary>
        /// Processes pointer input to toggle billboard selection and dispatch the corresponding world-state events.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (interactable)
            {
                if (Systems.Input != null && Systems.Input.GetMouseButtonUp(0))
                {
                    RideRay mouseRay = new RideRay(Camera.main.ScreenPointToRay(Systems.Input.mousePosition.ToVector2()));
                    RideRaycastHit hitInfo = RideMath.GetRaycastHit(mouseRay.origin, mouseRay.direction, RideLayerMask.AllLayers);
                    if (hitInfo.isHit)
                    {
                        if (ChildBelongToToggle(hitInfo.transform))
                        {
                            isOn = !isOn;
                            hitThisFrame = 1;
                            Systems.WorldState.DispatchEvent(WorldEvent.billboardSelected, new BillboardSelectedEvent(id));
                        }
                        else
                        {
                            isOn = false;
                            hitThisFrame = -1;
                            Systems.WorldState.DispatchEvent(WorldEvent.billboardUnselected, new BillboardUnselectedEvent(id));
                        }
                    }
                    else
                    {
                        isOn = false;
                        hitThisFrame = -1;
                        Systems.WorldState.DispatchEvent(WorldEvent.billboardUnselected, new BillboardUnselectedEvent(id));
                    }
                }
                else
                {
                    hitThisFrame = 0;
                }
            }
        }

        /// <summary>
        /// Determines whether the supplied transform belongs to this toggle or one of its child transforms.
        /// </summary>
        /// <param name="child">The transform to inspect.</param>
        /// <returns>True if the transform resolves back to this toggle; otherwise, false.</returns>
        private bool ChildBelongToToggle(Transform child)
        {
            if (child.GetComponent<BillboardIconToggle>() == this)
                return true;
            else if (child.parent != null)
                return ChildBelongToToggle(child.parent);

            return false;
        }
    }
}

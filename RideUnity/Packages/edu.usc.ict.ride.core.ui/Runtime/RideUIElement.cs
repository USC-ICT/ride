using UnityEngine;

namespace Ride.UI
{
    public abstract class RideUIElement : RideMonoBehaviour, IUIElement
    {
        public abstract bool isInteractable { get; set; }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }

        public virtual float RecalculateHeight()
        {
            return 1;
        }
    }
}

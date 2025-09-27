using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    [RequireComponent(typeof(RawImage))]
    public class RideRawImage : RideUIElement, IImage, IRawImage
    {
        public RawImage m_image;
        public override bool isInteractable { get => m_image.raycastTarget; set => m_image.raycastTarget = value; }
        public RideColor color { get => m_image.color; set => m_image.color = value; }
        public Texture texture { get => m_image.texture; set => m_image.texture = value; }
    }
}
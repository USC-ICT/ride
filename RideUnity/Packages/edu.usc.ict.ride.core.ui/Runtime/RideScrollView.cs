using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class RideScrollView : RideUIElement
    {
        private ScrollRect m_scrollRect;

        public override bool isInteractable { get; set; }
        public RectTransform content { get => m_scrollRect.content; set => m_scrollRect.content = value; }
        public bool horizontal { get => m_scrollRect.horizontal; set => m_scrollRect.horizontal = value; }
        public bool vertical { get => m_scrollRect.vertical; set => m_scrollRect.vertical = value; }
        public ScrollRect.MovementType movementType { get => m_scrollRect.movementType; set => m_scrollRect.movementType = value; }
        public float elasticity { get => m_scrollRect.elasticity; set => m_scrollRect.elasticity = value; }
        public bool inertia { get => m_scrollRect.inertia; set => m_scrollRect.inertia = value; }
        public float decerlationRate { get => m_scrollRect.decelerationRate; set => m_scrollRect.decelerationRate = value; }
        public float scrollSensitivity { get => m_scrollRect.scrollSensitivity; set => m_scrollRect.scrollSensitivity = value; }
        public RectTransform viewport { get => m_scrollRect.viewport; set => m_scrollRect.viewport = value; }
        public Scrollbar horizontalScrollbar { get => m_scrollRect.horizontalScrollbar; set => m_scrollRect.horizontalScrollbar = value; }
        public ScrollRect.ScrollbarVisibility horizontalVisibility { get => m_scrollRect.horizontalScrollbarVisibility; set => m_scrollRect.horizontalScrollbarVisibility = value;}
        public float horizontalSpacing { get => m_scrollRect.horizontalScrollbarSpacing; set => m_scrollRect.horizontalScrollbarSpacing = value; }
        public float horizontalValue { get => horizontalScrollbar.value; set => horizontalScrollbar.value = value; }
        public Scrollbar verticalScrollbar { get => m_scrollRect.verticalScrollbar; set => m_scrollRect.verticalScrollbar = value; }
        public ScrollRect.ScrollbarVisibility verticalVisibility { get => m_scrollRect.verticalScrollbarVisibility; set => m_scrollRect.verticalScrollbarVisibility = value; }
        public float verticalSpacing { get => m_scrollRect.verticalScrollbarSpacing; set => m_scrollRect.verticalScrollbarSpacing = value;}
        public float verticalValue { get => verticalScrollbar.value; set => verticalScrollbar.value = value; }
        protected override void Start()
        {
            base.Start();
            InitializeScrollView();
        }

        void InitializeScrollView()
        {
            m_scrollRect = GetComponent<ScrollRect>();
            scrollSensitivity = 25f;
        }
    }
}


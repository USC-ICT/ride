using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    /// <summary>
    /// Wrapper class for Unity LayoutGroups.
    /// </summary>
    [RequireComponent(typeof(LayoutGroup))]
    public class RideLayoutGroup : RideMonoBehaviour /*, IGroup,*/
    {
        private LayoutGroup m_layoutGroup;

        protected override void Start() => m_layoutGroup = GetComponent<LayoutGroup>();
    }
}

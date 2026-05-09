using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    /// <summary>
    /// Creates core Ride UI prefab instances for reusable menus and world-space UI elements.
    /// </summary>
    public class CoreUISystem : RideSystemMonoBehaviour, ICoreUISystem
    {
        [Tooltip("Prefab used when creating a new exit prompt menu instance.")]
        [SerializeField] ExitPromptMenu m_exitPromptMenu;
        [Tooltip("Prefab used when creating a new world-space text billboard instance.")]
        [SerializeField] WorldLabelBillboard m_worldLabelBillboard;
        [Tooltip("Prefab used when creating a new billboard icon toggle instance.")]
        [SerializeField] BillboardIconToggle m_billboardIconToggle;

        /// <summary>
        /// Instantiates the configured exit prompt menu prefab and returns it through the Ride UI interface.
        /// </summary>
        /// <returns>A new exit prompt menu instance.</returns>
        public IExitPromptMenu CreateExitPromptMenu()
        {
            var exitPromptMenu = Instantiate(m_exitPromptMenu);
            exitPromptMenu.name = exitPromptMenu.name.Replace("(Clone)", "");
            return exitPromptMenu;
        }

        /// <summary>
        /// Instantiates the configured world label billboard prefab and returns it through the Ride UI interface.
        /// </summary>
        /// <returns>A new world label billboard instance.</returns>
        public IWorldLabelBillboard CreateWorldLabelBillboard()
        {
            var worldLabelBillboard = Instantiate(m_worldLabelBillboard);
            worldLabelBillboard.name = worldLabelBillboard.name.Replace("(Clone)", "");
            return worldLabelBillboard;
        }

        /// <summary>
        /// Instantiates the configured billboard icon toggle prefab and returns it through the Ride UI interface.
        /// </summary>
        /// <returns>A new billboard icon toggle instance.</returns>
        public IBillboardIconToggle CreateBillboardIconToggle()
        {
            var billboardIconToggle = Instantiate(m_billboardIconToggle);
            billboardIconToggle.name = billboardIconToggle.name.Replace("(Clone)", "");
            return billboardIconToggle;
        }
    }
}

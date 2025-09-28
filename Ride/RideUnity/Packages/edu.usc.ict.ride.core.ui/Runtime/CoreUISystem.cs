using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    public class CoreUISystem : RideSystemMonoBehaviour, ICoreUISystem
    {
        [SerializeField] ExitPromptMenu m_exitPromptMenu;
        [SerializeField] WorldLabelBillboard m_worldLabelBillboard;
        [SerializeField] BillboardIconToggle m_billboardIconToggle;

        public IExitPromptMenu CreateExitPromptMenu()
        {
            var exitPromptMenu = Instantiate(m_exitPromptMenu);
            exitPromptMenu.name = exitPromptMenu.name.Replace("(Clone)", "");
            return exitPromptMenu;
        }

        public IWorldLabelBillboard CreateWorldLabelBillboard()
        {
            var worldLabelBillboard = Instantiate(m_worldLabelBillboard);
            worldLabelBillboard.name = worldLabelBillboard.name.Replace("(Clone)", "");
            return worldLabelBillboard;
        }

        public IBillboardIconToggle CreateBillboardIconToggle()
        {
            var billboardIconToggle = Instantiate(m_billboardIconToggle);
            billboardIconToggle.name = billboardIconToggle.name.Replace("(Clone)", "");
            return billboardIconToggle;
        }
    }
}

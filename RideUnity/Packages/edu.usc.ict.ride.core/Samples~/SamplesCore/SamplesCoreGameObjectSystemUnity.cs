using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreGameObjectSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        //GameObjectSystemUnity m_gameObjectSystem;
        string m_gameObjectName = "TestGameObject";


        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            //m_gameObjectSystem = Globals.api.GetSystem<GameObjectSystemUnity>();
        }

        public void OnGUIGameObject()
        {
            //m_debugMenu.DrawGUILabel($"SystemAccessSystem.GetSystem<SystemAccessSystem>():");
            //var testSystemAccess = m_systemAccessSystem.GetSystem<SystemAccessSystem>();
            //m_debugMenu.DrawGUILabel(testSystemAccess == null ? "Fail" : "Success");

            m_gameObjectName = m_debugMenu.TextField(m_gameObjectName);
            if (m_debugMenu.Button("Create GameObject"))
            {
                Globals.api.gameObjectSystem.Create(m_gameObjectName);

                // add to selectable list
            }

            // show selectable list of gameobjects created

            // button to call m_gameObjectSystem.SetName() on selected gameobject in list
            // button to call m_gameObjectSystem.SetActive() on selected gameobject in list
            // button to call m_gameObjectSystem.Destroy on selected gameobject in list
        }
    }
}

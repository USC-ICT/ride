using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreResourceLoaderSystem : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        ResourceLoaderSystem m_resourceLoader;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_resourceLoader = Globals.api.GetSystem<ResourceLoaderSystem>();
        }

        public void OnGUIResourceLoader()
        {
            if (m_debugMenu.Button("Create Sphere"))
                m_resourceLoader.InstantiateResource("Sphere", RideVector3.zero, RideQuaternion.identity);

            if (m_debugMenu.Button("Create Cube"))
                m_resourceLoader.InstantiateResource("Cube", RideVector3.zero, RideQuaternion.identity);

            m_debugMenu.Space();
            m_debugMenu.Label("<b>Available Resources:</b>");
            var resources = m_resourceLoader.GetAllResourceObjects();
            foreach (var resource in resources)
                m_debugMenu.Label(resource.name);

            m_debugMenu.Label("<b>Available AudioClips:</b>");
            //var resources = m_resourceLoader.GetAllResourceObjects();
            //foreach (var resource in resources)
            //    m_debugMenu.Label(resource.name);
        }
    }
}

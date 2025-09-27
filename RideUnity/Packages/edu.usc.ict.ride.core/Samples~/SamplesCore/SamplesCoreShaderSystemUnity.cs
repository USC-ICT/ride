using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreShaderSystemUnity : RideMonoBehaviour
    {
        public Material m_red;
        public Material m_blue;

        DebugMenu m_debugMenu;
        ShaderSystemUnity m_shaderSystem;

        MaterialUnity m_redMaterial;
        MaterialUnity m_blueMaterial;
        MaterialUnity m_currentMaterial;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_shaderSystem = Globals.api.GetSystem<ShaderSystemUnity>();

            m_redMaterial = new MaterialUnity(m_red);
            m_blueMaterial = new MaterialUnity(m_blue);

            //m_shaderSystem.AddMaterial(m_redMaterial, "samples");
            //m_shaderSystem.AddMaterial(m_blueMaterial, "samples");
        }

        public void OnGUIShaderSystem()
        {
            if (m_debugMenu.Button("Red Material"))
                Debug.Log("TODO");

            if (m_debugMenu.Button("Blue Material"))
                Debug.Log("TODO");

            if (m_debugMenu.Button("Clear"))
                Debug.Log("TODO");

            m_debugMenu.Space();
            m_debugMenu.Label("<b>Available Materials:</b>");
            foreach (var material in m_shaderSystem.GetMaterialsByFamily("samples"))
                m_debugMenu.Label("");

            m_debugMenu.Space();
            m_debugMenu.Label("<b>Current Material:</b>");

            if (m_currentMaterial != null)
            {
                Debug.Log("TODO");
                // draw quad in upper right showing material
                // show material statistics, color, texture, shader, etc
            }
        }
    }
}

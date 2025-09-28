using System.Collections;
using System.Collections.Generic;
using Ride.WorldState;

namespace Ride
{
    public class ShaderSystemUnity : RideSystemMonoBehaviour, IShaderSystem
    {
        Dictionary<RideID, IMaterial> m_materials = new();

        /// <summary>
        /// 8/14/2020.
        /// Material Property Blocks aren't working properly and sometimes result in a completely black texture.
        /// We have to make new materials for each terrain tile then. We now need a way to modify all the tiles'
        /// shader variables at once. We'll classify materials into 'families', where each family contains
        /// cloned materials that are all meant to be updated simultaneously.
        /// </summary>
        Dictionary<string, HashSet<RideID>> m_families = new Dictionary<string, HashSet<RideID>>();

        public RideID AddMaterial(IMaterial material, string family = null)
        {
            m_materials.Add(material.id, material);

            if (family != null)
            {
                if (!m_families.ContainsKey(family))
                    m_families.Add(family, new HashSet<RideID>());

                m_families[family].Add(id);
            }

            Globals.api.worldStateSystem?.DispatchEvent(WorldEvent.materialCreated, new MaterialAddedEvent(material.id));

            return material.id;
        }

        public IMaterial GetMaterial(RideID materialId)
        {
            if (!m_materials.TryGetValue(materialId, out IMaterial material))
                RideLog.LogError($"ShaderSystemUnity.GetMaterial() - material not found - {materialId}");

            //// when the go has been destroyed,
            //if ((agent != null && agent.ToString() == "null") || agent == null) {
            //    agent = null;
            //}

            return material;
        }

        public HashSet<RideID> GetMaterialsByFamily(string family)
        {
            if (!m_families.ContainsKey(family))
                return new HashSet<RideID>();
            else
                return m_families[family];
        }

        public RideColor GetMaterialColor(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetColor(key);
        }

        public RideVector4 GetMaterialVector(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetVector(key);
        }

        public float GetMaterialFloat(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetFloat(key);
        }

        public RideTexture GetMaterialTexture(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetTexture(key);
        }

        public void SetMaterialColor(RideID materialId, string key, RideColor color)
        {
            var material = GetMaterial(materialId);
            material.SetColor(key, color);
        }

        public void SetMaterialVector(RideID materialId, string key, RideVector4 vector)
        {
            var material = GetMaterial(materialId);
            material.SetVector(key, vector);
        }

        public void SetMaterialFloat(RideID materialId, string key, float value)
        {
            var material = GetMaterial(materialId);
            material.SetFloat(key, value);
        }

        public void SetMaterialTexture(RideID materialId, string key, RideTexture texture)
        {
            var material = GetMaterial(materialId);
            material.SetTexture(key, texture);
        }
    }
}

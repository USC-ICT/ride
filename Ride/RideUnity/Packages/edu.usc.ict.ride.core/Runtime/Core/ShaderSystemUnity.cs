using System.Collections;
using System.Collections.Generic;
using Ride.WorldState;

namespace Ride
{
    /// <summary>Registers RIDE materials and provides centralized access to their shader properties.</summary>
    /// <remarks>
    /// Materials can be grouped into named families so callers can locate related material instances that should be
    /// updated together. The Core sample <c>SamplesCoreShaderSystemUnity</c> demonstrates direct system access.
    /// </remarks>
    public class ShaderSystemUnity : RideSystemMonoBehaviour, IShaderSystem
    {
        /// <summary>The registered material adapters indexed by their RIDE identifiers.</summary>
        Dictionary<RideID, IMaterial> m_materials = new();

        /// <summary>
        /// Named groups of cloned material instances that are intended to receive coordinated shader updates.
        /// </summary>
        /// <remarks>
        /// Historical implementation note from August 14, 2020: Material Property Blocks were not working properly
        /// and sometimes resulted in a completely black texture. Separate materials therefore had to be created for
        /// each terrain tile, requiring a way to modify every tile's shader variables together. Materials were
        /// classified into families, with each family containing cloned materials intended to be updated
        /// simultaneously. This rationale has not yet been reverified and is retained until the material-family
        /// implementation can be reviewed.
        /// </remarks>
        Dictionary<string, HashSet<RideID>> m_families = new Dictionary<string, HashSet<RideID>>();

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public HashSet<RideID> GetMaterialsByFamily(string family)
        {
            if (!m_families.ContainsKey(family))
                return new HashSet<RideID>();
            else
                return m_families[family];
        }

        /// <inheritdoc/>
        public RideColor GetMaterialColor(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetColor(key);
        }

        /// <inheritdoc/>
        public RideVector4 GetMaterialVector(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetVector(key);
        }

        /// <inheritdoc/>
        public float GetMaterialFloat(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetFloat(key);
        }

        /// <inheritdoc/>
        public RideTexture GetMaterialTexture(RideID materialId, string key)
        {
            var material = GetMaterial(materialId);
            return material.GetTexture(key);
        }

        /// <inheritdoc/>
        public void SetMaterialColor(RideID materialId, string key, RideColor color)
        {
            var material = GetMaterial(materialId);
            material.SetColor(key, color);
        }

        /// <inheritdoc/>
        public void SetMaterialVector(RideID materialId, string key, RideVector4 vector)
        {
            var material = GetMaterial(materialId);
            material.SetVector(key, vector);
        }

        /// <inheritdoc/>
        public void SetMaterialFloat(RideID materialId, string key, float value)
        {
            var material = GetMaterial(materialId);
            material.SetFloat(key, value);
        }

        /// <inheritdoc/>
        public void SetMaterialTexture(RideID materialId, string key, RideTexture texture)
        {
            var material = GetMaterial(materialId);
            material.SetTexture(key, texture);
        }
    }
}

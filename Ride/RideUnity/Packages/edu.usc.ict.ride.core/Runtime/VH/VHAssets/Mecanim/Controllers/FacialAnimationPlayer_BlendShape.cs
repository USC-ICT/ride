using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride;

namespace VHAssets
{
    /// <summary>
    /// Facial animation player that drives blendshapes directly on one or more
    /// SkinnedMeshRenderer components under the character.
    /// 
    /// This implementation receives viseme weights from the base
    /// <see cref="FacialAnimationPlayer"/> and applies those weights to specific
    /// blendshapes using mapping data configured in the Inspector. Each viseme
    /// (for example: FV, PBM, ShCh, W, open) may map to one or more blendshape
    /// names, and each mapped blendshape is driven on every SkinnedMeshRenderer
    /// found under this GameObject that contains that blendshape name.
    /// 
    /// This allows a single viseme to affect multiple meshes (for example:
    /// face mesh, head mesh, beard mesh, teeth mesh) as long as the mesh
    /// contains a blendshape with the specified name.
    /// 
    /// Configuration:
    /// - The "Viseme - Blendshape Recipes" list defines the mapping from a viseme
    ///   name to one or more blendshape entries.
    /// - Each entry specifies a blendshape name and a per-entry multiplier.
    /// - At runtime, all SkinnedMeshRenderer components under this GameObject
    ///   are scanned. If a renderer contains a blendshape matching the entry's
    ///   name, it is added to the resolved mapping table.
    /// 
    /// Runtime Behavior:
    /// - When the lipsync system drives a viseme, <see cref="SetViseme"/> is invoked.
    /// - The viseme weight is multiplied by the global facial viseme multiplier
    ///   and any per-viseme modifier weight (inherited from the base class).
    /// - The final weight is applied to all resolved blendshape targets for that
    ///   viseme.
    /// - <see cref="GetViseme"/> returns the current weight of the first resolved
    ///   blendshape target for the viseme, or zero if none exist.
    /// 
    /// Assumptions:
    /// - Blendshape names must be identical across all SkinnedMeshRenderer
    ///   components where they should be driven.
    /// - Blendshape indices are resolved at startup. Adding or removing SMR
    ///   components at runtime is not supported.
    /// - The mapping list must contain valid blendshape names; otherwise warnings
    ///   will be logged during initialization.
    /// 
    /// Limitations:
    /// - Does not create or modify blendshape data; it only drives existing shapes.
    /// - Does not support editor-time previews unless Unity calls Start or the
    ///   mapping is manually rebuilt.
    /// 
    /// This class is meant for characters that use blendshape-driven facial
    /// animation instead of Animator Controller or Animation Clip based viseme
    /// playback.
    /// </summary>
    public class FacialAnimationPlayer_BlendShape : FacialAnimationPlayer
    {
        [Serializable]
        public class BlendShapeEntry
        {
            [Tooltip("Exact blendshape name on the mesh, e.g. \"F_V\", \"W_OO\", \"blendShape1.AA_VI_02_FF\".")]
            public string BlendShapeName;

            [Tooltip("Per-blendshape multiplier applied on top of the viseme weight and global multipliers.")]
            public float WeightMultiplier = 1f;

            public BlendShapeEntry(string blendShapeName)
            {
                BlendShapeName = blendShapeName;
                WeightMultiplier = 1f;
            }
        }

        [Serializable]
        public class VisemeBlendShapeMapping
        {
            [Tooltip("Viseme name coming from the lipsync system (e.g. \"FV\", \"PBM\", \"ShCh\"). Must match FaceShape or TTS/BML viseme keys.")]
            public string Viseme;

            [Tooltip("One or more blendshapes to drive when this viseme is active.")]
            public List<BlendShapeEntry> BlendShapes;

            public VisemeBlendShapeMapping(FaceShape viseme)
            {
                Viseme = ToVisemeName(viseme);
                BlendShapes = new List<BlendShapeEntry>() { new BlendShapeEntry(viseme.ToString()) };
            }
        }

        private struct ResolvedBlendShapeTarget
        {
            public SkinnedMeshRenderer Renderer;
            public int BlendShapeIndex;
            public float WeightMultiplier;

            public ResolvedBlendShapeTarget(SkinnedMeshRenderer renderer, int blendShapeIndex, float weightMultiplier) { Renderer = renderer; BlendShapeIndex = blendShapeIndex; WeightMultiplier = weightMultiplier; }
        }

        #region Fields
        [SerializeField]
        [Header("Viseme - Blendshape Recipes")]
        [Tooltip("Per-viseme recipes that map high-level viseme names (FV, PBM, etc.) to concrete blendshapes on one or more meshes.")]
        private List<VisemeBlendShapeMapping> m_visemeToBlendShapeMapping = new()
        {
            new(FaceShape.FV),
            new(FaceShape.open),
            new(FaceShape.PBM),
            new(FaceShape.ShCh),
            new(FaceShape.tBack),
            new(FaceShape.tRoof),
            new(FaceShape.tTeeth),
            new(FaceShape.W),
            new(FaceShape.wide),
            new(FaceShape.face_neutral),
        };

        // Cached: viseme name -> resolved targets (renderer + index + per-target multiplier).
        private readonly Dictionary<string, List<ResolvedBlendShapeTarget>> m_resolvedVisemeMappings = new(StringComparer.OrdinalIgnoreCase);
        #endregion


        void Start()
        {
            if (!TryGetComponent(out ILoadableAsset loadedAsset))
                InitializeLoadedAsset();
        }

        public void InitializeLoadedAsset()
        {
            BuildResolvedMappings();
        }

        protected override void SetViseme(string viseme, float weight)
        {
            if (!m_resolvedVisemeMappings.TryGetValue(viseme, out var targets) || targets == null || targets.Count == 0)
                return;

            // Apply global multipliers.
            float baseMultiplier = m_FacialVisemeMultiplier * GetVisemeModifierWeightMultiplier(viseme);
            float finalWeight = weight * baseMultiplier;

            foreach (var target in targets)
            {
                if (target.Renderer == null || target.Renderer.sharedMesh == null)
                    continue;

                float w = finalWeight * target.WeightMultiplier;
                w = Mathf.Clamp01(w) * 100f; // Convert to 0-100 range for blendshape weight.
                target.Renderer.SetBlendShapeWeight(target.BlendShapeIndex, w);
            }
        }

        protected override float GetViseme(string viseme)
        {
            if (!m_resolvedVisemeMappings.TryGetValue(viseme, out var targets) || targets == null || targets.Count == 0)
                return 0f;

            var target = targets[0];
            if (target.Renderer != null && target.Renderer.sharedMesh != null)
            {
                float w = target.Renderer.GetBlendShapeWeight(target.BlendShapeIndex);
                return Mathf.Clamp01(w / 100f); // Convert back to 0-1 space.
            }

            return 0f;
        }

        private void BuildResolvedMappings()
        {
            m_resolvedVisemeMappings.Clear();

            if (m_visemeToBlendShapeMapping == null || m_visemeToBlendShapeMapping.Count == 0)
                return;

            // Find all SkinnedMeshRenderers under this character.
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning($"FacialAnimationPlayer_BlendShape ({name}): No SkinnedMeshRenderer components found. Facial animations will not be visible.");
                return;
            }

            foreach (var mapping in m_visemeToBlendShapeMapping)
            {
                if (mapping == null)
                    continue;

                string viseme = mapping.Viseme;
                if (string.IsNullOrWhiteSpace(viseme))
                    continue;

                if (mapping.BlendShapes == null || mapping.BlendShapes.Count == 0)
                    continue;

                foreach (var blendShape in mapping.BlendShapes)
                {
                    if (blendShape == null)
                        continue;

                    string shapeName = blendShape.BlendShapeName;
                    if (string.IsNullOrEmpty(shapeName))
                        continue;

                    float weightMultiplier = Mathf.Max(0f, blendShape.WeightMultiplier);
                    bool foundOnAnyRenderer = false;

                    foreach (var renderer in renderers)
                    {
                        if (renderer == null || renderer.sharedMesh == null)
                            continue;

                        int index = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
                        if (index < 0)
                            continue;

                        foundOnAnyRenderer = true;

                        if (!m_resolvedVisemeMappings.TryGetValue(viseme, out var targets))
                        {
                            targets = new List<ResolvedBlendShapeTarget>();
                            m_resolvedVisemeMappings.Add(viseme, targets);
                        }

                        targets.Add(new ResolvedBlendShapeTarget(renderer, index, weightMultiplier));
                    }

                    if (!foundOnAnyRenderer)
                        Debug.LogWarning($"FacialAnimationPlayer_BlendShape ({name}): Blendshape '{shapeName}' for viseme '{viseme}' was not found on any SkinnedMeshRenderer under this GameObject.");
                }
            }
        }
    }
}

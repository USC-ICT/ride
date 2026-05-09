using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride;

namespace VHAssets
{
    /// <summary>
    /// Eyelid controller implementation that drives one or more blendshapes
    /// across all SkinnedMeshRenderer components under this GameObject.
    /// 
    /// The base <see cref="EyelidController"/> computes a normalized eyelid
    /// value in the range [0, 1], where 0 is fully open and 1 is fully closed.
    /// This class maps that logical lid value onto concrete blendshapes:
    /// 
    /// - You configure one or more blendshape names in the Inspector.
    /// - At startup, all SkinnedMeshRenderer components under this character
    ///   are scanned for those blendshape names.
    /// - Any renderer that contains a matching blendshape is added to an
    ///   internal list of targets, along with a per-entry weight multiplier.
    /// - Whenever the eyelid value changes, the final weight is applied to
    ///   every resolved blendshape target.
    /// 
    /// This is similar in spirit to <see cref="FacialAnimationPlayer_BlendShape"/>:
    /// a single logical channel (the eyelid) can drive multiple blendshapes on
    /// multiple meshes (face, head, eyelashes, etc.), as long as the meshes
    /// share the configured blendshape names.
    /// </summary>
    public class EyelidController_BlendShapes : EyelidController
    {
        public const string EyelidSideLeft = "045_blink_lf";
        public const string EyelidSideRight = "045_blink_rt";

        [Serializable]
        public class BlendShapeEntry
        {
            [Tooltip("Exact blendshape name on the mesh, e.g. \"UpperLid_L\", \"UpperLid_R\".")]
            public string BlendShapeName;

            [Tooltip("Per-blendshape multiplier applied on top of the eyelid value.")]
            public float WeightMultiplier = 1f;

            public BlendShapeEntry(string blendShapeName)
            {
                BlendShapeName = blendShapeName;
                WeightMultiplier = 1f;
            }
        }

        [Serializable]
        public class BlendShapeMapping
        {
            [Tooltip("Name that matches keys for the eyelids used in the system.")]
            public string Name;

            [Tooltip("One or more blendshapes to drive when this is active.")]
            public List<BlendShapeEntry> BlendShapes;

            public BlendShapeMapping(string name)
            {
                Name = name;
                BlendShapes = new List<BlendShapeEntry>() { new BlendShapeEntry(name) };
            }
        }

        private struct ResolvedBlendShapeTarget
        {
            public SkinnedMeshRenderer Renderer;
            public int BlendShapeIndex;
            public float WeightMultiplier;

            public ResolvedBlendShapeTarget(SkinnedMeshRenderer renderer, int blendShapeIndex, float weightMultiplier) { Renderer = renderer; BlendShapeIndex = blendShapeIndex; WeightMultiplier = weightMultiplier; }
        }

        [Header("Blend Shape Settings")]
        [Tooltip("Blendshape recipes for the eyelid channel. Each entry names a blendshape " +
                 "that will be driven on any SkinnedMeshRenderer under this GameObject " +
                 "that contains that blendshape.")]
        [SerializeField]
        private List<BlendShapeMapping> m_eyelidBlendShapeMapping = new()
        {   new(EyelidSideLeft),
            new(EyelidSideRight),
        };

        // Cached: key name -> resolved targets (renderer + index + per-target multiplier).
        private readonly Dictionary<string, List<ResolvedBlendShapeTarget>> m_resolvedTargets = new(StringComparer.OrdinalIgnoreCase);


        protected override void Start()
        {
            base.Start();

            if (!TryGetComponent(out ILoadableAsset loadedAsset))
                InitializeLoadedAsset();
        }

        public override void InitializeLoadedAsset()
        {
            base.InitializeLoadedAsset();

            BuildResolvedMappings();
        }

        public override void ResetLoadedAsset()
        {
            base.ResetLoadedAsset();

            // Best-effort: open lids (0) so we don't freeze in a closed pose
            // during the unload frame. This is safe even if renderers are null-checked.
            ApplyLid(0f);

            // Release cached renderer/index references so assets can unload.
            m_resolvedTargets.Clear();
        }

        /// <summary>
        /// Applies the final eyelid value to all resolved blendshape targets.
        /// The input <paramref name="lidValue"/> is expected to be in [0, 1],
        /// where 0 is fully open and 1 is fully closed.
        /// </summary>
        /// <param name="lidValue">Normalized eyelid closure value in [0, 1].</param>
        protected override void ApplyLid(float lidValue)
        {
            m_resolvedTargets.TryGetValue(EyelidSideLeft, out var targetsLeft);
            m_resolvedTargets.TryGetValue(EyelidSideRight, out var targetsRight);

            float clamped = Mathf.Clamp01(lidValue);

            if (targetsLeft != null)
                foreach (var target in targetsLeft)
                    SetBlendWeight(target, clamped);

            if (targetsRight != null)
                foreach (var target in targetsRight)
                    SetBlendWeight(target, clamped);
        }

        private void SetBlendWeight(ResolvedBlendShapeTarget target, float weight)
        {
            if (target.Renderer == null || target.Renderer.sharedMesh == null)
                return;

            float finalWeight = weight * target.WeightMultiplier;
            finalWeight = Mathf.Clamp01(finalWeight) * 100f; // Unity blendshape weights are 0..100.
            target.Renderer.SetBlendShapeWeight(target.BlendShapeIndex, finalWeight);
        }

        /// <summary>
        /// Scans all SkinnedMeshRenderer components under this GameObject and
        /// resolves which ones contain the configured blendshape names. The
        /// results are stored in <see cref="m_resolvedTargets"/>.
        /// </summary>
        private void BuildResolvedMappings()
        {
            m_resolvedTargets.Clear();

            if (m_eyelidBlendShapeMapping == null || m_eyelidBlendShapeMapping.Count == 0)
                return;

            // Find all SkinnedMeshRenderers under this character.
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning($"EyelidController_BlendShapes ({name}): No SkinnedMeshRenderer components found. Eyelid blendshapes will not be visible.");
                return;
            }

            foreach (var mapping in m_eyelidBlendShapeMapping)
            {
                if (mapping == null)
                    continue;

                string keyName = mapping.Name;
                if (string.IsNullOrEmpty(keyName))
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

                        if (!m_resolvedTargets.TryGetValue(keyName, out var targets))
                        {
                            targets = new List<ResolvedBlendShapeTarget>();
                            m_resolvedTargets.Add(keyName, targets);
                        }

                        targets.Add(new ResolvedBlendShapeTarget(renderer, index, weightMultiplier));
                    }

                    if (!foundOnAnyRenderer)
                        Debug.LogWarning($"EyelidController_BlendShapes ({keyName}): Blendshape '{shapeName}' was not found on any SkinnedMeshRenderer under this GameObject.");
                }
            }
        }

        /// <summary>
        /// Editor-only helper to assign a blendshape mapping recipe and rebuild resolved targets.
        /// Intended for prefab authoring time (e.g., CCCharacterSetupWindow).
        /// </summary>
        public void EditorSetBlendShapeMapping(List<BlendShapeMapping> mapping)
        {
            m_eyelidBlendShapeMapping = mapping ?? new List<BlendShapeMapping>();

            if (Application.isPlaying)
                BuildResolvedMappings();
        }
    }
}

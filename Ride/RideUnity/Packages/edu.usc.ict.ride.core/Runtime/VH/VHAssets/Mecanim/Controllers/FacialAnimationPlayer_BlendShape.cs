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

        private readonly struct BlendShapeTargetKey : IEquatable<BlendShapeTargetKey>
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly int BlendShapeIndex;

            public BlendShapeTargetKey(SkinnedMeshRenderer renderer, int blendShapeIndex)
            {
                Renderer = renderer;
                BlendShapeIndex = blendShapeIndex;
            }

            public bool Equals(BlendShapeTargetKey other) => Renderer == other.Renderer && BlendShapeIndex == other.BlendShapeIndex;
            public override bool Equals(object obj) => obj is BlendShapeTargetKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Renderer, BlendShapeIndex);
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
        // Cached: target -> visemes that can drive it. Used to resolve shared blendshapes safely.
        private readonly Dictionary<BlendShapeTargetKey, List<string>> m_targetToVisemes = new();
        // Cached: current effective viseme output after global multipliers, before per-target multipliers.
        private readonly Dictionary<string, float> m_currentVisemeWeights = new(StringComparer.OrdinalIgnoreCase);
        #endregion


        void Start()
        {
            if (!TryGetComponent(out ILoadableAsset loadedAsset))
                InitializeLoadedAsset();
        }

        public override void InitializeLoadedAsset()
        {
            base.InitializeLoadedAsset();

            BuildResolvedMappings();
        }

        /// <summary>
        /// Called (via SendMessage) by RideCatalogAsset when the loaded character asset
        /// is being reset/unloaded. This clears cached component references and disables
        /// runtime driving until InitializeLoadedAsset is invoked again.
        /// </summary>
        public override void ResetLoadedAsset()
        {
            base.ResetLoadedAsset();

            // Best-effort: reset any blendshapes we have been driving back to zero.
            // This avoids leaving the last driven pose stuck if the asset remains visible
            // for a frame during teardown.
            ResetDrivenBlendShapesToZero();

            // Clear cached renderer/index mappings so we do not hold references after unload.
            m_resolvedVisemeMappings.Clear();
            m_targetToVisemes.Clear();
            m_currentVisemeWeights.Clear();
        }

        protected override void SetViseme(string viseme, float weight)
        {
            // Apply global multipliers.
            float baseMultiplier = GetResolvedWeightMultiplier(viseme);
            float finalWeight = weight * baseMultiplier;
            m_currentVisemeWeights[viseme] = Mathf.Clamp01(finalWeight);

            if (!m_resolvedVisemeMappings.TryGetValue(viseme, out var targets) || targets == null || targets.Count == 0)
                return;

            foreach (var target in targets)
                ApplyResolvedTargetWeight(target);
        }

        protected override float GetViseme(string viseme)
        {
            return m_currentVisemeWeights.TryGetValue(viseme, out float weight) ? Mathf.Clamp01(weight) : 0f;
        }

        private void BuildResolvedMappings()
        {
            m_resolvedVisemeMappings.Clear();
            m_targetToVisemes.Clear();
            m_currentVisemeWeights.Clear();

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

                        var target = new ResolvedBlendShapeTarget(renderer, index, weightMultiplier);
                        targets.Add(target);

                        var key = new BlendShapeTargetKey(renderer, index);
                        if (!m_targetToVisemes.TryGetValue(key, out var drivingVisemes))
                        {
                            drivingVisemes = new List<string>();
                            m_targetToVisemes.Add(key, drivingVisemes);
                        }

                        drivingVisemes.Add(viseme);
                    }

                    if (!foundOnAnyRenderer)
                        Debug.LogWarning($"FacialAnimationPlayer_BlendShape ({name}): Blendshape '{shapeName}' for viseme '{viseme}' was not found on any SkinnedMeshRenderer under this GameObject.");
                }
            }
        }

        private void ResetDrivenBlendShapesToZero()
        {
            if (m_resolvedVisemeMappings == null || m_resolvedVisemeMappings.Count == 0)
                return;

            foreach (var kvp in m_resolvedVisemeMappings)
            {
                var targets = kvp.Value;
                if (targets == null)
                    continue;

                foreach (var target in targets)
                {
                    if (target.Renderer == null || target.Renderer.sharedMesh == null)
                        continue;

                    target.Renderer.SetBlendShapeWeight(target.BlendShapeIndex, 0f);
                }
            }
        }

        /// <summary>
        /// Resolves and applies the final weight for a single concrete blendshape target.
        ///
        /// A "target" here is one specific blendshape index on one specific
        /// <see cref="SkinnedMeshRenderer"/>. Multiple visemes may map to that same
        /// target through the Inspector-configured viseme recipes. In that case, the
        /// target should not be written independently by each viseme because a later
        /// zero-weight write could overwrite an earlier active write in the same frame.
        ///
        /// Instead, this method asks <see cref="ResolveTargetWeight"/> for the combined
        /// weight that should be realized on this target, then writes that resolved
        /// value once to the renderer in Unity's expected 0-100 blendshape range.
        ///
        /// For ordinary one-to-one mappings, this behaves the same as the previous
        /// direct-write implementation. The difference only matters when multiple
        /// visemes share the same underlying blendshape target.
        /// </summary>
        /// <param name="target">
        /// The resolved renderer/index pair to update, including its per-target
        /// weight multiplier from the Inspector mapping.
        /// </param>
        private void ApplyResolvedTargetWeight(ResolvedBlendShapeTarget target)
        {
            if (target.Renderer == null || target.Renderer.sharedMesh == null)
                return;

            float resolvedWeight = ResolveTargetWeight(target);
            target.Renderer.SetBlendShapeWeight(target.BlendShapeIndex, resolvedWeight * 100f);
        }

        /// <summary>
        /// Computes the final normalized weight for a single concrete blendshape target.
        ///
        /// This method exists to safely handle shared-target mappings, where two or
        /// more visemes resolve to the same <see cref="SkinnedMeshRenderer"/> and
        /// blendshape index. In the old direct-write model, each viseme wrote that
        /// target independently, so whichever viseme happened to write later would win,
        /// even if that later write was zero. That could cause inactive visemes to
        /// stomp active ones.
        ///
        /// The current approach resolves the target from all visemes that can drive it:
        /// - Look up every viseme associated with this concrete target.
        /// - Read each viseme's current effective weight from the runtime cache.
        /// - Re-apply the per-target multiplier for the matching mapping entry.
        /// - Take the maximum contribution as the final realized target weight.
        ///
        /// Using the maximum is intentionally conservative:
        /// - It preserves the previous behavior for one-to-one mappings.
        /// - It prevents a zero-weight viseme from clearing a target that another
        ///   active viseme still needs.
        /// - It avoids changing the base-class scheduling and expression logic.
        ///
        /// The returned value is normalized to [0, 1]. Callers are responsible for
        /// converting it to Unity's 0-100 blendshape weight space before writing.
        /// </summary>
        /// <param name="target">
        /// The concrete renderer/index pair whose realized output weight should be
        /// computed.
        /// </param>
        /// <returns>
        /// The final normalized weight in [0, 1] that should be applied to the target
        /// after considering all viseme mappings that share it.
        /// </returns>
        private float ResolveTargetWeight(ResolvedBlendShapeTarget target)
        {
            var key = new BlendShapeTargetKey(target.Renderer, target.BlendShapeIndex);
            if (!m_targetToVisemes.TryGetValue(key, out var drivingVisemes) || drivingVisemes == null || drivingVisemes.Count == 0)
                return 0f;

            float resolvedWeight = 0f;
            for (int i = 0; i < drivingVisemes.Count; i++)
            {
                string viseme = drivingVisemes[i];
                if (!m_resolvedVisemeMappings.TryGetValue(viseme, out var targets) || targets == null)
                    continue;

                float visemeWeight = m_currentVisemeWeights.TryGetValue(viseme, out float currentWeight) ? currentWeight : 0f;
                if (visemeWeight <= 0f)
                    continue;

                for (int j = 0; j < targets.Count; j++)
                {
                    var candidate = targets[j];
                    if (candidate.Renderer != target.Renderer || candidate.BlendShapeIndex != target.BlendShapeIndex)
                        continue;

                    resolvedWeight = Mathf.Max(resolvedWeight, Mathf.Clamp01(visemeWeight * candidate.WeightMultiplier));
                }
            }

            return resolvedWeight;
        }

        /// <summary>
        /// Editor-only helper to assign a blendshape mapping recipe and rebuild resolved targets.
        /// Intended for prefab authoring time (e.g., CCCharacterSetupWindow).
        /// </summary>
        public void EditorSetBlendShapeMapping(List<VisemeBlendShapeMapping> mapping)
        {
            m_visemeToBlendShapeMapping = mapping ?? new List<VisemeBlendShapeMapping>();

            if (Application.isPlaying)
                BuildResolvedMappings();
        }
    }
}

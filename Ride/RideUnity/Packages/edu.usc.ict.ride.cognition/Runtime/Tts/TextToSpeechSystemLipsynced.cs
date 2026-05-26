using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Abstract base class for TTS systems that support lipsync data output.
    /// </summary>
    public abstract class TextToSpeechSystemLipsynced : TextToSpeechSystemUnity, ILipsyncedTextToSpeechSystem
    {
        /// <summary>
        /// Captures the output of a viseme trimming pass along with before/after metrics
        /// that help explain what changed in debug output.
        /// </summary>
        /// <remarks>
        /// The trimming pipeline is intentionally conservative: the <see cref="Visemes"/>
        /// list always contains original provider entries that survived filtering, while
        /// the remaining fields summarize how much schedule density was removed.
        /// </remarks>
        protected struct VisemeTrimResult
        {
            public List<GenerateAudioReplyViseme> Visemes;
            public int OriginalEntryCount;
            public int TrimmedEntryCount;
            public int OriginalGroupCount;
            public int TrimmedGroupCount;
            public int OriginalZeroOnlyGroupCount;
            public int TrimmedZeroOnlyGroupCount;
            public int DenseClusterCount;
            public int LargestClusterGroupCount;
            public double AverageClusterGroupCount;
            public int RemovedGroupCount;
            public int RemovedEntryCount;
            public int ClusterBreaksBySpan;
            public int ClusterBreaksByAdjacentGap;
            public double OriginalMinimumGapSeconds;
            public double TrimmedMinimumGapSeconds;
        }

        /// <summary>
        /// Represents one timestamp-grouped viseme pose assembled from adjacent viseme
        /// entries that share the same start time.
        /// </summary>
        /// <remarks>
        /// Grouping by timestamp lets the trimming pipeline remove chatter at the pose
        /// level instead of deleting individual viseme components. That keeps any surviving
        /// pose faithful to the original provider output while still allowing dense
        /// timestamp clusters to be reduced.
        /// </remarks>
        private struct VisemeGroup
        {
            public double StartTime;
            public int FirstSourceIndex;
            public List<GenerateAudioReplyViseme> Entries;
            public double TotalWeight;
            public double PeakWeight;
            public int NonZeroCount;
        }

        [field: Header("Lipsync Status")]
        [field: SerializeField] public string lipsyncSchedule { get; private set; }
        [field: SerializeField] public bool lipsyncProcessing { get; private set; } = false;
        [Header("Lipsync Trimming")]
        [Tooltip("When enabled, viseme timestamp groups that fall within the configured cluster span are merged using simple shape-aware rules. The merged group uses the strongest original group's time, keeps max weights for important outer-mouth shapes, and only retains strong internal tongue shapes.")]
        [SerializeField] private bool m_TrimMergeDenseClustersUsingShapeRules = false;
        [Tooltip("Maximum time span, in seconds, covered by one merged viseme cluster. Smaller values preserve more timing detail; larger values merge more nearby viseme activity into one reduced cluster.")]
        [SerializeField, Min(0f)] private float m_TrimClusterSpanSeconds = 0.1f;
        [field: Header("Lipsync Debug")]
        [field: SerializeField] public bool lipsyncDebugOutput { get; protected set; } = false;

        // Internal clustering guard: limits how large an adjacent gap can be, relative
        // to m_TrimClusterSpanSeconds, before a new cluster must begin.
        private const double TrimAdjacentGapToClusterSpanRatio = 0.8;


        /// <inheritdoc/>
        public void CreateTextToSpeech(string voice, string text, LipsyncedTextToSpeechResult resultCallback)
        {
            if (!ContainsVoice(voice))
            {
                Debug.Log("Voice not found!");
                return;
            }

            if (textToSpeechProcessing || lipsyncProcessing)
            {
                Debug.Log("TTS already in progress!");
                return;
            }

            InitalizeTTSVariables();

            StartTextToSpeechGeneration(voice, text);
            StartLipsyncGeneration(voice, text);

            StartCoroutine(WaitForLipsyncedTextToSpeech(resultCallback));
        }

        /// <inheritdoc/>
        protected override void InitalizeTTSVariables()
        {
            base.InitalizeTTSVariables();
            lipsyncSchedule = "";
            lipsyncProcessing = true;
        }

        /// <summary>
        /// Signals that lipsync data generation is complete.
        /// </summary>
        /// <param name="lipsyncSchedule">The generated lipsync data (e.g. XML string).</param>
        protected void CompleteLipsyncGeneration(string lipsyncSchedule)
        {
            this.lipsyncSchedule = lipsyncSchedule;
            lipsyncProcessing = false;
        }

        /// <summary>
        /// Starts the lipsync data generation for the provided voice and text.
        /// Must be implemented by child classes.
        /// </summary>
        /// <param name="voice">The selected voice.</param>
        /// <param name="text">The input text to convert to speech.</param>
        protected abstract void StartLipsyncGeneration(string voice, string text);

        /// <summary>
        /// Emits shared lipsync debug output for the supplied map and generated XML.
        /// </summary>
        protected void LogSpeechXmlDebug(AudioSpeechMap map, string xml, string debugLabel)
        {
            if (!lipsyncDebugOutput)
                return;

            string label = string.IsNullOrEmpty(debugLabel) ? GetType().Name : debugLabel;
            Debug.Log($"[{label} XML Visemes] count={(map?.VisemeList?.Count ?? 0)}{Environment.NewLine}{FormatAudioSpeechMapVisemesForDebug(map)}");
            Debug.Log($"[{label} XML Words/Marks]{Environment.NewLine}{FormatAudioSpeechMapWordMarksForDebug(map)}");
            Debug.Log($"[{label} XML Parsed Visemes]{Environment.NewLine}{FormatXmlVisemesForDebug(xml)}");
        }

        private static string FormatAudioSpeechMapVisemesForDebug(AudioSpeechMap map)
        {
            if (map == null || map.VisemeList == null || map.VisemeList.Count == 0)
                return "<none>";

            StringBuilder sb = new();
            int i = 0;
            while (i < map.VisemeList.Count)
            {
                double start = map.VisemeList[i].start;
                StringBuilder line = new();
                line.Append(start.ToString("0.000", CultureInfo.InvariantCulture));
                line.Append("  ");

                int hiddenZeroCount = 0;
                bool wroteAnyNonZero = false;
                while (i < map.VisemeList.Count && Math.Abs(map.VisemeList[i].start - start) <= 0.0000001)
                {
                    double articulation = map.VisemeList[i].articulation;
                    if (Math.Abs(articulation) <= 0.0000001)
                    {
                        hiddenZeroCount++;
                        i++;
                        continue;
                    }

                    if (!wroteAnyNonZero)
                        wroteAnyNonZero = true;
                    else
                        line.Append(", ");

                    line.Append(map.VisemeList[i].type);
                    line.Append('=');
                    line.Append(articulation.ToString("0.00", CultureInfo.InvariantCulture));
                    i++;
                }

                if (hiddenZeroCount > 0)
                    line.Insert(line.Length > 0 ? line.ToString().IndexOf("  ", StringComparison.Ordinal) + 2 : line.Length, $"+{hiddenZeroCount} zero  ");

                if (!wroteAnyNonZero)
                    line.Append("<all zero>");

                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append(line);
            }

            return sb.ToString();
        }

        private static string FormatAudioSpeechMapWordMarksForDebug(AudioSpeechMap map)
        {
            if (map == null)
                return "<none>";

            StringBuilder sb = new();

            sb.Append("Words");
            if (map.WordTimingList == null || map.WordTimingList.Count == 0)
            {
                sb.Append(": <none>");
            }
            else
            {
                for (int i = 0; i < map.WordTimingList.Count; i++)
                {
                    var word = map.WordTimingList[i];
                    sb.AppendLine();
                    sb.Append($"  {i:00}: {word.Start.ToString("0.000", CultureInfo.InvariantCulture)}-{word.End.ToString("0.000", CultureInfo.InvariantCulture)}");
                    if (!string.IsNullOrEmpty(word.Text))
                        sb.Append($" '{word.Text}'");
                }
            }

            sb.AppendLine();
            sb.Append("Marks");
            if (map.MarkList == null || map.MarkList.Count == 0)
            {
                sb.Append(": <none>");
            }
            else
            {
                for (int i = 0; i < map.MarkList.Count; i++)
                {
                    var mark = map.MarkList[i];
                    sb.AppendLine();
                    sb.Append($"  {mark.Key}: {mark.Value.ToString("0.000", CultureInfo.InvariantCulture)}");
                }
            }

            return sb.ToString();
        }

        private static string FormatXmlVisemesForDebug(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return "<none>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var visemeNodes = doc.SelectNodes("/speak//viseme");
            if (visemeNodes == null || visemeNodes.Count == 0)
                return "<none>";

            StringBuilder sb = new();
            double? currentTime = null;
            StringBuilder line = null;
            int hiddenZeroCount = 0;
            bool wroteAnyNonZero = false;

            foreach (XmlNode node in visemeNodes)
            {
                if (node.Attributes == null)
                    continue;

                string startText = node.Attributes["start"]?.Value;
                string type = node.Attributes["type"]?.Value;
                string articulationText = node.Attributes["articulation"]?.Value;

                if (!double.TryParse(startText, NumberStyles.Float, CultureInfo.InvariantCulture, out double start))
                    continue;

                if (!double.TryParse(articulationText, NumberStyles.Float, CultureInfo.InvariantCulture, out double articulation))
                    articulation = 0.0;

                if (!currentTime.HasValue || Math.Abs(currentTime.Value - start) > 0.0000001)
                {
                    if (line != null)
                    {
                        if (hiddenZeroCount > 0)
                            line.Insert(line.Length > 0 ? line.ToString().IndexOf("  ", StringComparison.Ordinal) + 2 : line.Length, $"+{hiddenZeroCount} zero  ");

                        if (!wroteAnyNonZero)
                            line.Append("<all zero>");

                        if (sb.Length > 0)
                            sb.AppendLine();
                        sb.Append(line);
                    }

                    currentTime = start;
                    line = new StringBuilder();
                    line.Append(start.ToString("0.000", CultureInfo.InvariantCulture));
                    line.Append("  ");
                    hiddenZeroCount = 0;
                    wroteAnyNonZero = false;
                }

                if (Math.Abs(articulation) <= 0.0000001)
                {
                    hiddenZeroCount++;
                    continue;
                }

                if (wroteAnyNonZero)
                    line.Append(", ");
                else
                    wroteAnyNonZero = true;

                line.Append(type);
                line.Append('=');
                line.Append(articulation.ToString("0.00", CultureInfo.InvariantCulture));
            }

            if (line != null)
            {
                if (hiddenZeroCount > 0)
                    line.Insert(line.Length > 0 ? line.ToString().IndexOf("  ", StringComparison.Ordinal) + 2 : line.Length, $"+{hiddenZeroCount} zero  ");

                if (!wroteAnyNonZero)
                    line.Append("<all zero>");

                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(line);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Applies the currently configured shared viseme trimming rules to the supplied
        /// <see cref="AudioSpeechMap"/>, if any trim rules are enabled for this provider
        /// instance.
        /// </summary>
        /// <param name="map">
        /// The speech map whose <see cref="AudioSpeechMap.VisemeList"/> should be trimmed
        /// before XML generation.
        /// </param>
        /// <param name="debugLabel">
        /// Provider label used when emitting trim summary debug output.
        /// </param>
        /// <remarks>
        /// This helper centralizes the inspector-driven trimming path for all derived
        /// lipsynced TTS providers. It reads the current serialized settings, runs the
        /// shared trim pipeline, logs a compact before/after summary when debug output is
        /// enabled, and then writes the resulting viseme sequence back onto the map.
        /// </remarks>
        protected void ApplyConfiguredVisemeTrimming(AudioSpeechMap map, string debugLabel)
        {
            if (map == null || map.VisemeList == null || map.VisemeList.Count == 0)
                return;

            VisemeTrimResult trimResult = TrimVisemeSequence(map.VisemeList);

            LogVisemeTrimDebug(debugLabel, trimResult);

            map.VisemeList = trimResult.Visemes;
        }

        /// <summary>
        /// Shared entry point for conservative viseme schedule trimming. This currently
        /// performs dense-cluster trimming using original timestamp groups, so it reduces
        /// schedule chatter without synthesizing new poses or reinterpreting the data.
        /// </summary>
        /// <param name="visemes">
        /// The original viseme sequence to evaluate. Entries are expected to already be
        /// grouped logically by identical timestamp when emitted by the provider.
        /// </param>
        /// <returns>
        /// A <see cref="VisemeTrimResult"/> containing the surviving viseme sequence along
        /// with before/after metrics useful for runtime debug logging.
        /// </returns>
        /// <remarks>
        /// The intent of this entry point is to provide a single shared orchestration layer
        /// for provider viseme cleanup. The current implementation focuses on conservative
        /// trimming only: it removes dense timestamp-group chatter while collapsing those
        /// dense regions into one merged representative timestamp group. Future rules can
        /// be added here without changing the external call sites in each provider
        /// implementation.
        /// </remarks>
        protected VisemeTrimResult TrimVisemeSequence(IReadOnlyList<GenerateAudioReplyViseme> visemes)
        {
            double clusterSpanSeconds = Math.Max(0.0, m_TrimClusterSpanSeconds);
            VisemeTrimResult trimResult = CreatePassThroughTrimResult(visemes, clusterSpanSeconds);
            IReadOnlyList<GenerateAudioReplyViseme> workingVisemes = trimResult.Visemes;

            if (clusterSpanSeconds > 0.0 && m_TrimMergeDenseClustersUsingShapeRules)
            {
                trimResult = MergeDenseVisemeGroupsUsingShapeRules(workingVisemes, clusterSpanSeconds);
                workingVisemes = trimResult.Visemes;
            }

            return trimResult;
        }

        /// <summary>
        /// Merges densely packed viseme timestamp groups into one reduced timestamp group
        /// using simple shape-aware rules.
        /// </summary>
        /// <param name="visemes">
        /// The original flat viseme sequence to trim.
        /// </param>
        /// <param name="clusterSpanSeconds">
        /// The maximum time span allowed for one merged viseme cluster.
        /// </param>
        /// <returns>
        /// A populated <see cref="VisemeTrimResult"/> describing both the surviving
        /// viseme entries and the before/after trim metrics.
        /// </returns>
        /// <remarks>
        /// A cluster starts at one viseme timestamp group and grows forward while later
        /// groups stay within the supplied cluster span. An additional adjacency guard
        /// prevents large local jumps from being folded into the same cluster. The merged
        /// group uses the strongest original group's timestamp, keeps the maximum
        /// articulation seen for each viseme across the cluster, and then applies a simple
        /// shape filter: visually important outer mouth shapes are preserved, while lower-
        /// visibility internal tongue shapes are only retained when they are strong enough
        /// to matter on their own.
        /// </remarks>
        private static VisemeTrimResult MergeDenseVisemeGroupsUsingShapeRules(IReadOnlyList<GenerateAudioReplyViseme> visemes, double clusterSpanSeconds)
        {
            var result = new VisemeTrimResult
            {
                OriginalEntryCount = visemes?.Count ?? 0,
                Visemes = new List<GenerateAudioReplyViseme>()
            };

            if (visemes == null || visemes.Count == 0)
                return result;

            if (clusterSpanSeconds <= 0.0)
                return CreatePassThroughTrimResult(visemes, clusterSpanSeconds);

            List<VisemeGroup> groups = BuildVisemeGroups(visemes);
            result.OriginalGroupCount = groups.Count;
            result.OriginalZeroOnlyGroupCount = CountZeroOnlyGroups(groups);
            result.OriginalMinimumGapSeconds = ComputeMinimumGapSeconds(groups);

            var trimmed = new List<GenerateAudioReplyViseme>(visemes.Count);
            int totalClusterGroupCount = 0;
            int clusterStartIndex = 0;
            while (clusterStartIndex < groups.Count)
            {
                int clusterEndIndex = clusterStartIndex;
                double clusterStartTime = groups[clusterStartIndex].StartTime;
                double maximumAdjacentGapSeconds = clusterSpanSeconds * TrimAdjacentGapToClusterSpanRatio;
                while (clusterEndIndex + 1 < groups.Count)
                {
                    double nextGroupStartTime = groups[clusterEndIndex + 1].StartTime;
                    double adjacentGapSeconds = nextGroupStartTime - groups[clusterEndIndex].StartTime;
                    double clusterSpan = nextGroupStartTime - clusterStartTime;

                    if (clusterSpan > clusterSpanSeconds)
                    {
                        result.ClusterBreaksBySpan++;
                        break;
                    }

                    if (maximumAdjacentGapSeconds > 0.0 && adjacentGapSeconds > maximumAdjacentGapSeconds)
                    {
                        result.ClusterBreaksByAdjacentGap++;
                        break;
                    }

                    clusterEndIndex++;
                }

                if (clusterEndIndex > clusterStartIndex)
                {
                    result.DenseClusterCount++;
                    int clusterGroupCount = clusterEndIndex - clusterStartIndex + 1;
                    totalClusterGroupCount += clusterGroupCount;
                    if (clusterGroupCount > result.LargestClusterGroupCount)
                        result.LargestClusterGroupCount = clusterGroupCount;
                }

                int strongestGroupIndex = ChooseStrongestGroupIndex(groups, clusterStartIndex, clusterEndIndex);
                trimmed.AddRange(BuildMergedClusterEntries(groups, clusterStartIndex, clusterEndIndex, strongestGroupIndex));

                clusterStartIndex = clusterEndIndex + 1;
            }

            if (result.DenseClusterCount > 0)
                result.AverageClusterGroupCount = totalClusterGroupCount / (double)result.DenseClusterCount;

            result.Visemes = trimmed;
            result.TrimmedEntryCount = trimmed.Count;
            result.TrimmedGroupCount = CountDistinctVisemeStartGroups(trimmed);
            result.TrimmedZeroOnlyGroupCount = CountZeroOnlyGroups(trimmed);
            result.RemovedGroupCount = Math.Max(0, result.OriginalGroupCount - result.TrimmedGroupCount);
            result.RemovedEntryCount = Math.Max(0, result.OriginalEntryCount - result.TrimmedEntryCount);
            result.TrimmedMinimumGapSeconds = ComputeMinimumGapSeconds(trimmed);
            return result;
        }

        /// <summary>
        /// Builds one merged timestamp group for the specified dense cluster.
        /// </summary>
        /// <param name="groups">
        /// The grouped viseme sequence that contains the dense cluster.
        /// </param>
        /// <param name="startIndex">
        /// The inclusive start index of the cluster to merge.
        /// </param>
        /// <param name="endIndex">
        /// The inclusive end index of the cluster to merge.
        /// </param>
        /// <param name="strongestGroupIndex">
        /// The index of the strongest original group in the cluster. Its timestamp is used
        /// for the merged output.
        /// </param>
        /// <returns>
        /// A reduced timestamp group whose viseme weights are merged from the full cluster.
        /// </returns>
        /// <remarks>
        /// Merging keeps the maximum articulation seen for each viseme across the cluster.
        /// The merged output then keeps all higher-value outer mouth shapes, while lower-
        /// value internal tongue shapes are only retained if they are strong enough to be
        /// visually meaningful. If every merged shape is filtered out, the strongest
        /// original group's entries are preserved as a safety fallback.
        /// </remarks>
        private static List<GenerateAudioReplyViseme> BuildMergedClusterEntries(IReadOnlyList<VisemeGroup> groups, int startIndex, int endIndex, int strongestGroupIndex)
        {
            const double minimumInternalArticulationToKeep = 0.6;

            double mergedTime = groups[strongestGroupIndex].StartTime;
            var maxByType = new Dictionary<string, double>(StringComparer.Ordinal);
            double strongestOuterArticulation = 0.0;

            for (int i = startIndex; i <= endIndex; i++)
            {
                var entries = groups[i].Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    GenerateAudioReplyViseme entry = entries[j];
                    string visemeType = entry.type ?? string.Empty;
                    double articulation = entry.articulation;

                    if (maxByType.TryGetValue(visemeType, out double existingArticulation))
                    {
                        if (articulation > existingArticulation)
                            maxByType[visemeType] = articulation;
                    }
                    else
                    {
                        maxByType.Add(visemeType, articulation);
                    }

                    if (IsHighValueOuterViseme(visemeType) && articulation > strongestOuterArticulation)
                        strongestOuterArticulation = articulation;
                }
            }

            var mergedEntries = new List<GenerateAudioReplyViseme>(maxByType.Count);
            foreach (var pair in maxByType)
            {
                string visemeType = pair.Key;
                double articulation = pair.Value;

                if (Math.Abs(articulation) <= 0.0000001)
                {
                    mergedEntries.Add(new GenerateAudioReplyViseme(visemeType, mergedTime, articulation));
                    continue;
                }

                if (IsHighValueOuterViseme(visemeType))
                {
                    mergedEntries.Add(new GenerateAudioReplyViseme(visemeType, mergedTime, articulation));
                    continue;
                }

                if (!IsHighValueOuterViseme(visemeType))
                {
                    if (strongestOuterArticulation <= 0.0000001 || articulation >= minimumInternalArticulationToKeep)
                        mergedEntries.Add(new GenerateAudioReplyViseme(visemeType, mergedTime, articulation));

                    continue;
                }
            }

            if (mergedEntries.Count > 0)
                return mergedEntries;

            return new List<GenerateAudioReplyViseme>(groups[strongestGroupIndex].Entries);
        }

        /// <summary>
        /// Builds a no-op trim result that preserves the original viseme sequence while
        /// still populating the standard before/after metrics.
        /// </summary>
        /// <param name="visemes">
        /// The viseme sequence that should be passed through unchanged.
        /// </param>
        /// <param name="minimumGapSeconds">
        /// The configured trim cluster span to report in the resulting metrics.
        /// </param>
        /// <returns>
        /// A <see cref="VisemeTrimResult"/> whose original and trimmed metrics are the same.
        /// </returns>
        private static VisemeTrimResult CreatePassThroughTrimResult(IReadOnlyList<GenerateAudioReplyViseme> visemes, double minimumGapSeconds)
        {
            List<GenerateAudioReplyViseme> preservedVisemes = visemes != null
                ? new List<GenerateAudioReplyViseme>(visemes)
                : new List<GenerateAudioReplyViseme>();

            int groupCount = CountDistinctVisemeStartGroups(visemes);
            int zeroOnlyGroupCount = CountZeroOnlyGroups(visemes);
            double minimumGap = ComputeMinimumGapSeconds(visemes);

            return new VisemeTrimResult
            {
                Visemes = preservedVisemes,
                OriginalEntryCount = preservedVisemes.Count,
                TrimmedEntryCount = preservedVisemes.Count,
                OriginalGroupCount = groupCount,
                TrimmedGroupCount = groupCount,
                OriginalZeroOnlyGroupCount = zeroOnlyGroupCount,
                TrimmedZeroOnlyGroupCount = zeroOnlyGroupCount,
                OriginalMinimumGapSeconds = minimumGap,
                TrimmedMinimumGapSeconds = minimumGap
            };
        }

        /// <summary>
        /// Emits a compact runtime debug summary describing how viseme trimming changed a
        /// provider's original viseme sequence.
        /// </summary>
        /// <param name="debugLabel">
        /// Provider label shown in the Unity console output.
        /// </param>
        /// <param name="trimResult">
        /// The populated trim result produced by the shared viseme trimming pipeline.
        /// </param>
        /// <remarks>
        /// The resulting log line is intended to support quick visual comparison between
        /// raw and trimmed schedules. It reports entry counts, timestamp-group counts,
        /// zero-only groups, minimum gap changes, and dense-cluster statistics without
        /// dumping the full schedule twice.
        /// </remarks>
        protected void LogVisemeTrimDebug(string debugLabel, VisemeTrimResult trimResult)
        {
            if (!lipsyncDebugOutput)
                return;

            string label = string.IsNullOrEmpty(debugLabel) ? GetType().Name : debugLabel;
            double clusterSpanSeconds = Math.Max(0.0, m_TrimClusterSpanSeconds);
            double adjacentGapSeconds = clusterSpanSeconds * TrimAdjacentGapToClusterSpanRatio;

            StringBuilder sb = new();
            sb.Append($"[{label} XML Trim] entries={trimResult.OriginalEntryCount}->{trimResult.TrimmedEntryCount} ");
            sb.Append($"groups={trimResult.OriginalGroupCount}->{trimResult.TrimmedGroupCount} ");
            sb.Append($"denseClusters={trimResult.DenseClusterCount} removedEntries={trimResult.RemovedEntryCount}");
            sb.AppendLine();
            sb.Append("Rules");
            sb.AppendLine();
            sb.Append($"  mergeDenseClustersUsingShapeRules={m_TrimMergeDenseClustersUsingShapeRules} ");
            sb.Append($"clusterSpan={clusterSpanSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s ");
            sb.Append($"adjacentGapGuard={adjacentGapSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s ");
            sb.Append($"ratio={TrimAdjacentGapToClusterSpanRatio.ToString("0.00", CultureInfo.InvariantCulture)}");
            sb.AppendLine();
            sb.Append("Details");
            sb.AppendLine();
            sb.Append($"  zeroOnlyGroups={trimResult.OriginalZeroOnlyGroupCount}->{trimResult.TrimmedZeroOnlyGroupCount} ");
            sb.Append($"minGap={FormatGapForDebug(trimResult.OriginalMinimumGapSeconds)}->{FormatGapForDebug(trimResult.TrimmedMinimumGapSeconds)} ");
            sb.Append($"removedGroups={trimResult.RemovedGroupCount} ");
            sb.Append($"avgClusterSize={trimResult.AverageClusterGroupCount.ToString("0.00", CultureInfo.InvariantCulture)} ");
            sb.Append($"largestCluster={trimResult.LargestClusterGroupCount}");
            sb.AppendLine();
            sb.Append($"  clusterBreaks span={trimResult.ClusterBreaksBySpan} adjacentGap={trimResult.ClusterBreaksByAdjacentGap}");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Builds timestamp-group records from a flat viseme list by collecting adjacent
        /// entries that share the same start time.
        /// </summary>
        /// <param name="visemes">
        /// The flat provider viseme sequence to convert into grouped timestamp records.
        /// </param>
        /// <returns>
        /// A list of <see cref="VisemeGroup"/> records preserving the original timestamp
        /// order while also capturing simple strength metrics for each group.
        /// </returns>
        /// <remarks>
        /// Grouping by timestamp is the foundation of the conservative trimming strategy.
        /// Rather than trimming individual viseme components, later rules operate on these
        /// complete groups so that any surviving pose remains an original provider pose.
        /// The aggregate strength values stored here are used to decide which group should
        /// survive when multiple timestamps are considered part of one dense cluster.
        /// </remarks>
        private static List<VisemeGroup> BuildVisemeGroups(IReadOnlyList<GenerateAudioReplyViseme> visemes)
        {
            var groups = new List<VisemeGroup>();
            int i = 0;
            while (i < visemes.Count)
            {
                double startTime = visemes[i].start;
                var entries = new List<GenerateAudioReplyViseme>();
                double totalWeight = 0.0;
                double peakWeight = 0.0;
                int nonZeroCount = 0;
                int firstSourceIndex = i;

                while (i < visemes.Count && Math.Abs(visemes[i].start - startTime) <= 0.0000001)
                {
                    GenerateAudioReplyViseme entry = visemes[i];
                    entries.Add(entry);

                    double absWeight = Math.Abs(entry.articulation);
                    if (absWeight > 0.0000001)
                    {
                        totalWeight += absWeight;
                        peakWeight = Math.Max(peakWeight, absWeight);
                        nonZeroCount++;
                    }

                    i++;
                }

                groups.Add(new VisemeGroup
                {
                    StartTime = startTime,
                    FirstSourceIndex = firstSourceIndex,
                    Entries = entries,
                    TotalWeight = totalWeight,
                    PeakWeight = peakWeight,
                    NonZeroCount = nonZeroCount
                });
            }

            return groups;
        }

        /// <summary>
        /// Selects the strongest viseme timestamp group within the specified cluster range.
        /// </summary>
        /// <param name="groups">
        /// The grouped viseme sequence that contains the dense cluster being evaluated.
        /// </param>
        /// <param name="startIndex">
        /// The inclusive start index of the dense cluster.
        /// </param>
        /// <param name="endIndex">
        /// The inclusive end index of the dense cluster.
        /// </param>
        /// <returns>
        /// The index of the group that should survive trimming for this dense cluster.
        /// </returns>
        /// <remarks>
        /// This helper does not synthesize a new pose. It simply picks one of the original
        /// timestamp groups using the shared strength comparison rules so that dense-cluster
        /// trimming remains a conservative filtering step.
        /// </remarks>
        private static int ChooseStrongestGroupIndex(IReadOnlyList<VisemeGroup> groups, int startIndex, int endIndex)
        {
            int bestIndex = startIndex;
            for (int i = startIndex + 1; i <= endIndex; i++)
            {
                if (CompareGroupStrength(groups[i], groups[bestIndex]) > 0)
                    bestIndex = i;
            }

            return bestIndex;
        }

        /// <summary>
        /// Compares two grouped viseme poses to determine which one should be considered
        /// stronger for dense-cluster trimming.
        /// </summary>
        /// <param name="left">The first grouped viseme pose to compare.</param>
        /// <param name="right">The second grouped viseme pose to compare.</param>
        /// <returns>
        /// A positive value when <paramref name="left"/> is stronger, a negative value
        /// when <paramref name="right"/> is stronger, or zero when they are equivalent.
        /// </returns>
        /// <remarks>
        /// Strength is currently determined by total non-zero articulation, then peak
        /// articulation, then the number of non-zero visemes present. If those metrics are
        /// still tied, later source order is preferred so the cluster keeps the most recent
        /// equally strong original pose.
        /// </remarks>
        private static int CompareGroupStrength(VisemeGroup left, VisemeGroup right)
        {
            int totalCompare = left.TotalWeight.CompareTo(right.TotalWeight);
            if (totalCompare != 0)
                return totalCompare;

            int peakCompare = left.PeakWeight.CompareTo(right.PeakWeight);
            if (peakCompare != 0)
                return peakCompare;

            int nonZeroCompare = left.NonZeroCount.CompareTo(right.NonZeroCount);
            if (nonZeroCompare != 0)
                return nonZeroCompare;

            return right.FirstSourceIndex.CompareTo(left.FirstSourceIndex);
        }

        /// <summary>
        /// Determines whether a viseme should be treated as a high-value outer mouth
        /// shape during dense-cluster merging.
        /// </summary>
        /// <param name="visemeType">
        /// The viseme name to classify.
        /// </param>
        /// <returns>
        /// <c>true</c> when the viseme is one of the hard-coded outer-mouth shapes that
        /// should always survive merged dense clusters. Visemes not listed here are
        /// treated as lower-priority internal/supporting shapes by the current merge rule.
        /// </returns>
        private static bool IsHighValueOuterViseme(string visemeType)
        {
            switch (visemeType)
            {
                case "open":
                case "W":
                case "wide":
                case "PBM":
                case "FV":
                case "ShCh":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Counts how many distinct timestamp groups exist in a flat viseme sequence.
        /// </summary>
        /// <param name="visemes">
        /// The viseme sequence whose timestamp groups should be counted.
        /// </param>
        /// <returns>
        /// The number of unique adjacent start-time groups present in the sequence.
        /// </returns>
        private static int CountDistinctVisemeStartGroups(IReadOnlyList<GenerateAudioReplyViseme> visemes)
        {
            if (visemes == null || visemes.Count == 0)
                return 0;

            int count = 1;
            double previousStart = visemes[0].start;
            for (int i = 1; i < visemes.Count; i++)
            {
                if (Math.Abs(visemes[i].start - previousStart) <= 0.0000001)
                    continue;

                count++;
                previousStart = visemes[i].start;
            }

            return count;
        }

        /// <summary>
        /// Counts grouped timestamps whose entries are entirely zero-articulation visemes.
        /// </summary>
        /// <param name="visemes">
        /// The flat viseme sequence to inspect.
        /// </param>
        /// <returns>
        /// The number of timestamp groups that contain no non-zero articulation.
        /// </returns>
        private static int CountZeroOnlyGroups(IReadOnlyList<GenerateAudioReplyViseme> visemes)
        {
            if (visemes == null || visemes.Count == 0)
                return 0;

            return CountZeroOnlyGroups(BuildVisemeGroups(visemes));
        }

        /// <summary>
        /// Counts grouped timestamp records that represent zero-only poses.
        /// </summary>
        /// <param name="groups">
        /// The grouped viseme sequence to inspect.
        /// </param>
        /// <returns>
        /// The number of grouped timestamps whose non-zero viseme count is zero.
        /// </returns>
        private static int CountZeroOnlyGroups(IReadOnlyList<VisemeGroup> groups)
        {
            if (groups == null || groups.Count == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].NonZeroCount == 0)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Computes the smallest gap between adjacent timestamp groups in a flat viseme
        /// sequence.
        /// </summary>
        /// <param name="visemes">
        /// The flat viseme sequence to inspect.
        /// </param>
        /// <returns>
        /// The smallest positive gap between grouped timestamps, or <see cref="double.NaN"/>
        /// when fewer than two groups are present.
        /// </returns>
        private static double ComputeMinimumGapSeconds(IReadOnlyList<GenerateAudioReplyViseme> visemes)
        {
            if (visemes == null || visemes.Count == 0)
                return double.NaN;

            return ComputeMinimumGapSeconds(BuildVisemeGroups(visemes));
        }

        /// <summary>
        /// Computes the smallest gap between adjacent grouped viseme timestamps.
        /// </summary>
        /// <param name="groups">
        /// The grouped viseme sequence to inspect.
        /// </param>
        /// <returns>
        /// The smallest positive gap between adjacent groups, or <see cref="double.NaN"/>
        /// when fewer than two groups exist.
        /// </returns>
        private static double ComputeMinimumGapSeconds(IReadOnlyList<VisemeGroup> groups)
        {
            if (groups == null || groups.Count < 2)
                return double.NaN;

            double minGap = double.PositiveInfinity;
            for (int i = 1; i < groups.Count; i++)
            {
                double gap = groups[i].StartTime - groups[i - 1].StartTime;
                if (gap < minGap)
                    minGap = gap;
            }

            return double.IsPositiveInfinity(minGap) ? double.NaN : minGap;
        }

        /// <summary>
        /// Formats a gap duration for compact debug output.
        /// </summary>
        /// <param name="seconds">
        /// The gap duration to format.
        /// </param>
        /// <returns>
        /// A human-readable seconds string, or <c>n/a</c> when the value is not defined.
        /// </returns>
        private static string FormatGapForDebug(double seconds) => double.IsNaN(seconds) ? "n/a" : seconds.ToString("0.000", CultureInfo.InvariantCulture) + "s";


        /// <summary>
        /// Coroutine that waits for both TTS and lipsync to complete before invoking the result callback.
        /// </summary>
        /// <param name="resultCallback">Callback with lipsync XML and audio file path.< see cref = "LipsyncedTextToSpeechResult" /></param>
        IEnumerator WaitForLipsyncedTextToSpeech(LipsyncedTextToSpeechResult resultCallback)
        {
            float timer = 0;
            while ((lipsyncProcessing || textToSpeechProcessing) && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (string.IsNullOrWhiteSpace(generatedAudioFilePath))
                Debug.LogWarning($"[{GetType().Name}] TTS completed without a generated audio file path.");

            resultCallback?.Invoke(lipsyncSchedule, generatedAudioFilePath);
        }
    }
}

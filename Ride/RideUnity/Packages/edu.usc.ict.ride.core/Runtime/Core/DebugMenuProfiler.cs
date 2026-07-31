using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace Ride
{
    public partial class DebugMenu
    {
        const float ProfilerCounterPeakHoldSeconds = 2f;

        Vector2 m_profilerCountersScroll = new();
        bool m_profilerCountersInitialized = false;
        readonly List<ProfilerCounterStat> m_profilerCounters = new();
        readonly List<ProfilerCounterDisplayRow> m_profilerCounterDisplayRows = new();

        enum ProfilerCounterUnit
        {
            Count,
            Bytes,
            //Time,
            //Percent,
        }

        enum ProfilerCounterId
        {
            // File access profiler counters
            FileBytesRead,
            FileBytesWritten,
            FileHandlesOpen,
            FileReadsFinished,
            FileReadsStarted,
            FileSeeks,
            FilesClosed,
            FilesOpened,
            ReadsInFlight,

            // Asset loading profiler counters
            AudioReads,
            EntitiesReads,
            MeshReads,
            OtherReads,
            ScriptingReads,
            TextureReads,
            VirtualTextureReads,

            // Memory profiler counters
            AnimationClipCount,
            AnimationClipMemory,
            AppCommittedMemory,
            AppResidentMemory,
            AssetCount,
            AudioReservedMemory,
            AudioUsedMemory,
            AudioClipCount,
            AudioClipMemory,
            GameObjectCount,
            GcAllocatedInFrame,
            GcAllocationInFrameCount,
            GcReservedMemory,
            GcUsedMemory,
            GfxReservedMemory,
            GfxUsedMemory,
            MaterialCount,
            MaterialMemory,
            MeshCount,
            MeshMemory,
            ObjectCount,
            PhysicsUsedMemory,
            PhysicsReservedMemory2D,
            ProfilerReservedMemory,
            ProfilerUsedMemory,
            SceneObjectCount,
            SystemTotalUsedMemory,
            SystemUsedMemory,
            TextureCount,
            TextureMemory,
            TotalReservedMemory,
            TotalUsedMemory,
            VideoReservedMemory,
            VideoUsedMemory,

            // Physics profiler counters
            // 2D Physics profiler counters
            // Rendering profiler counters
            // Virtual texturing profiler counters
        }

        class ProfilerCounterSpec
        {
            public string StatName;
            public ProfilerCategory Category;
            public ProfilerCounterUnit Unit;

            public ProfilerCounterSpec(string statName, ProfilerCategory category, ProfilerCounterUnit unit)
            {
                StatName = statName;
                Category = category;
                Unit = unit;
            }
        }

        // Unity Profiler counter names reference:
        // https://docs.unity3d.com/Manual/profiler-counters-reference.html
        static readonly IReadOnlyDictionary<ProfilerCounterId, ProfilerCounterSpec> s_profilerCounterSpecs = new Dictionary<ProfilerCounterId, ProfilerCounterSpec>
        {
            // File access profiler counters
            { ProfilerCounterId.FileBytesRead, new("File Bytes Read", ProfilerCategory.FileIO, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.FileBytesWritten, new("File Bytes Written", ProfilerCategory.FileIO, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.FileHandlesOpen, new("File Handles Open", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.FileReadsFinished, new("File Reads Finished", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.FileReadsStarted, new("File Reads Started", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.FileSeeks, new("File Seeks", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.FilesClosed, new("Files Closed", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.FilesOpened, new("Files Opened", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.ReadsInFlight, new("Reads in Flight", ProfilerCategory.FileIO, ProfilerCounterUnit.Count) },

            // Asset loading profiler counters
            { ProfilerCounterId.AudioReads, new("Audio Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.EntitiesReads, new("Entities Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.MeshReads, new("Mesh Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.OtherReads, new("Other Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.ScriptingReads, new("Scripting Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.TextureReads, new("Texture Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.VirtualTextureReads, new("Virtual Texture Reads", ProfilerCategory.Loading, ProfilerCounterUnit.Bytes) },

            // Memory profiler counters
            { ProfilerCounterId.AnimationClipCount, new("AnimationClip Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.AnimationClipMemory, new("AnimationClip Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.AppCommittedMemory, new("App Committed Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.AppResidentMemory, new("App Resident Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.AssetCount, new("Asset Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.AudioReservedMemory, new("Audio Reserved Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.AudioUsedMemory, new("Audio Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.AudioClipCount, new("AudioClip Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.AudioClipMemory, new("AudioClip Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.GameObjectCount, new("GameObject Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.GcAllocatedInFrame, new("GC Allocated In Frame", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.GcAllocationInFrameCount, new("GC Allocation In Frame Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.GcReservedMemory, new("GC Reserved Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.GcUsedMemory, new("GC Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.GfxReservedMemory, new("Gfx Reserved Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.GfxUsedMemory, new("Gfx Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.MaterialCount, new("Material Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.MaterialMemory, new("Material Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.MeshCount, new("Mesh Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.MeshMemory, new("Mesh Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.ObjectCount, new("Object Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.PhysicsUsedMemory, new("Physics Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.PhysicsReservedMemory2D, new("Physics Reserved Memory(2D)", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.ProfilerReservedMemory, new("Profiler Reserved Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.ProfilerUsedMemory, new("Profiler Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.SceneObjectCount, new("Scene Object Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.SystemTotalUsedMemory, new("System Total Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.SystemUsedMemory, new("System Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.TextureCount, new("Texture Count", ProfilerCategory.Memory, ProfilerCounterUnit.Count) },
            { ProfilerCounterId.TextureMemory, new("Texture Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.TotalReservedMemory, new("Total Reserved Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.TotalUsedMemory, new("Total Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.VideoReservedMemory, new("Video Reserved Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },
            { ProfilerCounterId.VideoUsedMemory, new("Video Used Memory", ProfilerCategory.Memory, ProfilerCounterUnit.Bytes) },

            // Physics profiler counters
            // 2D Physics profiler counters
            // Rendering profiler counters
            // Virtual texturing profiler counters
        };

        static ProfilerCounterSpec GetProfilerCounterSpec(ProfilerCounterId counterId) => s_profilerCounterSpecs[counterId];

        class ProfilerCounterStat
        {
            public ProfilerCounterId CounterId;
            public ProfilerRecorder Recorder;
            public bool ShowPeak;
            public Color PeakColor;
            public double PeakHighlightRatio;
            public double PeakValue = double.MinValue;
            public float PeakHoldRemaining;

            public ProfilerCounterStat(ProfilerCounterId counterId, bool showPeak, Color peakColor, double peakHighlightRatio = 1.5)
            {
                CounterId = counterId;
                ShowPeak = showPeak;
                PeakColor = peakColor;
                PeakHighlightRatio = peakHighlightRatio;
                Recorder = default;
                PeakValue = double.MinValue;
                PeakHoldRemaining = 0;
            }
        }

        class ProfilerCounterDisplayRow
        {
            public string GroupName;
            public string Format;
            public ProfilerCounterId[] CounterIds;
            public ProfilerCounterStat[] Counters;
            public string[] FormattedValues;

            public ProfilerCounterDisplayRow(string groupName, string format, params ProfilerCounterId[] counterIds)
            {
                GroupName = groupName;
                Format = format;
                CounterIds = counterIds;
                Counters = Array.Empty<ProfilerCounterStat>();
                FormattedValues = new string[counterIds.Length];
            }
        }

        void InitializeProfilerCounters()
        {
            DisposeProfilerCounters();
            foreach (var counter in GetProfilerCounters())
            {
                ProfilerCounterSpec spec = GetProfilerCounterSpec(counter.CounterId);
                counter.Recorder = ProfilerRecorder.StartNew(spec.Category, spec.StatName, 1);
                m_profilerCounters.Add(counter);
            }

            InitializeProfilerCounterDisplayRows();
            m_profilerCountersInitialized = true;
        }

        void DisposeProfilerCounters()
        {
            foreach (var counter in m_profilerCounters)
                counter.Recorder.Dispose();

            m_profilerCounters.Clear();
            m_profilerCounterDisplayRows.Clear();
            m_profilerCountersInitialized = false;
        }

        void InitializeProfilerCounterDisplayRows()
        {
            m_profilerCounterDisplayRows.Clear();

            var countersById = m_profilerCounters.ToDictionary(c => c.CounterId);
            foreach (var row in GetProfilerCounterDisplayRows())
            {
                row.Counters = row.CounterIds
                    .Select(counterId => countersById.TryGetValue(counterId, out var counter) ? counter : null)
                    .ToArray();

                if (row.Counters.Any(counter => counter != null))
                    m_profilerCounterDisplayRows.Add(row);
            }
        }

        static IReadOnlyList<ProfilerCounterStat> GetProfilerCounters()
        {
            Color defaultPeakColor = Color.white;
            Color memoryBytesPeakColor = new Color(1.0f, 0.55f, 0.2f);
            Color memoryCountPeakColor = new Color(1.0f, 0.9f, 0.3f);
            Color diskBytesPeakColor = new Color(0.4f, 0.9f, 1.0f);
            Color diskCountPeakColor = new Color(0.6f, 0.9f, 1.0f);
            Color assetLoadingPeakColor = new Color(0.5f, 1.0f, 0.5f);

            const double defaultPeakRatio = 1.5;
            const double countPeakRatio = 1.25;

            return new ProfilerCounterStat[]
            {
                new(ProfilerCounterId.AnimationClipCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.AnimationClipMemory, false, defaultPeakColor),
                new(ProfilerCounterId.AppCommittedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.AppResidentMemory, false, defaultPeakColor),
                new(ProfilerCounterId.AssetCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.AudioReservedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.AudioUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.AudioClipCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.AudioClipMemory, false, defaultPeakColor),
                new(ProfilerCounterId.GameObjectCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.GcAllocatedInFrame, true, memoryBytesPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.GcAllocationInFrameCount, true, memoryCountPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.GcReservedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.GcUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.GfxReservedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.GfxUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.MaterialCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.MaterialMemory, false, defaultPeakColor),
                new(ProfilerCounterId.MeshCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.MeshMemory, false, defaultPeakColor),
                new(ProfilerCounterId.ObjectCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.PhysicsUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.PhysicsReservedMemory2D, false, defaultPeakColor),
                new(ProfilerCounterId.ProfilerReservedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.ProfilerUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.SceneObjectCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.SystemTotalUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.SystemUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.TextureCount, true, memoryCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.TextureMemory, false, defaultPeakColor),
                new(ProfilerCounterId.TotalReservedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.TotalUsedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.VideoReservedMemory, false, defaultPeakColor),
                new(ProfilerCounterId.VideoUsedMemory, false, defaultPeakColor),

                new(ProfilerCounterId.FileBytesRead, true, diskBytesPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.FileBytesWritten, true, diskBytesPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.FileHandlesOpen, true, diskCountPeakColor, countPeakRatio),
                new(ProfilerCounterId.FileReadsFinished, true, diskCountPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.FileReadsStarted, true, diskCountPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.FileSeeks, true, diskCountPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.FilesClosed, true, diskCountPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.FilesOpened, true, diskCountPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.ReadsInFlight, true, diskCountPeakColor, defaultPeakRatio),

                new(ProfilerCounterId.AudioReads, true, assetLoadingPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.EntitiesReads, true, assetLoadingPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.MeshReads, true, assetLoadingPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.OtherReads, true, assetLoadingPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.ScriptingReads, true, assetLoadingPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.TextureReads, true, assetLoadingPeakColor, defaultPeakRatio),
                new(ProfilerCounterId.VirtualTextureReads, true, assetLoadingPeakColor, defaultPeakRatio),
            };
        }

        static IReadOnlyList<ProfilerCounterDisplayRow> GetProfilerCounterDisplayRows()
        {
            return new ProfilerCounterDisplayRow[]
            {
                new("Memory", "AnimationClip - {0} - {1}", ProfilerCounterId.AnimationClipCount, ProfilerCounterId.AnimationClipMemory),
                new("Memory", "AudioClip - {0} - {1}", ProfilerCounterId.AudioClipCount, ProfilerCounterId.AudioClipMemory),
                new("Memory", "Material - {0} - {1}", ProfilerCounterId.MaterialCount, ProfilerCounterId.MaterialMemory),
                new("Memory", "Mesh - {0} - {1}", ProfilerCounterId.MeshCount, ProfilerCounterId.MeshMemory),
                new("Memory", "Texture - {0} - {1}", ProfilerCounterId.TextureCount, ProfilerCounterId.TextureMemory),
                new("Memory", "GC - {0} - {1} - {2}", ProfilerCounterId.GcUsedMemory, ProfilerCounterId.GcReservedMemory, ProfilerCounterId.GcAllocatedInFrame),
                new("Memory", "GC Count - {0}", ProfilerCounterId.GcAllocationInFrameCount),
                new("Memory", "Objects - {0} - {1} - {2}", ProfilerCounterId.GameObjectCount, ProfilerCounterId.SceneObjectCount, ProfilerCounterId.ObjectCount),
                new("Memory", "Assets - {0}", ProfilerCounterId.AssetCount),
                new("Memory", "App - {0} - {1}", ProfilerCounterId.AppResidentMemory, ProfilerCounterId.AppCommittedMemory),
                new("Memory", "Total - {0} - {1}", ProfilerCounterId.TotalUsedMemory, ProfilerCounterId.TotalReservedMemory),
                new("Memory", "System - {0} - {1}", ProfilerCounterId.SystemUsedMemory, ProfilerCounterId.SystemTotalUsedMemory),
                new("Memory", "Audio Sys - {0} - {1}", ProfilerCounterId.AudioUsedMemory, ProfilerCounterId.AudioReservedMemory),
                new("Memory", "Gfx - {0} - {1}", ProfilerCounterId.GfxUsedMemory, ProfilerCounterId.GfxReservedMemory),
                new("Memory", "Video - {0} - {1}", ProfilerCounterId.VideoUsedMemory, ProfilerCounterId.VideoReservedMemory),
                new("Memory", "Profiler - {0} - {1}", ProfilerCounterId.ProfilerUsedMemory, ProfilerCounterId.ProfilerReservedMemory),
                new("Memory", "Physics - {0} - {1}", ProfilerCounterId.PhysicsUsedMemory, ProfilerCounterId.PhysicsReservedMemory2D),

                new("File", "Bytes - {0} - {1}", ProfilerCounterId.FileBytesRead, ProfilerCounterId.FileBytesWritten),
                //new("File", "Reads - {0} - {1} - {2}", ProfilerCounterId.FileReadsStarted, ProfilerCounterId.FileReadsFinished, ProfilerCounterId.ReadsInFlight),
                new("File", "Files - {0} - {1} - {2}", ProfilerCounterId.FilesOpened, ProfilerCounterId.FilesClosed, ProfilerCounterId.FileHandlesOpen),
                new("File", "Seeks - {0}", ProfilerCounterId.FileSeeks),
                //new("File", "In Flight - {0}", ProfilerCounterId.ReadsInFlight),

                new("Asset Loading", "Audio/Mesh/Tex - {0} - {1} - {2}", ProfilerCounterId.AudioReads, ProfilerCounterId.MeshReads, ProfilerCounterId.TextureReads),
                new("Asset Loading", "Script/Ent - {0} - {1}", ProfilerCounterId.ScriptingReads, ProfilerCounterId.EntitiesReads),
                new("Asset Loading", "Other/VT - {0} - {1}", ProfilerCounterId.OtherReads, ProfilerCounterId.VirtualTextureReads),
            };
        }

        void UpdateProfilerCounterPeaks(float dt)
        {
            foreach (var counter in m_profilerCounters)
            {
                if (!counter.ShowPeak || !counter.Recorder.Valid)
                    continue;

                double value = counter.Recorder.LastValue;
                if (value >= counter.PeakValue)
                {
                    counter.PeakValue = value;
                    counter.PeakHoldRemaining = ProfilerCounterPeakHoldSeconds;
                    continue;
                }

                if (counter.PeakHoldRemaining > 0)
                {
                    counter.PeakHoldRemaining = Mathf.Max(0, counter.PeakHoldRemaining - dt);
                    continue;
                }

                counter.PeakValue = double.MinValue;
            }
        }

        void OnGUIProfilerCounters()
        {
            if (!m_profilerCountersInitialized)
                InitializeProfilerCounters();

            if (m_profilerCounterDisplayRows.Count == 0)
            {
                Label("No profiler counters available in Memory, Disk, or Asset Loading.");
                return;
            }

            Label($"Frame: {Time.frameCount:N0}");
            Label(RideUtils.IsDebugBuild()
                ? "Debug build. Showing last completed frame."
                : "Release build. Counters may be unavailable.");
            Label($"Peak holds for {ProfilerCounterPeakHoldSeconds:0.0}s, then clears.");
            Space();

            string currentGroup = null;
            using (var scrollViewScope = new GUILayout.ScrollViewScope(m_profilerCountersScroll))
            {
                m_profilerCountersScroll = scrollViewScope.scrollPosition;

                foreach (var row in m_profilerCounterDisplayRows)
                {
                    if (!string.Equals(currentGroup, row.GroupName, StringComparison.Ordinal))
                    {
                        currentGroup = row.GroupName;
                        if (!string.IsNullOrEmpty(currentGroup) && !ReferenceEquals(row, m_profilerCounterDisplayRows[0]))
                            Space();

                        Label(currentGroup);
                    }

                    for (int i = 0; i < row.Counters.Length; i++)
                        row.FormattedValues[i] = FormatDisplayValue(row.Counters[i]);

                    Label(string.Format(row.Format, row.FormattedValues));
                }
            }
        }

        bool IsProfilerCountersMenuSelected()
        {
            if (m_debugMenuSelected < 0 || m_debugMenuSelected >= m_debugMenus.Count)
                return false;

            return m_debugMenus[m_debugMenuSelected].callback == OnGUIProfilerCounters;
        }

        static string FormatDisplayValue(ProfilerCounterStat counter)
        {
            if (counter == null || !counter.Recorder.Valid)
                return "N/A";

            ProfilerCounterSpec spec = GetProfilerCounterSpec(counter.CounterId);
            string currentText = FormatProfilerCounterValue(counter.Recorder.LastValue, spec.Unit);
            if (!counter.ShowPeak)
                return currentText;

            if (counter.PeakHoldRemaining <= 0 || counter.PeakValue == double.MinValue)
                return currentText;

            bool peaksMatch = PeaksMatchCurrent(counter, counter.Recorder.LastValue);
            if (peaksMatch)
                return currentText;

            string peakText = FormatProfilerCounterValue(counter.PeakValue, spec.Unit);
            if (ShouldHighlightPeak(counter, counter.Recorder.LastValue))
                peakText = WrapRichTextColor(peakText, counter.PeakColor);

            return $"{currentText} ({peakText})";
        }

        static string FormatProfilerCounterValue(double value, ProfilerCounterUnit unit)
        {
            switch (unit)
            {
                case ProfilerCounterUnit.Bytes:
                    return FormatBytes(value);
                case ProfilerCounterUnit.Count:
                default:
                    return $"{value:N0}";
            }
        }

        static string FormatBytes(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double absBytes = Math.Abs(bytes);
            int unitIndex = 0;
            while (absBytes >= 1024.0 && unitIndex < units.Length - 1)
            {
                absBytes /= 1024.0;
                unitIndex++;
            }

            double scaledValue = bytes / Math.Pow(1024.0, unitIndex);
            return $"{scaledValue:0.##} {units[unitIndex]}";
        }

        static bool ShouldHighlightPeak(ProfilerCounterStat counter, double currentValue)
        {
            if (counter.PeakValue <= 0)
                return false;
            if (currentValue <= 0)
                return true;

            return counter.PeakValue >= currentValue * counter.PeakHighlightRatio;
        }

        static bool PeaksMatchCurrent(ProfilerCounterStat counter, double currentValue) => Math.Abs(counter.PeakValue - currentValue) < 0.5;

        static string WrapRichTextColor(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}

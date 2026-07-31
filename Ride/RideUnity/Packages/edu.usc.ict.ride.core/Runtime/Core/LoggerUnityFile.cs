using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Abstract base for file-based <see cref="LoggerUnity"/> sinks.
    /// </summary>
    /// <remarks>
    /// <para><b>File naming and folder structure</b></para>
    /// <para>
    /// Files are written to a date-organised folder mirroring the CloudWatch log stream hierarchy:
    /// <c>{base}/{YYYY}/{YYYY-MM}/{YYYY-MM-DD}/{prefix}-{HH-mm-ss}-{platform}{extension}</c>.
    /// The base folder defaults to <c>{MyDocuments}/{productName}/Logs/</c> when <c>m_folder</c> is empty.
    /// Provide an absolute path to write anywhere, or a relative path to resolve from <c>MyDocuments</c>.
    /// The prefix defaults to <see cref="Application.productName"/>; override via <c>m_fileNamePrefix</c>.
    /// </para>
    /// <para><b>Flush timing</b></para>
    /// <para>
    /// When <c>m_flushIntervalSeconds</c> is zero (the default) every entry is flushed
    /// immediately via a single open-write-close, leaving no persistent file handle between
    /// flushes. Set a positive value to batch writes when log volume causes frame-time spikes.
    /// A final flush always runs in <c>OnDestroy</c>.
    /// </para>
    /// <para><b>Rollover</b></para>
    /// <para>
    /// When <c>m_enableRollover</c> is true and the file exceeds <c>m_maxFileSizeBytes</c>,
    /// the current file is renamed with a zero-padded counter suffix before the next write
    /// (e.g. <c>MyApp-2026-07-17T21-59-54_01.txt</c>) and a fresh file starts at the
    /// original path. The counter width is controlled by <c>m_rolloverCounterPadding</c>.
    /// </para>
    /// <para><b>Subclasses</b></para>
    /// <list type="bullet">
    ///   <item><description><see cref="LoggerUnityFileJsonl"/> — one JSON object per line, events only</description></item>
    ///   <item><description><see cref="LoggerUnityFileCsv"/> — one CSV row per entry, all kinds</description></item>
    /// </list>
    /// </remarks>
    public class LoggerUnityFile : LoggerUnity
    {
        [Header("File")]
        [SerializeField, Tooltip("Folder for the log file. Leave empty to write to {Documents}/{ProductName}/Logs/. Absolute paths are used as-is; relative paths resolve from Documents.")]
        string m_folder;

        [SerializeField, Tooltip("Prefix for the log file name. Leave empty to use the project name. Final filename is {prefix}-{startTimestamp}{extension}.")]
        string m_fileNamePrefix;

        [SerializeField, Tooltip("Seconds between disk writes. 0 = flush immediately on every log entry. Increase if high log volume causes frame-time spikes.")]
        float m_flushIntervalSeconds = 0f;

        [Header("Rollover")]
        [SerializeField, Tooltip("When enabled, the log file is renamed with a counter suffix when it exceeds the maximum size, and a fresh file is started.")]
        bool m_enableRollover = false;
        [SerializeField, Tooltip("Maximum log file size in bytes before rollover occurs. Default is 10 MB.")]
        long m_maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        [SerializeField, Tooltip("Number of digits in the rollover counter suffix. Default 2 produces _01, _02, etc.")]
        int m_rolloverCounterPadding = 2;


        readonly List<string> m_buffer = new List<string>();
        float m_flushTimer;
        string m_resolvedPath;
        bool m_fatalError;


        /// <summary>File extension for the log file, including the dot. Override in subclasses to match the output format.</summary>
        protected virtual string DefaultExtension => ".txt";

        /// <summary>Full path of the log file currently being written, set at <c>Start()</c>.</summary>
        public string LogFilePath => m_resolvedPath ?? string.Empty;

        /// <summary>
        /// Optional header line written as the first line of a new file.
        /// Override in subclasses to emit column headers (e.g. CSV).
        /// </summary>
        protected virtual string FileHeader => null;


        protected override void Start()
        {
            base.Start();

            m_resolvedPath = BuildPath();
            if (FileHeader != null)
                m_buffer.Add(FileHeader);
        }

        protected override void Update()
        {
            base.Update();

            if (m_flushIntervalSeconds <= 0f || m_buffer.Count == 0)
                return;

            m_flushTimer += Time.deltaTime;
            if (m_flushTimer >= m_flushIntervalSeconds)
                Flush();
        }

        void OnDestroy() => Flush();

        public override void Log(LogEntry entry)
        {
            if (m_fatalError || !ShouldLog(entry))
                return;

            m_buffer.Add(FormatEntry(entry));

            if (m_flushIntervalSeconds <= 0f)
                Flush();
        }

        void Flush()
        {
            if (m_buffer.Count == 0)
                return;

            m_flushTimer = 0f;

            string path = m_resolvedPath ?? BuildPath();  // Path may not be set yet if Log() is called before Start()

            try
            {
                EnsureDirectory(path);
                CheckRollover(path);
                File.AppendAllLines(path, m_buffer);
            }
            catch (Exception e)
            {
                m_fatalError = true;
                // Use Debug.LogError directly — bypasses RideLog to avoid a feedback loop
                // where UnityLogBridgeSystem forwards the error back into this logger.
                Debug.LogError($"[LoggerUnityFile] Fatal write error at {path} — logging disabled. {e.GetType().Name}: {e.Message}");
            }

            m_buffer.Clear();
        }

        string BuildPath()
        {
            string product = string.IsNullOrEmpty(m_productName) ? Application.productName : m_productName;
            string prefix  = string.IsNullOrWhiteSpace(m_fileNamePrefix) ? product : m_fileNamePrefix;

            string baseFolder;
            if (string.IsNullOrWhiteSpace(m_folder))
            {
                baseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    product,
                    "Logs");
            }
            else if (Path.IsPathRooted(m_folder))
            {
                baseFolder = m_folder;
            }
            else
            {
                baseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    m_folder);
            }

            DateTime start = DateTime.TryParse(m_sessionStartUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToLocalTime() : DateTime.Now;

            string folder   = Path.Combine(baseFolder, start.ToString("yyyy"), start.ToString("yyyy-MM"), start.ToString("yyyy-MM-dd"));
            string fileName = $"{prefix}-{start:yyyyMMdd-HHmmss}-{m_platform}{DefaultExtension}";

            return Path.Combine(folder, fileName);
        }

        static void EnsureDirectory(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        void CheckRollover(string path)
        {
            if (!m_enableRollover)
                return;

            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length < m_maxFileSizeBytes)
                return;

            fi.MoveTo(BuildRolloverPath(path));
        }

        string BuildRolloverPath(string path)
        {
            string dir  = Path.GetDirectoryName(path) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(path);
            string ext  = Path.GetExtension(path);
            int    n    = NextCounterSuffix(dir, name, ext);
            string pad  = n.ToString($"D{m_rolloverCounterPadding}");

            return Path.Combine(dir, $"{name}_{pad}{ext}");
        }

        int NextCounterSuffix(string dir, string name, string ext)
        {
            int i = 1;
            string pad;
            do { pad = i.ToString($"D{m_rolloverCounterPadding}"); i++; }
            while (File.Exists(Path.Combine(dir, $"{name}_{pad}{ext}")));
            return i - 1;
        }
    }
}

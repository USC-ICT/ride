using System.IO;
using UnityEngine;
using Ride;

namespace Ride.Samples
{
    public class SamplesCoreLogSystemUnity : RideMonoBehaviour
    {
        [SerializeField] LoggerUnityFile m_fileLogger;
        [SerializeField] LoggerUnityFileJsonl m_jsonlLogger;
        [SerializeField] LoggerUnityFileCsv m_csvLogger;
        [SerializeField] LoggerUnityHttp m_httpLogger;

        DebugMenu m_debugMenu;


        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();
        }

        public void OnGUILogSystem()
        {
            // --- RideLog entry point ---
            // All calls below route through LogSystemUnity to every registered logger,
            // including LoggerUnityConsole (console) and any file loggers in the scene.

            if (m_debugMenu.Button("Developer Log"))
                RideLog.LogDeveloper("Sample developer log from the Log tab", nameof(SamplesCoreLogSystemUnity));

            if (m_debugMenu.Button("Developer Warning"))
                RideLog.LogDeveloper(LogType.Warning, "Sample developer warning from the Log tab", nameof(SamplesCoreLogSystemUnity));

            if (m_debugMenu.Button("Application Event"))
                RideLog.LogApplicationEvent("SampleButtonClicked", "Log tab application event payload", nameof(SamplesCoreLogSystemUnity));

            if (m_debugMenu.Button("Developer Error"))
                RideLog.LogDeveloper(LogType.Error, "Sample developer error from the Log tab", nameof(SamplesCoreLogSystemUnity));

            m_debugMenu.Space();

            // --- File sinks ---
            // LoggerUnityFile (text) and LoggerUnityFileJsonl receive the same RideLog calls above.
            // Use these buttons to inspect where files are being written.

            if (m_debugMenu.Button("Print Log File Paths"))
            {
                if (m_fileLogger != null)
                    RideLog.LogDeveloper($"Text log: {m_fileLogger.LogFilePath}", nameof(SamplesCoreLogSystemUnity));
                else
                    RideLog.LogDeveloper("Text logger not assigned in inspector.", nameof(SamplesCoreLogSystemUnity));

                if (m_jsonlLogger != null)
                    RideLog.LogDeveloper($"JSONL log: {m_jsonlLogger.LogFilePath}", nameof(SamplesCoreLogSystemUnity));
                else
                    RideLog.LogDeveloper("JSONL logger not assigned in inspector.", nameof(SamplesCoreLogSystemUnity));

                if (m_csvLogger != null)
                    RideLog.LogDeveloper($"CSV log: {m_csvLogger.LogFilePath}", nameof(SamplesCoreLogSystemUnity));
                else
                    RideLog.LogDeveloper("CSV logger not assigned in inspector.", nameof(SamplesCoreLogSystemUnity));

                if (m_httpLogger != null)
                    RideLog.LogDeveloper($"HTTP endpoint: {ConfigurationSystemUnity.GetLogsProxyEndpoint()}", nameof(SamplesCoreLogSystemUnity));
                else
                    RideLog.LogDeveloper("HTTP logger not assigned in inspector.", nameof(SamplesCoreLogSystemUnity));
            }

            if (m_debugMenu.Button("Open Log Folder"))
            {
                string path = m_fileLogger  != null ? m_fileLogger.LogFilePath
                            : m_jsonlLogger != null ? m_jsonlLogger.LogFilePath
                            : m_csvLogger   != null ? m_csvLogger.LogFilePath
                            : string.Empty;

                if (!string.IsNullOrEmpty(path))
                    Application.OpenURL($"file://{Path.GetDirectoryName(path)}");
                else
                    RideLog.LogDeveloper("No file logger assigned — cannot open folder.", nameof(SamplesCoreLogSystemUnity));
            }
        }
    }
}

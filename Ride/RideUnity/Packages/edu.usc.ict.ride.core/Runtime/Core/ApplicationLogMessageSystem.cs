using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VHAssets;

namespace Ride
{
    public class ApplicationLogMessageSystem : RideSystemMonoBehaviour, IApplicationLogMessageSystem
    {
        [SerializeField] LogCallbackHandler m_logCallbackHandler;

        List<IApplicationLogMessageSystem.LogCallback> m_callbacks = new List<IApplicationLogMessageSystem.LogCallback>();


        public override void SystemAwake()
        {
            base.SystemAwake();

            m_logCallbackHandler.AddCallback(LogCallback);
        }

        public void AddCallback(IApplicationLogMessageSystem.LogCallback callback)
        {
            m_callbacks.Add(callback);
        }

        public void RemoveCallback(IApplicationLogMessageSystem.LogCallback callback)
        {
            m_callbacks.Remove(callback);
        }


        void LogCallback(string condition, string stackTrace, UnityEngine.LogType type)
        {
            IApplicationLogMessageSystem.LogType logType;
            switch (type)
            {
                case UnityEngine.LogType.Error:     logType = IApplicationLogMessageSystem.LogType.Error; break;
                case UnityEngine.LogType.Assert:    logType = IApplicationLogMessageSystem.LogType.Assert; break;
                case UnityEngine.LogType.Warning:   logType = IApplicationLogMessageSystem.LogType.Warning; break;
                case UnityEngine.LogType.Log:       logType = IApplicationLogMessageSystem.LogType.Log; break;
                case UnityEngine.LogType.Exception: logType = IApplicationLogMessageSystem.LogType.Exception; break;
                default:  UnityEngine.Debug.LogErrorFormat("ApplicationLogMessageSystem.LogCallback() - unknown LogType: {0}", type); logType = default; break;
            }

            foreach (var callback in m_callbacks)
                callback(condition, stackTrace, logType);
        }
    }
}

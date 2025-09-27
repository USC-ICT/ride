using System;
using System.Collections;
using UnityEngine;

namespace VHAssets
{
public class MicrophoneRecorder : RecordingDevice
{
    #region Variables
    public KeyCode m_RecordingKey = KeyCode.Alpha0;
    public int m_RecordingMouseButton = 0;
    public bool m_CheckRecordingInput = true;
    public int m_Frequency = 44100;
    public int m_RecordLength = 10;

    string m_CurrentDevice = "";

    float [] m_SampleBuffer;
    #endregion

    #region Properties
    public string [] ConnectedMicrophones
    {
        get { return MicrophoneDevices(); }
    }

    public int NumConnectedMicrophones
    {
        get { return MicrophoneDevices().Length; }
    }

    public bool IsMicrophoneAvailable
    {
        get { return NumConnectedMicrophones > 0; }
    }

    public string CurrentMicrophone
    {
        get { return m_CurrentDevice; }
    }

    override public bool IsRecording
    {
        get { return MicrophoneIsRecording(CurrentMicrophone); }
    }

    bool MicAccessAllowed
    {
        get { return Application.HasUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone); }
    }

    bool CanRecord
    {
        get { return !IsRecording && !string.IsNullOrEmpty(CurrentMicrophone); }
    }

    public bool CheckRecordingInput
    {
        get { return m_CheckRecordingInput; }
        set
        {
            m_CheckRecordingInput = value;
            enabled = m_CheckRecordingInput;
        }
    }
#endregion

#region Functions
    void Awake()
    {
        if (VHUtils.IsWebGL())
            StartCoroutine(WaitForMicConfirmation());
        else
            SetDefaultMic();
    }

    void Start()
    {
        CheckRecordingInput = m_CheckRecordingInput;
    }

    public override void SetContinuousStreaming(bool tf)
    {
        base.SetContinuousStreaming(tf);
        CheckRecordingInput = !tf;
        StopRecording();
        if (tf)
        {
            StartRecording();
        }
    }

    void SetDefaultMic()
    {
        if (IsMicrophoneAvailable)
        {
            SetRecordingDevice(ConnectedMicrophones[0]);
        }
        else
        {
            // there aren't any recording devices
            //Debug.LogWarning("No recording devices found");
            CheckRecordingInput = false;
        }
    }

    void Update()
    {
        bool checkMouseButton = m_RecordingMouseButton == 0 || m_RecordingMouseButton == 1 || m_RecordingMouseButton == 2;

        if ((checkMouseButton && Input.GetMouseButton(m_RecordingMouseButton)) || Input.GetKey(m_RecordingKey))
        {
            StartRecording();
        }
        else if ((checkMouseButton && Input.GetMouseButtonUp(m_RecordingMouseButton)) || Input.GetKeyUp(m_RecordingKey))
        {
            StopRecording();
        }
    }

    void OnDestroy()
    {
        StopRecording();
    }

    public int GetMicrophonePosition()
    {
        return MicrophoneGetPosition(CurrentMicrophone);
    }

    override public float GetRecordingVolumeLevel()
    {
        return GetRecordingVolumeLevel(GetMicrophonePosition() - 1);
    }

    public float GetRecordingVolumeLevel(int micPos)
    {
        float volume = 0;
        if (IsRecording && m_SampleBuffer != null)
        {
            m_Stream.Clip.GetData(m_SampleBuffer, 0);
            int pos = Mathf.Clamp(micPos, 0, m_SampleBuffer.Length);
            volume = Mathf.Clamp01(Mathf.Abs(m_SampleBuffer[pos]));
        }
        return volume;
    }

    public override AudioStream StartRecording()
    {
        return StartRecording(CurrentMicrophone, true, m_RecordLength, m_Frequency);
    }

    AudioStream StartRecording(string deviceName)
    {
        SetRecordingDevice(deviceName);
        return StartRecording(CurrentMicrophone, true, m_RecordLength, m_Frequency);
    }

    AudioStream StartRecording(string deviceName, bool loop, int recordLength, int frequency)
    {
        if (CanRecord)
        {
            m_Stream.Clip = MicrophoneStart(deviceName, loop, recordLength, frequency);

            if (m_SampleBuffer == null)
            {
                m_SampleBuffer = new float[m_Stream.Clip.samples];
            }

            m_OnStartedRecordingCBs?.Invoke(m_Stream);
        }

        return m_Stream;
    }

    public override void StopRecording()
    {
        if (IsRecording)
        {
            // only use audio data up to where the current pos of the mic is
            int micPos = MicrophoneGetPosition(CurrentMicrophone);
            if (micPos > 0)
            {
                // extract clip bugger data up to the point of the mic pos
                float[] clipData = new float[micPos];
                m_Stream.Clip.GetData(clipData, 0);

                // copy the data into a new clip
                AudioClip clip = AudioClip.Create("micStreamingAudio", micPos, m_Stream.Clip.channels, m_Stream.Clip.frequency, false);
                clip.SetData(clipData, 0);

                AudioStream completedUtterance = new AudioStream(clip);

                MicrophoneEnd(CurrentMicrophone);

                m_Stream.StreamComplete = true;

                m_OnFinishedRecordingCBs?.Invoke(completedUtterance);
            }
        }
    }

    public override void SetRecordingDevice(int deviceIndex)
    {
        if (deviceIndex < 0 || deviceIndex >= NumConnectedMicrophones)
        {
            Debug.LogError("SetRecordingDevice bad index " + deviceIndex);
        }
        else
        {
            SetRecordingDevice(ConnectedMicrophones[deviceIndex]);
        }
    }

    public override void SetRecordingDevice(string deviceName)
    {
        if (DoesDeviceExist(deviceName))
        {
            m_CurrentDevice = deviceName;
            //Debug.Log(deviceName);
        }
        else
        {
            Debug.LogError("Failed to SetRecordingDevice.  Device " + deviceName + " doesn't exist");
        }
    }

    string [] MicrophoneDevices()
    {
#if UNITY_WEBGL
        return new string [] { };
#else
        return Microphone.devices;
#endif
    }

    AudioClip MicrophoneStart(string deviceName, bool loop, int lengthSec, int frequency)
    {
#if UNITY_WEBGL
        return null;
#else
        return Microphone.Start(deviceName, loop, lengthSec, frequency);
#endif
    }

    void MicrophoneEnd(string deviceName)
    {
#if UNITY_WEBGL
#else
        Microphone.End(deviceName);
#endif
    }

    bool MicrophoneIsRecording(string deviceName)
    {
#if UNITY_WEBGL
        return false;
#else
        return Microphone.devices.Length > 0 ? Microphone.IsRecording(deviceName) : false;
#endif
    }

    int MicrophoneGetPosition(string deviceName)
    {
#if UNITY_WEBGL
        return 0;
#else
        return Microphone.GetPosition(deviceName);
#endif
    }

    public void PrintRecordingDevices()
    {
#if !UNITY_WSA
        Array.ForEach<string>(ConnectedMicrophones, rd => Debug.Log(rd));
#else
        Debug.LogErrorFormat("MicrophoneRecorder.PrintRecordingDevices() - not implemented on this platform.");
#endif
    }

    bool DoesDeviceExist(string deviceName)
    {
        return !Array.TrueForAll<string>(ConnectedMicrophones, s => s != deviceName);
    }

    IEnumerator WaitForMicConfirmation()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone);
        SetDefaultMic();
    }

    public bool IsMicSilent(float silenceThreshold)
    {
        float micVolume = GetRecordingVolumeLevel();
        return micVolume >= -silenceThreshold && micVolume <= silenceThreshold;
    }

    public override string GetDeviceName()
    {
        return CurrentMicrophone;
    }

    public override int GetNumRecordingDevices()
    {
        return NumConnectedMicrophones;
    }
#endregion
}
}

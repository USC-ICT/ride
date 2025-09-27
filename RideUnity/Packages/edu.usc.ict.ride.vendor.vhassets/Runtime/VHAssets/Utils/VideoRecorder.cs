using System.Collections;
using UnityEngine;

namespace VHAssets
{
public class VideoRecorder : MonoBehaviour
{
    #region Variables
    public int m_RecordingMouseButton;
    public KeyCode m_RecordingKey;
    public bool m_CheckRecordingInput = true;
    public Renderer m_RenderTarget;
#if UNITY_WEBGL
    Texture m_CurrentCam;
#else
    WebCamTexture m_CurrentCam;
#endif
    #endregion

    #region Properties

#if UNITY_WEBGL
    public Texture CurrentCam
#else
    public WebCamTexture CurrentCam
#endif
    {
        get { return m_CurrentCam; }
    }

    public string CurrentCameraName
    {
        get { return CurrentCam != null ? WebCamTextureDeviceName(CurrentCam) : ""; }
    }

#if UNITY_WEBGL
    public string[] ConnectedDevices
#else
    public WebCamDevice[] ConnectedDevices
#endif
    {
        get { return WebCamTextureDevices(); }
    }

    public bool IsPlaying
    {
        get { return m_CurrentCam != null ? WebCamTextureIsPlaying(m_CurrentCam) : false; }
    }

    public int NumConnectedDevices
    {
        get { return ConnectedDevices.Length; }
    }
    #endregion

    #region Functions
    void Start()
    {
        if (VHUtils.IsWebGL())
            StartCoroutine(WaitForConfirmation());
        else
            SetDefaultCamera();
    }

    IEnumerator WaitForConfirmation()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        SetDefaultCamera();
    }

    void SetDefaultCamera()
    {
        if (NumConnectedDevices > 0)
        {
            SetCamera(WebCamDeviceName(ConnectedDevices[0]), m_RenderTarget);
        }

        if (NumConnectedDevices > 0)
        {
            foreach (var camera in ConnectedDevices)
                Debug.Log("VideoRecorder: " + WebCamDeviceName(camera));
        }
        else
        {
            Debug.Log("VideoRecorder: No camera devices detected");
        }
    }

    void Update()
    {
        if (m_CheckRecordingInput)
        {
            if (Input.GetMouseButton(m_RecordingMouseButton) || Input.GetKey(m_RecordingKey))
            {
                StartRecording();
            }
            else if (Input.GetMouseButtonUp(m_RecordingMouseButton) || Input.GetKeyUp(m_RecordingKey))
            {
                PauseRecording();
            }
        }
    }

#if UNITY_WEBGL
    string [] WebCamTextureDevices()
#else
    WebCamDevice [] WebCamTextureDevices()
#endif
    {
#if UNITY_WEBGL
        return new string [] { };
#else
        return WebCamTexture.devices;
#endif
    }

#if UNITY_WEBGL
    string WebCamTextureDeviceName(Texture device)
#else
    string WebCamTextureDeviceName(WebCamTexture device)
#endif
    {
#if UNITY_WEBGL
        return "";
#else
        return device.deviceName;
#endif
    }

#if UNITY_WEBGL
    void WebCamTexturePlay(Texture device)
#else
    void WebCamTexturePlay(WebCamTexture device)
#endif
    {
#if UNITY_WEBGL
#else
        device.Play();
#endif
    }

#if UNITY_WEBGL
    void WebCamTextureStop(Texture device)
#else
    void WebCamTextureStop(WebCamTexture device)
#endif
    {
#if UNITY_WEBGL
#else
        device.Stop();
#endif
    }

#if UNITY_WEBGL
    void WebCamTexturePause(Texture device)
#else
    void WebCamTexturePause(WebCamTexture device)
#endif
    {
#if UNITY_WEBGL
#else
        device.Pause();
#endif
    }

#if UNITY_WEBGL
    bool WebCamTextureIsPlaying(Texture device)
#else
    bool WebCamTextureIsPlaying(WebCamTexture device)
#endif
    {
#if UNITY_WEBGL
        return false;
#else
        return device.isPlaying;
#endif
    }

#if UNITY_WEBGL
    Texture WebCamTextureNew(string deviceName)
#else
    WebCamTexture WebCamTextureNew(string deviceName)
#endif
    {
#if UNITY_WEBGL
        return null;
#else
        return new WebCamTexture(deviceName);
#endif
    }

#if UNITY_WEBGL
    string WebCamDeviceName(string device)
#else
    string WebCamDeviceName(WebCamDevice device)
#endif
    {
#if UNITY_WEBGL
        return null;
#else
        return device.name;
#endif
    }

    public void SetCamera(string deviceName, Renderer renderTarget)
    {
        if (m_CurrentCam != null)
        {
            WebCamTextureStop(m_CurrentCam);
        }
        m_CurrentCam = WebCamTextureNew(deviceName);

        SetRenderTarget(renderTarget);
    }

    public void SetRenderTarget(Renderer renderTarget)
    {
        m_RenderTarget = renderTarget;
        if (m_RenderTarget != null)
        {
            m_RenderTarget.material.mainTexture = m_CurrentCam;
        }
        else
        {
            Debug.LogWarning("No render target set so the video won't be shown.  Call SetRenderTarget in order to set one");
        }
    }

    public void StartRecording()
    {
        if (m_CurrentCam != null)
        {
            WebCamTexturePlay(m_CurrentCam);
        }
        else
        {
            Debug.LogError("No camera set. Call SetCamera to set one");
        }
    }

    public void PauseRecording()
    {
        if (m_CurrentCam != null)
        {
            WebCamTexturePause(m_CurrentCam);
        }
        else
        {
            Debug.LogError("No camera set. Call SetCamera to set one");
        }
    }

    public void StopRecording()
    {
        if (m_CurrentCam != null)
        {
            WebCamTextureStop(m_CurrentCam);
        }
        else
        {
            Debug.LogError("No camera set. Call SetCamera to set one");
        }
    }

    public void PrintConnectedDevices()
    {
        foreach (var device in WebCamTextureDevices())
            Debug.Log("Device Name: " + WebCamDeviceName(device));
    }

    /// <summary>
    ///
    /// </summary>
    public void SwitchCameras()
    {
        if (m_CurrentCam != null)
        {
            for (int i = 0; i < ConnectedDevices.Length; i++)
            {
                if (WebCamDeviceName(ConnectedDevices[i]) == CurrentCameraName)
                {
                    int newIndex = (i + 1) % NumConnectedDevices;
                    SetCamera(WebCamDeviceName(ConnectedDevices[newIndex]), m_RenderTarget);
                }
            }
        }
        else
        {
            SetDefaultCamera();
        }
    }
    #endregion
}
}

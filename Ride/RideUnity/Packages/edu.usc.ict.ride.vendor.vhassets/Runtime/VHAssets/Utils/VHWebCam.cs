using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class VHWebCam : MonoBehaviour
{
    /// <summary>
    /// The texture coming from the web cam
    /// </summary>
    WebCamTexture camTex;

    /// <summary>
    /// The renderer that displays the data from the webcam
    /// </summary>
    public Renderer renderTarget;
    public Material renderMaterial;
    public bool playAtStart = true;

    public int currentDevice;

    public int texWidth { get { return camTex != null ? camTex.width : 0; } }
    public int texHeight { get { return camTex != null ? camTex.height : 0; } }
    public string[] deviceNames { get; private set; }
    public int numDevices { get { return WebCamTexture.devices.Length; } }
    bool IsAllowed { get { return Application.HasUserAuthorization(UserAuthorization.WebCam); } }
    public bool isPlaying { get { return camTex != null ? camTex.isPlaying : false; } }


    void Awake()
    {
        if (IsAllowed)
        {
            deviceNames = new string[numDevices];
            for (int i = 0; i < numDevices; i++)
            {
                deviceNames[i] = WebCamTexture.devices[i].name;
            }

            if (deviceNames.Length > 0)
            {
                SetCurrentDevice(0);

                if (playAtStart)
                {
                    Play();
                }
            }
        }
        else
        {
            deviceNames = new string[0];
            enabled = false;
        }
    }

    //void Start()
    //{

    //}

    public void SetupTexture(string deviceName)
    {
        if (camTex != null && camTex.isPlaying)
        {
            camTex.Stop();
        }
        camTex = new WebCamTexture(deviceName);
        renderMaterial = renderMaterial != null ? renderMaterial : renderTarget.material;
        renderMaterial.mainTexture = camTex;
    }

    public void SetCurrentDevice(string deviceName)
    {
        int index = GetDeviceIndex(deviceName);
        if (index != -1)
        {
            SetCurrentDevice(index);
        }
    }

    public void SetCurrentDevice(int deviceIndex)
    {
        deviceIndex = Mathf.Clamp(deviceIndex, 0, Mathf.Max(numDevices - 1, 0));
        currentDevice = deviceIndex;
        if (numDevices > 0)
        {
            SetupTexture(deviceNames[deviceIndex]);
        }
    }

    public int GetDeviceIndex(string deviceName)
    {
        int index = Array.FindIndex<string>(deviceNames, d => string.Compare(d, deviceName) == 0);
        if (index == -1)
        {
            Debug.LogErrorFormat("Failed to find web cam with name {0}", deviceName);
        }
        return index;
    }

    public Color GetPixel(int x, int y)
    {
        return camTex.GetPixel(x, y);
    }

    public Color[] GetPixels()
    {
        return camTex.GetPixels();
    }

    public Color32[] GetPixels32()
    {
        return camTex.GetPixels32();
    }

    public void Play()
    {
        if (camTex != null) camTex.Play();
    }

    public void Stop()
    {
        if (camTex != null) camTex.Stop();
    }

    public void Pause()
    {
        if (camTex != null) camTex.Pause();
    }

    public void PrintRecordingDevices()
    {
#if !UNITY_WSA
        Array.ForEach<string>(deviceNames, rd => Debug.Log(rd));
#else
        Debug.LogErrorFormat("VHWebCam.PrintRecordingDevices() - not implemented on this platform.");
#endif
    }
}
}

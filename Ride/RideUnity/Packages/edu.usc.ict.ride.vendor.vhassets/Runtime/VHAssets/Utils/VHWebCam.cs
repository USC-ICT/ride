using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class VHWebCam : MonoBehaviour
{
    public enum CameraFacing
    {
        Any,
        Front,
        Back
    }

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
    public CameraFacing preferredFacing = CameraFacing.Any;
    public bool debugForceNativeOrientationCorrection;
    public int debugVideoRotationAngle;
    public bool debugVideoVerticallyMirrored;

    public int currentDevice;
    bool m_isInitializing;
    bool m_isInitialized;
    bool m_playWhenReady;
    const float DeviceRefreshTimeoutSeconds = 2.0f;

    public int texWidth { get { return camTex != null ? camTex.width : 0; } }
    public int texHeight { get { return camTex != null ? camTex.height : 0; } }
    public string[] deviceNames { get; private set; }
    public int numDevices { get { return WebCamTexture.devices.Length; } }
    bool IsAllowed { get { return Application.HasUserAuthorization(UserAuthorization.WebCam); } }
    public bool isPlaying { get { return camTex != null ? camTex.isPlaying : false; } }
    public int videoRotationAngle { get { return camTex != null ? camTex.videoRotationAngle : 0; } }
    public bool videoVerticallyMirrored { get { return camTex != null && camTex.videoVerticallyMirrored; } }
    public int effectiveVideoRotationAngle { get { return debugForceNativeOrientationCorrection ? debugVideoRotationAngle : videoRotationAngle; } }
    public bool effectiveVideoVerticallyMirrored { get { return debugForceNativeOrientationCorrection ? debugVideoVerticallyMirrored : videoVerticallyMirrored; } }
    public bool nativeOrientationCorrectionEnabled { get { return ShouldApplyNativeOrientationCorrection(); } }
    public bool isFrontFacing
    {
        get
        {
            var devices = WebCamTexture.devices;
            return currentDevice >= 0 && currentDevice < devices.Length && devices[currentDevice].isFrontFacing;
        }
    }


    void Awake()
    {
        deviceNames = new string[0];
        InitializeRenderMaterial();
        BeginInitialize(playAtStart);
    }

    /// <summary>
    /// Starts webcam initialization if needed and records whether playback should begin once initialization completes.
    /// </summary>
    /// <param name="playWhenReady">Whether the webcam should start playing after initialization succeeds.</param>
    void BeginInitialize(bool playWhenReady)
    {
        m_playWhenReady |= playWhenReady;

        if (m_isInitialized)
        {
            if (m_playWhenReady)
            {
                m_playWhenReady = false;
                Play();
            }

            return;
        }

        if (!m_isInitializing)
            StartCoroutine(InitializeWebCam());
    }

    /// <summary>
    /// Requests webcam permission when needed, refreshes available devices, and optionally starts playback.
    /// </summary>
    /// <returns>Coroutine enumerator for asynchronous webcam initialization.</returns>
    IEnumerator InitializeWebCam()
    {
        m_isInitializing = true;

        if (!IsAllowed)
        {
            if (VHUtils.IsWebGL())
            {
                m_playWhenReady = false;
                m_isInitializing = false;
                yield break;
            }

            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!IsAllowed)
        {
            deviceNames = new string[0];
            m_playWhenReady = false;
            m_isInitializing = false;
            yield break;
        }

        float deviceRefreshEndTime = Time.realtimeSinceStartup + DeviceRefreshTimeoutSeconds;
        do
        {
            RefreshDeviceNames();
            if (deviceNames.Length > 0)
                break;

            yield return null;
        }
        while (Time.realtimeSinceStartup < deviceRefreshEndTime);

        if (deviceNames.Length > 0)
        {
            SetCurrentDevice(GetPreferredDeviceIndex());
            m_isInitialized = camTex != null;
        }

        m_isInitializing = false;

        bool shouldPlay = m_playWhenReady;
        m_playWhenReady = false;

        if (shouldPlay && camTex != null)
        {
            Play();
        }
    }

    /// <summary>
    /// Ensures the webcam render material is initialized from the render target when no explicit material was assigned.
    /// </summary>
    void InitializeRenderMaterial()
    {
        if (renderMaterial == null && renderTarget != null)
            renderMaterial = renderTarget.material;
    }

    /// <summary>
    /// Refreshes the cached list of connected Unity webcam device names.
    /// </summary>
    void RefreshDeviceNames()
    {
        var devices = WebCamTexture.devices;
        deviceNames = new string[devices.Length];
        for (int i = 0; i < devices.Length; i++)
            deviceNames[i] = devices[i].name;
    }

    int GetPreferredDeviceIndex()
    {
        var devices = WebCamTexture.devices;
        CameraFacing facing = GetRuntimePreferredFacing();
        for (int i = 0; i < devices.Length; i++)
        {
            if (MatchesPreferredFacing(devices[i], facing))
                return i;
        }

        return 0;
    }

    CameraFacing GetRuntimePreferredFacing()
    {
        if (preferredFacing != CameraFacing.Any)
            return preferredFacing;

        return ShouldApplyNativeOrientationCorrection() ? CameraFacing.Front : CameraFacing.Any;
    }

    bool MatchesPreferredFacing(WebCamDevice device, CameraFacing facing)
    {
        switch (facing)
        {
            case CameraFacing.Front:
                return device.isFrontFacing;
            case CameraFacing.Back:
                return !device.isFrontFacing;
            default:
                return true;
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
        InitializeRenderMaterial();
        if (renderMaterial != null)
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
        if (deviceNames == null || deviceNames.Length <= 0)
            return;

        deviceIndex = Mathf.Clamp(deviceIndex, 0, deviceNames.Length - 1);
        currentDevice = deviceIndex;
        SetupTexture(deviceNames[deviceIndex]);
    }

    /// <summary>
    /// Sets the preferred webcam facing and switches to the best matching connected device.
    /// </summary>
    /// <param name="facing">The desired webcam facing preference.</param>
    public void SetPreferredFacing(CameraFacing facing)
    {
        preferredFacing = facing;
        if (deviceNames != null && deviceNames.Length > 0)
            SetCurrentDevice(GetPreferredDeviceIndex());
    }

    /// <summary>
    /// Overrides mobile-native webcam orientation correction for editor and desktop diagnostics.
    /// </summary>
    /// <param name="enabled">Whether native orientation correction should be forced on this runtime.</param>
    public void SetDebugNativeOrientationCorrection(bool enabled)
    {
        debugForceNativeOrientationCorrection = enabled;
    }

    /// <summary>
    /// Sets simulated webcam orientation metadata for editor and desktop diagnostics.
    /// </summary>
    /// <param name="rotationAngle">Clockwise rotation angle to use when debug orientation correction is forced.</param>
    /// <param name="verticallyMirrored">Whether the debug source pixels should be vertically mirrored before rotation.</param>
    public void SetDebugOrientationMetadata(int rotationAngle, bool verticallyMirrored)
    {
        debugVideoRotationAngle = rotationAngle;
        debugVideoVerticallyMirrored = verticallyMirrored;
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

    /// <summary>
    /// Gets the current webcam pixels with mobile native rotation and mirroring applied.
    /// </summary>
    /// <param name="width">Receives the width of the returned pixel buffer.</param>
    /// <param name="height">Receives the height of the returned pixel buffer.</param>
    /// <returns>Webcam pixels in bottom-left-origin order, or an empty array if no valid frame is available.</returns>
    public Color32[] GetOrientedPixels32(out int width, out int height)
    {
        width = texWidth;
        height = texHeight;

        if (camTex == null || width <= 0 || height <= 0)
            return new Color32[0];

        if (!ShouldApplyNativeOrientationCorrection())
            return camTex.GetPixels32();

        return OrientPixels(camTex.GetPixels32(), width, height, effectiveVideoRotationAngle, effectiveVideoVerticallyMirrored, out width, out height);
    }

    /// <summary>
    /// Applies mobile native webcam rotation and mirroring metadata to a UI preview image.
    /// </summary>
    /// <param name="rawImage">The raw image displaying the active webcam texture.</param>
    public void ApplyRawImageOrientation(UnityEngine.UI.RawImage rawImage)
    {
        if (rawImage == null)
            return;

        if (!ShouldApplyNativeOrientationCorrection())
        {
            rawImage.rectTransform.localEulerAngles = Vector3.zero;
            rawImage.uvRect = new Rect(0, 0, 1, 1);
            return;
        }

        rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -effectiveVideoRotationAngle);
        rawImage.uvRect = effectiveVideoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
    }

    /// <summary>
    /// Normalizes a rotation angle to one of the right-angle rotations supported by webcam orientation correction.
    /// </summary>
    /// <param name="rotationAngle">Rotation angle in degrees.</param>
    /// <returns>A normalized rotation angle of 0, 90, 180, or 270; unsupported angles return 0.</returns>
    static int NormalizeRotation(int rotationAngle)
    {
        int rotation = ((rotationAngle % 360) + 360) % 360;
        return rotation % 90 == 0 ? rotation : 0;
    }

    /// <summary>
    /// Determines whether the current runtime needs native webcam orientation correction.
    /// </summary>
    /// <returns>True for mobile platforms that report camera orientation metadata; otherwise false.</returns>
    bool ShouldApplyNativeOrientationCorrection()
    {
        return debugForceNativeOrientationCorrection || VHUtils.IsIOS() || VHUtils.IsAndroid();
    }

    /// <summary>
    /// Rotates and vertically mirrors a webcam pixel buffer using Unity webcam orientation metadata.
    /// </summary>
    /// <param name="source">Source pixels in bottom-left-origin order.</param>
    /// <param name="sourceWidth">Width of the source pixel buffer.</param>
    /// <param name="sourceHeight">Height of the source pixel buffer.</param>
    /// <param name="rotationAngle">Clockwise rotation angle reported by the webcam texture.</param>
    /// <param name="verticallyMirrored">Whether the source pixels should be vertically mirrored before rotation.</param>
    /// <param name="width">Receives the width of the oriented pixel buffer.</param>
    /// <param name="height">Receives the height of the oriented pixel buffer.</param>
    /// <returns>Pixels transformed into the corrected orientation.</returns>
    static Color32[] OrientPixels(Color32[] source, int sourceWidth, int sourceHeight, int rotationAngle, bool verticallyMirrored, out int width, out int height)
    {
        int rotation = NormalizeRotation(rotationAngle);

        bool swapsDimensions = rotation == 90 || rotation == 270;
        width = swapsDimensions ? sourceHeight : sourceWidth;
        height = swapsDimensions ? sourceWidth : sourceHeight;

        Color32[] oriented = new Color32[source.Length];
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                int sourceY = verticallyMirrored ? sourceHeight - 1 - y : y;
                int destX;
                int destY;

                switch (rotation)
                {
                    case 90:
                        destX = sourceHeight - 1 - sourceY;
                        destY = x;
                        break;
                    case 180:
                        destX = sourceWidth - 1 - x;
                        destY = sourceHeight - 1 - sourceY;
                        break;
                    case 270:
                        destX = sourceY;
                        destY = sourceWidth - 1 - x;
                        break;
                    default:
                        destX = x;
                        destY = sourceY;
                        break;
                }

                oriented[destY * width + destX] = source[y * sourceWidth + x];
            }
        }

        return oriented;
    }

    public void Play()
    {
        if (camTex != null)
        {
            camTex.Play();
            return;
        }

        BeginInitialize(true);
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

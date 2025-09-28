using UnityEngine;

namespace VHAssets
{
public class TimedEvents : GenericEvents
{
    #region Functions
    public override string GetEventType() { return GenericEventNames.Timed; }
    #endregion

    #region Events
    public class TimedEvent_Base : ICutsceneEventInterface { }

    public class TimedEvent_FollowCurve : TimedEvent_Base
    {
        #region Functions
        public void FollowCurve(GameObject follower, Curve curve)
        {
            follower.transform.position = curve.GetPosition(m_InterpolationTime);
            follower.transform.forward = curve.GetForwardDirection(m_InterpolationTime, true);
        }

        public void FollowCurve(GameObject follower, Curve curve, Transform lookAtTarget)
        {
            follower.transform.position = curve.GetPosition(m_InterpolationTime);
            follower.transform.LookAt(lookAtTarget);
        }

        public void FollowCurve(string follower, string curve)
        {
            GameObject gameObj = GameObject.Find(follower);
            GameObject curveGo = GameObject.Find(curve);
            if (gameObj != null && curveGo != null)
            {
                gameObj.transform.position = curveGo.GetComponent<Curve>().GetPosition(m_InterpolationTime);
                gameObj.transform.forward = curveGo.GetComponent<Curve>().GetForwardDirection(m_InterpolationTime, true);
            }
        }

        public void FollowCurve(string follower, string curve, string lookAtTarget)
        {
            GameObject gameObj = GameObject.Find(follower);
            GameObject curveGo = GameObject.Find(curve);
            if (gameObj != null && curveGo != null)
            {
                gameObj.transform.position = curveGo.GetComponent<Curve>().GetPosition(m_InterpolationTime);
                gameObj.transform.forward = curveGo.GetComponent<Curve>().GetForwardDirection(m_InterpolationTime, true);

                GameObject lookAtGo = GameObject.Find(lookAtTarget);
                if (lookAtGo != null)
                {
                    gameObj.transform.LookAt(lookAtGo.transform);
                }
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_FadeLight : TimedEvent_Base
    {
        #region Functions
        public void FadeLight(Light light, Color startColor, Color endColor)
        {
            light.color = Color.Lerp(startColor, endColor, m_InterpolationTime);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startColor")
            {
                param.colorData = Color.white;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_FadeLightIntensity : TimedEvent_Base
    {
        #region Functions
        public void FadeLightIntensity(Light light, float startIntensity, float endIntensity)
        {
            light.intensity = Mathf.Lerp(startIntensity, endIntensity, m_InterpolationTime);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startIntensity")
            {
                param.floatData = 1;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_FadeRenderer : TimedEvent_Base
    {
        #region Functions
        public void FadeRenderer(Renderer renderer, Color startColor, Color endColor)
        {
            if (Application.isPlaying)
            {
                renderer.material.color = Color.Lerp(startColor, endColor, m_InterpolationTime);
            }
            else
            {
                renderer.sharedMaterial.color = Color.Lerp(startColor, endColor, m_InterpolationTime);
            }
        }

        public void FadeRenderer(Renderer renderer, Color startColor, Color endColor, int materialIndex)
        {
            Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
            mats[materialIndex].color = Color.Lerp(startColor, endColor, m_InterpolationTime);
            renderer.materials = mats;
        }

        public void FadeRenderer(string renderer, Color startColor, Color endColor)
        {
            Renderer rendComp = GetComponentFromString<Renderer>(renderer);
            if (rendComp != null)
            {
                FadeRenderer(rendComp, startColor, endColor);
            }
        }

        public void FadeRenderer(string renderer, Color startColor, Color endColor, int materialIndex)
        {
            Renderer rendComp = GetComponentFromString<Renderer>(renderer);
            if (rendComp != null)
            {
                FadeRenderer(rendComp, startColor, endColor, materialIndex);
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startColor")
            {
                param.colorData = Color.white;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_FadeGUITexture : TimedEvent_Base
    {
        #region Functions
        public void FadeGUITexture(UnityEngine.UI.Image texture, Color startColor, Color endColor)
        {
            texture.color = Color.Lerp(startColor, endColor, m_InterpolationTime);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startColor")
            {
                param.colorData = Color.white;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_FadeAudio : TimedEvent_Base
    {
        #region Functions
        public void FadeAudio(AudioSource source, float startVolume, float endVolume)
        {
            source.volume = Mathf.Lerp(startVolume, endVolume, m_InterpolationTime);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startVolume")
            {
                param.floatData = 1;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_Rotate : TimedEvent_Base
    {
        #region Functions
        public void Rotate(Transform transform, Vector3 startRotation, Vector3 endRotation)
        {
            transform.localRotation = Quaternion.Euler(Vector3.Lerp(startRotation, endRotation, Mathf.Clamp01(m_InterpolationTime)));
        }

        public void Rotate(Transform transform, Vector3 startRotation, Transform endRotation)
        {
            transform.localRotation = Quaternion.Euler(Vector3.Lerp(startRotation, endRotation.rotation.eulerAngles, Mathf.Clamp01(m_InterpolationTime)));
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "endRotation")
            {
                param.vec3Data = new Vector3(90, 90, 90);
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_Translate : TimedEvent_Base
    {
        #region Functions
        public void Translate(Transform transform, Vector3 startPosition, Vector3 endPosition)
        {
            transform.localPosition = Vector3.Lerp(startPosition, endPosition, Mathf.Clamp01(m_InterpolationTime));
        }

        public void Translate(Transform transform, Vector3 startPosition, Transform endPosition)
        {
            transform.localPosition = Vector3.Lerp(startPosition, endPosition.position, Mathf.Clamp01(m_InterpolationTime));
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "endPosition")
            {
                param.vec3Data = new Vector3(10, 0, 0);
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_Scale : TimedEvent_Base
    {
        #region Functions
        public void Scale(Transform transform, Vector3 startScale, Vector3 endScale)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Clamp01(m_InterpolationTime));
        }

        public void Scale(Transform transform, Vector3 startScale, Transform endScale)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale.localScale, Mathf.Clamp01(m_InterpolationTime));
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startScale")
            {
                param.vec3Data = Vector3.one;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }

    public class TimedEvent_CameraFOV : TimedEvent_Base
    {
        #region Functions
        public void CameraFOV(Camera camera, float startFov, float endFov)
        {
            camera.fieldOfView = Mathf.Lerp(startFov, endFov, Mathf.Clamp01(m_InterpolationTime));
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startFov")
            {
                param.floatData = 60;
            }
            else if (param.Name == "endFov")
            {
                param.floatData = 90;
            }
        }

        public override bool IsFireAndForget() { return false; }
        #endregion
    }
    #endregion
}
}

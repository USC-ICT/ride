using UnityEngine;

namespace VHAssets
{
//[ExecuteInEditMode]
public class AudioEvents : GenericEvents
{
    #region Functions
    public override string GetEventType() { return GenericEventNames.Audio; }
    #endregion

    #region Events
    public class AudioEvent_Base : ICutsceneEventInterface
    {
        protected AudioSource GetAudioSource(CutsceneEvent ce)
        {
            AudioSource src = null;
            if (!IsParamNull(ce, 0))
            {
                src = Cast<AudioSource>(ce, 0);
            }
            else
            {
                src = GetAudioSource(Param(ce, 0).stringData);
            }
            return src;
        }

        protected AudioSource GetAudioSource(string goName)
        {
            AudioSource src = null;
            GameObject audioSrc = GameObject.Find(goName);
            if (audioSrc != null)
            {
                src = audioSrc.GetComponent<AudioSource>();
                if (src == null)
                {
                    Debug.LogError(goName + " doesn't have an audio source");
                }
            }
            else
            {
                Debug.LogError("Can't find gameobject " + goName);
            }
            return src;
        }
    }

    public class AudioEvent_PlaySound : AudioEvent_Base
    {
        #region Functions
        public void PlaySound(AudioSource source, AudioClip clip)
        {
            source.clip = clip;
            source.time = m_InterpolationTime * clip.length;
            source.Play();
        }

        public void PlaySound(string source, AudioClip clip)
        {
            AudioSource src = GetAudioSource(source);
            if (src != null)
            {
                PlaySound(src, clip);
            }
        }

        public override void Pause (CutsceneEvent ce)
        {
            AudioSource src = GetAudioSource(ce);
            if (src != null)
            {
                src.Pause();
            }
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            AudioSource src = null;
            if (IsParamNull(ce, 0))
            {
                GameObject audioSrc = GameObject.Find(Param(ce, 0).stringData);
                if (audioSrc != null)
                {
                    src = audioSrc.GetComponent<AudioSource>();
                }
            }
            else
            {
                src = Cast<AudioSource>(ce, 0);
            }
            return src;
        }

         public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            if (rData != null)
            {
                AudioSource src = null;
                if (IsParamNull(ce, 0))
                {
                    GameObject audioSrc = GameObject.Find(Param(ce, 0).stringData);
                    if (audioSrc != null)
                    {
                        src = audioSrc.GetComponent<AudioSource>();
                    }
                }
                else
                {
                    src = Cast<AudioSource>(ce, 0);
                }

                if (src != null)
                {
                    src.Stop();
                }
            }
        }

        public override string GetLengthParameterName() { return "clip"; }

        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }
        #endregion
    }

    public class AudioEvent_StopSound : AudioEvent_Base
    {
        #region Functions
        public void StopSound(AudioSource source)
        {
            source.Stop();
        }
        #endregion
    }

    public class AudioEvent_SetVolume : AudioEvent_Base
    {
        #region Functions
        public void SetVolume(AudioSource source, float volume)
        {
            source.volume = volume;
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            return Cast<AudioSource>(ce, 0).volume;
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            SetVolume(Cast<AudioSource>(ce, 0), (float)rData);
        }
        #endregion
    }

    public class AudioEvent_SetPitch : AudioEvent_Base
    {
        #region Functions
        public void SetPitch(AudioSource source, float pitch)
        {
            source.pitch = pitch;
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            return Cast<AudioSource>(ce, 0).pitch;
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            SetPitch(Cast<AudioSource>(ce, 0), (float)rData);
        }
        #endregion
    }

    public class AudioEvent_SetPriority : AudioEvent_Base
    {
        #region Functions
        public void SetPriority(AudioSource source, int priority)
        {
            source.priority = priority;
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            return Cast<AudioSource>(ce, 0).priority;
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            SetPriority(Cast<AudioSource>(ce, 0), (int)rData);
        }
        #endregion
    }
    #endregion
}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.Audio
{
    /// <inheritdoc cref="IAudioSystem"/>
    public class AudioSystemUnity : RideSystemMonoBehaviour, IAudioSystem
    {
        class RideAudioSource
        {
            public AudioSource source;
        }

        public int m_numAudioSourcesToCreate = 5;

        ResourceLoaderSystem m_resourceLoader;

        Dictionary<RideID, RideAudioSource> m_sources = new Dictionary<RideID, RideAudioSource>();

        GameObject m_genericSourceParent;
        List<RideID> m_genericSources = new List<RideID>();

        public override void SystemInit()
        {
            base.SystemInit();

            m_resourceLoader = Globals.api.GetSystem<ResourceLoaderSystem>();

            m_genericSourceParent = new GameObject("AudioSourceParent");
            m_genericSourceParent.transform.SetParent(this.transform);

            for (int i = 0; i < m_numAudioSourcesToCreate; i++)
            {
                GameObject sourceObj = new GameObject("GenericAudioSource" + i.ToString(), typeof(AudioSource));
                sourceObj.transform.SetParent(m_genericSourceParent.transform);
                RideID genericSourceId = IdentityFactory.CreateId();
                m_genericSources.Add(genericSourceId);
                m_sources.Add(genericSourceId, new RideAudioSource { source = sourceObj.GetComponent<AudioSource>() });
            }

            FindAllAudioSources();
        }

        public void FindAllAudioSources()
        {
            AudioSource[] sources = GameObject.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                RideAudioSource source = m_sources.Values.FirstOrDefault(s => s.source == sources[i]);
                if (source == null)
                {
                    // we don't already have this one cached, add it
                    m_sources.Add(IdentityFactory.CreateId(), new RideAudioSource { source = sources[i] });
                }
            }
        }

        public override void SystemShutdown()
        {
            base.SystemShutdown();

            GameObject.Destroy(m_genericSourceParent);
        }

        RideID GetAvailableGenericSource()
        {
            for (int i = 0; i < m_genericSources.Count; i++)
            {
                if (!GetSource(m_genericSources[i]).source.isPlaying)
                {
                    return m_genericSources[i];
                }
            }

            RideLog.LogError("There are no available generic audio sources");
            return RideID.Null;
        }

        RideAudioSource GetSource(RideID source)
        {
            if (m_sources.ContainsKey(source))
            {
                return m_sources[source];
            }
            RideLog.LogError($"Failed to find source {source}");
            return null;
        }

        public RideID Play(string clip)
        {
            RideID source = GetAvailableGenericSource();
            return Play(source, clip);
        }

        public RideID Play(RideID source, string clip)
        {
            return PlayInternal(source, clip);
        }

        RideID PlayInternal(RideID source, string clip)
        {
            AudioClip c = m_resourceLoader.GetAudioClip(clip);
            if (c != null)
            {
                RideAudioSource src = GetSource(source);
                src?.source.PlayOneShot(c, 1);
            }
            return source;
        }

        public void PlayAtPosition(string clip, RideVector3 pos)
        {
            AudioClip c = m_resourceLoader.GetAudioClip(clip);
            if (c != null)
            {
                AudioSource.PlayClipAtPoint(c, pos);
            }
        }

        public void Stop(RideID source)
        {
            RideAudioSource src = GetSource(source);
            src?.source.Stop();
        }

        public bool IsPlaying(RideID source)
        {
            RideAudioSource src = GetSource(source);
            if (src != null)
            {
                return src.source.isPlaying;
            }
            else {
                RideLog.LogError($"Failed to find source {source}");
                return false;
            };
        }

        public void LoadAudioFile(string pathOrUrl, Action<AudioClip> onComplete)
        {
            StartCoroutine(LoadAudioFileCoroutine(pathOrUrl, onComplete));
        }

        /// <summary>
        /// Loads an audio file from either a local file path or a remote URL and returns an <see cref="AudioClip"/> via callback.
        /// </summary>
        /// <param name="pathOrUrl">
        /// The path to the local audio file (e.g., C:/path/to/file.mp3) or a remote HTTP/HTTPS URL.
        /// Supported extensions include .mp3, .ogg, and .wav.
        /// For local files, <c>file://</c> is automatically prepended on platforms that require it.
        /// </param>
        /// <param name="onComplete">
        /// Callback invoked when the operation completes. If successful, the resulting <see cref="AudioClip"/> is passed; otherwise, <c>null</c> is passed on failure.
        /// </param>
        /// <remarks>
        /// This coroutine-compatible version is used for WebGL and other Unity platforms that do not support <c>async/await</c>.
        /// Local file access is supported on platforms that allow file I/O (e.g., Windows, macOS, Android, iOS).
        /// WebGL builds can only load audio from remote URLs.
        /// </remarks>
        /// <example>
        /// <code>
        /// StartCoroutine(LoadAudioFileCoroutine("https://example.com/audio.mp3", (clip) =>
        /// {
        ///     if (clip != null)
        ///     {
        ///         audioSource.clip = clip;
        ///         audioSource.Play();
        ///     }
        /// }));
        /// </code>
        /// </example>
        public IEnumerator LoadAudioFileCoroutine(string pathOrUrl, Action<AudioClip> onComplete)
        {
            if (string.IsNullOrEmpty(pathOrUrl))
            {
                Debug.LogError("LoadAudioFile() - pathOrUrl is null or empty");
                onComplete?.Invoke(null);
                yield break;
            }

            var audioType = AudioType.WAV;
            string extension = Path.GetExtension(pathOrUrl).ToLowerInvariant();
            switch (extension)
            {
                case ".mp3": audioType = AudioType.MPEG; break;
                case ".ogg": audioType = AudioType.OGGVORBIS; break;
                case ".wav": audioType = AudioType.WAV; break;
            }

            string finalUrl;

            if (pathOrUrl.StartsWith("http://") || pathOrUrl.StartsWith("https://"))
            {
                finalUrl = pathOrUrl;  // Remote URL
            }
            else
            {
                // local file path
                if (!File.Exists(pathOrUrl))
                {
                    Debug.LogError($"Audio file not found: {pathOrUrl}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                // Normalize path and prepend file://
                string normalizedPath = pathOrUrl.Replace("\\", "/");
                finalUrl = normalizedPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                        ? normalizedPath
                        : "file://" + normalizedPath;
            }

            using (var www = UnityWebRequestMultimedia.GetAudioClip(finalUrl, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load audio: {www.error}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);
                onComplete?.Invoke(clip);
            }
        }
    }
}

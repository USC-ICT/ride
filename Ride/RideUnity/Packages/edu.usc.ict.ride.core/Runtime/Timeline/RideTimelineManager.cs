using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VHAssets;

namespace Ride.Timeline
{
    public class RideTimelineManager : RideMonoBehaviour
    {
        public delegate void TimelineNotificationHandler(string message);
        public event TimelineNotificationHandler OnTimelineNotification;

        public Dictionary<string, EntityData> m_entityDictionary = new Dictionary<string, EntityData>();    //-Key: Entity Name, Value: EntityData
        private Dictionary<PlayableDirector, double> m_pausedDirectors = new Dictionary<PlayableDirector, double>();  // Key: Timeline director, Value: Time is was paused
        private List<PlayableDirector> m_directors = new List<PlayableDirector>();
        private List<MecanimCharacter> m_cutsceneCharacters = new List<MecanimCharacter>();        

        protected override void Start()
        {
            PopulateDirectorList();
            PopulateCharacterList();
        }

        public void ProcessClip(RideTimelineBehaviour behaviour)
        {
            behaviour.ProcessBehaviour();
            StartCoroutine(behaviour.ProcessContinuousBehaviour());
        }

        public RideID GetEntityID(string entityName)
        {
            return m_entityDictionary[entityName].m_rideID;
        }

        public void ClearEntityDictionary()
        {
            m_entityDictionary.Clear();
        }

        public void PopulateDirectorList()
        {
            m_directors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None).ToList();
        }

        private void PopulateCharacterList()
        {
            m_cutsceneCharacters.Clear();
            //List<GameObject> rootObjects = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            //foreach (GameObject obj in rootObjects)
            //    SearchForComponentRecursive<MecanimCharacter>(obj.transform, m_cutsceneCharacters);
            m_cutsceneCharacters = FindObjectsByType<MecanimCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        }

        private void SearchForComponentRecursive<T>(Transform parent, List<T> componentList)
        {
            if (parent.TryGetComponent<T>(out T component))
                componentList.Add(component);

            foreach (Transform child in parent)
                SearchForComponentRecursive<T>(child, componentList);
        }

        public void PlayTimeline(string directorName)
        {
            PlayTimeline(null, directorName);
        }

        public void PlayTimeline(Transform parent, string directorName)
        {
            List<PlayableDirector> directors = (parent == null) ? GetAllDirectors() : parent.GetComponentsInChildren<PlayableDirector>().ToList();

            foreach (var director in directors)
            {
                if (director.playableAsset == null)
                    continue;
                if (director.gameObject.name == directorName)
                {
                    director.Play();
                    return;
                }
            }

            Debug.LogWarning($"RideTimelineManager.cs::Failed to find director '{directorName}'");
        }

        public void PauseTimelines(PlayableDirector _director)
        {
            // Pause the director that unresolved clip is bound to
            if (m_pausedDirectors.ContainsKey(_director))
            {
                _director.time = m_pausedDirectors[_director];
            }
            else
            {
                m_pausedDirectors.Add(_director, _director.time);
            }

            // Pause possible parent director.
            foreach (var director in GetAllDirectors())
            {
                if (director.playableAsset == null) continue;
                if (director.state != PlayState.Playing) continue;

                foreach (var track in director.playableAsset.outputs)
                {
                    if (track.sourceObject is ControlTrack == false) continue;

                    ControlTrack controlTrack = (ControlTrack)track.sourceObject;
                    foreach (TimelineClip clip in controlTrack.GetClips())
                    {
                        if (clip.displayName != _director.name) continue;

                        if (m_pausedDirectors.ContainsKey(director))
                        {
                            director.time = m_pausedDirectors[director];
                        }
                        else
                        {
                            m_pausedDirectors.Add(director, director.time);
                        }
                    }
                }
            }
        }

        public void ResumeTimelines()
        {
            m_pausedDirectors.Clear();
        }

        /// <summary>
        /// Check if any Timeline director with matching name is playing.
        /// Prone to bug if there are Timelines with identical name.
        /// </summary>
        public bool IsTimelinePlaying(string directorName)
        {
            return IsTimelinePlaying(null, directorName);
        }

        /// <summary>
        /// Check if Timeline director within the parent object is playing
        /// </summary>
        public bool IsTimelinePlaying(Transform parent, string directorName)
        {
            List<PlayableDirector> directors = (parent == null) ? GetAllDirectors() : parent.GetComponentsInChildren<PlayableDirector>().ToList();
            var director = directors.FirstOrDefault(x => x.gameObject.name == directorName);
            if (director == null)
            {
                Debug.LogWarning($"RideTimelineManager.cs::IsTimelinePlaying() - Failed to find Timeline '{directorName}'.");
                return false;
            }

            return (director.state == PlayState.Playing) ? true : false;
        }

        public bool IsTimelinePlaying()
        {
            foreach (var director in GetAllDirectors())
            {
                if (director.state == PlayState.Playing)
                    return true;
            }
            return false;
        }

        public void StopAllTimelines()
        {
            foreach (var director in GetAllDirectors())
            {
                director.Stop();
            }
        }

        public List<PlayableDirector> GetAllDirectors()
        {
            return FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None).ToList();
        }

        public void ResetDirectors()
        {
            m_directors.Clear();
            PopulateDirectorList();
        }

        public MecanimCharacter GetCharacter(string name)
        {
            PopulateCharacterList();

            foreach (var character in m_cutsceneCharacters)
            {
                if (character.name == name)
                    return character;
            }

            Debug.LogWarning($"TimelineManager.cs::GetCharacter() - Failed to find '{name}'");
            return null;
        }

        public PlayableDirector GetDirector(string directorName, Transform parentObject = null)
        {
            List<PlayableDirector> directorList;
            if (parentObject == null)
            {
                ResetDirectors();
                directorList = m_directors;
            }
            else
            {
                directorList = parentObject.GetComponentsInChildren<PlayableDirector>().ToList();
            }

            foreach (var director in directorList)
            {
                if (director.name == directorName)
                    return director;
            }

            Debug.LogWarning($"TimelineManager.cs::GetDirector() - Failed to find '{directorName}'");
            return null;
        }

        public void SendNotification(string message)
        {
            OnTimelineNotification.Invoke(message);
        }

        public void OverwriteCharacterName(string directorName, string characterName, Transform parentObject = null)
        {
            PlayableDirector director = GetDirector(directorName, parentObject);
            if (director == null) { return; }

            foreach (var output in director.playableAsset.outputs)
            {
                if (output.sourceObject is ControlTrack controlTrack)
                {
                    foreach (var clip in controlTrack.GetClips())
                    {
                        OverwriteCharacterName(clip.displayName, characterName, parentObject);
                    }
                }

                else if (output.sourceObject is RideTimelineTrack track)
                {
                    foreach (var clip in track.GetClips())
                    {
                        Clip_vhPlayAudio audioClip = clip.asset as Clip_vhPlayAudio;
                        if (audioClip != null) { audioClip.m_behaviour.m_characterName = characterName; continue; }

                        Clip_vhBodyMovement bodyClip = clip.asset as Clip_vhBodyMovement;
                        if (bodyClip != null) { bodyClip.m_behaviour.m_characterName = characterName; continue; }

                        Clip_vhHeadMovement headClip = clip.asset as Clip_vhHeadMovement;
                        if (headClip != null) { headClip.m_behaviour.m_characterName = characterName; continue; }

                        Clip_vhFaceAnimation faceClip = clip.asset as Clip_vhFaceAnimation;
                        if (faceClip != null) { faceClip.m_behaviour.m_characterName = characterName; continue; }

                        foreach (var childTracks in clip.GetParentTrack().GetChildTracks())
                        {
                            foreach (var childClip in childTracks.GetClips())
                            {
                                Clip_vhPlayAudio audioClip_c = childClip.asset as Clip_vhPlayAudio;
                                if (audioClip_c != null) { audioClip_c.m_behaviour.m_characterName = characterName; continue; }

                                Clip_vhBodyMovement bodyClip_c = childClip.asset as Clip_vhBodyMovement;
                                if (bodyClip_c != null) { bodyClip_c.m_behaviour.m_characterName = characterName; continue; }

                                Clip_vhHeadMovement headClip_c = childClip.asset as Clip_vhHeadMovement;
                                if (headClip_c != null) { headClip_c.m_behaviour.m_characterName = characterName; continue; }

                                Clip_vhFaceAnimation faceClip_c = childClip.asset as Clip_vhFaceAnimation;
                                if (faceClip_c != null) { faceClip_c.m_behaviour.m_characterName = characterName; continue; }
                            }
                        }
                    }
                }
            }
        }
    }

    public class EntityData
    {
        public EntityData(RideID id, EntityHierarchy hierarchy, Team team, string name)
        {
            m_rideID = id;
            m_hierarchy = hierarchy;
            m_team = team;
            m_name = name;
        }

        public RideID m_rideID;
        public EntityHierarchy m_hierarchy;
        public Team m_team;
        public string m_name;
    }

    public enum EntityHierarchy
    {
        Agent,
        Fireteam,
        Squad,
        Company,
        Platoon,
        Mount,
    }

    public enum GroupBehaviour
    {
        MoveInFormation = 0,
        Ambush_Initiate = 1,
        Ambush_React = 2,
    }

    public enum MountTypes
    {
        M4A1Sherman = 0,
        M777 = 1,
        Humvee = 2,
    }
}


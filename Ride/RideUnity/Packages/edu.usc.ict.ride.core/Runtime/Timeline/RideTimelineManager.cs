using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VHAssets;

namespace Ride.Timeline
{
    /// <summary>Coordinates RIDE Timeline clip execution, director lookup, virtual-human lookup, and blocking clip playback.</summary>
    /// <remarks>
    /// <see cref="RideTimelineTrackMixer"/> delegates active clips to this manager. Virtual-human Timeline behaviors
    /// also use it to resolve <see cref="MecanimCharacter"/> instances and retarget generated cutscenes by name.
    /// See <see href="https://docs.unity3d.com/ScriptReference/Playables.PlayableDirector.html"/> for director behavior.
    /// </remarks>
    public class RideTimelineManager : RideMonoBehaviour
    {
        /// <summary>Represents a notification emitted by a Timeline clip.</summary>
        /// <param name="message">The notification payload.</param>
        public delegate void TimelineNotificationHandler(string message);

        /// <summary>Occurs when <see cref="SendNotification"/> forwards a Timeline notification.</summary>
        public event TimelineNotificationHandler OnTimelineNotification;

        /// <summary>Entity metadata indexed by the names referenced in Timeline content.</summary>
        [NonSerialized] public Dictionary<string, EntityData> m_entityDictionary = new Dictionary<string, EntityData>();    //-Key: Entity Name, Value: EntityData

        /// <summary>Directors paused for unresolved blocking clips and the time at which each director was paused.</summary>
        private Dictionary<PlayableDirector, double> m_pausedDirectors = new Dictionary<PlayableDirector, double>();  // Key: Timeline director, Value: Time is was paused

        /// <summary>The cached directors used by name-based lookup.</summary>
        private List<PlayableDirector> m_directors = new List<PlayableDirector>();

        /// <summary>The cached virtual-human characters available to Timeline clips.</summary>
        private List<MecanimCharacter> m_cutsceneCharacters = new List<MecanimCharacter>();        

        /// <inheritdoc/>
        protected override void Start()
        {
            PopulateDirectorList();
            PopulateCharacterList();
        }

        /// <summary>Starts the immediate and continuous processing for an activated Timeline behavior.</summary>
        /// <param name="behaviour">The behavior belonging to the active clip.</param>
        public void ProcessClip(RideTimelineBehaviour behaviour)
        {
            behaviour.ProcessBehaviour();
            StartCoroutine(behaviour.ProcessContinuousBehaviour());
        }

        /// <summary>Gets the RIDE identifier associated with a named Timeline entity.</summary>
        /// <param name="entityName">The exact key stored in <see cref="m_entityDictionary"/>.</param>
        /// <returns>The entity's RIDE identifier.</returns>
        public RideID GetEntityID(string entityName)
        {
            return m_entityDictionary[entityName].m_rideID;
        }

        /// <summary>Removes all Timeline entity-name mappings.</summary>
        public void ClearEntityDictionary()
        {
            m_entityDictionary.Clear();
        }

        /// <summary>Refreshes the cached list of active-scene <see cref="PlayableDirector"/> components.</summary>
        public void PopulateDirectorList()
        {
            m_directors = RideUtils.FindObjectsByType<PlayableDirector>().ToList();
        }

        /// <summary>Refreshes the virtual-human cache, including inactive characters.</summary>
        private void PopulateCharacterList()
        {
            m_cutsceneCharacters.Clear();
            //List<GameObject> rootObjects = SceneManager.GetActiveScene().GetRootGameObjects().ToList();
            //foreach (GameObject obj in rootObjects)
            //    SearchForComponentRecursive<MecanimCharacter>(obj.transform, m_cutsceneCharacters);
            m_cutsceneCharacters = RideUtils.FindObjectsByType<MecanimCharacter>(FindObjectsInactive.Include).ToList();
        }

        /// <summary>Adds components of a requested type found below a transform to a caller-provided list.</summary>
        /// <typeparam name="T">The component type to find.</typeparam>
        /// <param name="parent">The root transform to inspect.</param>
        /// <param name="componentList">The list that receives matching components.</param>
        private void SearchForComponentRecursive<T>(Transform parent, List<T> componentList)
        {
            if (parent.TryGetComponent<T>(out T component))
                componentList.Add(component);

            foreach (Transform child in parent)
                SearchForComponentRecursive<T>(child, componentList);
        }

        /// <summary>Plays the first scene director whose GameObject has the requested name.</summary>
        /// <param name="directorName">The director GameObject name.</param>
        public void PlayTimeline(string directorName)
        {
            PlayTimeline(null, directorName);
        }

        /// <summary>Plays a named director globally or beneath a specified parent.</summary>
        /// <param name="parent">The optional hierarchy root used to limit the search.</param>
        /// <param name="directorName">The director GameObject name.</param>
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

        /// <summary>Freezes a director at its current time and also freezes any playing parent director that controls it.</summary>
        /// <param name="_director">The director bound to the unresolved blocking clip.</param>
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

        /// <summary>Releases all Timeline pause records so directors can advance again.</summary>
        public void ResumeTimelines()
        {
            m_pausedDirectors.Clear();
        }

        /// <summary>Determines whether the first scene director with a matching name is playing.</summary>
        /// <remarks>Director names are not guaranteed to be unique; use the parent overload to constrain ambiguous searches.</remarks>
        /// <param name="directorName">The director GameObject name.</param>
        /// <returns><c>true</c> when the matching director is playing; otherwise <c>false</c>.</returns>
        public bool IsTimelinePlaying(string directorName)
        {
            return IsTimelinePlaying(null, directorName);
        }

        /// <summary>Determines whether a named director beneath a parent is playing.</summary>
        /// <param name="parent">The optional hierarchy root used to limit the search.</param>
        /// <param name="directorName">The director GameObject name.</param>
        /// <returns><c>true</c> when the matching director is playing; otherwise <c>false</c>.</returns>
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

        /// <summary>Determines whether any director in the active scene is playing.</summary>
        /// <returns><c>true</c> when at least one director is playing; otherwise <c>false</c>.</returns>
        public bool IsTimelinePlaying()
        {
            foreach (var director in GetAllDirectors())
            {
                if (director.state == PlayState.Playing)
                    return true;
            }
            return false;
        }

        /// <summary>Stops every director found in the active scene.</summary>
        public void StopAllTimelines()
        {
            foreach (var director in GetAllDirectors())
            {
                director.Stop();
            }
        }

        /// <summary>Finds all active-scene Timeline directors.</summary>
        /// <returns>A newly allocated list of the directors found in the scene.</returns>
        public List<PlayableDirector> GetAllDirectors()
        {
            return RideUtils.FindObjectsByType<PlayableDirector>().ToList();
        }

        /// <summary>Clears and repopulates the cached director list.</summary>
        public void ResetDirectors()
        {
            m_directors.Clear();
            PopulateDirectorList();
        }

        /// <summary>Finds a virtual-human character by exact GameObject name.</summary>
        /// <param name="name">The character GameObject name.</param>
        /// <returns>The matching character, or <c>null</c> when none is found.</returns>
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

        /// <summary>Finds a named Timeline director globally or beneath a specified parent.</summary>
        /// <param name="directorName">The director GameObject name.</param>
        /// <param name="parentObject">The optional hierarchy root used to limit the search.</param>
        /// <returns>The matching director, or <c>null</c> when none is found.</returns>
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

        /// <summary>Forwards a message to Timeline notification subscribers.</summary>
        /// <param name="message">The notification payload.</param>
        /// <remarks>At least one <see cref="OnTimelineNotification"/> subscriber must be registered before this method is called.</remarks>
        public void SendNotification(string message)
        {
            OnTimelineNotification.Invoke(message);
        }

        /// <summary>Retargets supported virtual-human clips in a director and its nested controlled directors.</summary>
        /// <param name="directorName">The root director GameObject name.</param>
        /// <param name="characterName">The character name written to supported virtual-human behaviors.</param>
        /// <param name="parentObject">The optional hierarchy root used to limit director lookup.</param>
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

    /// <summary>Stores the RIDE identity and grouping metadata associated with a named Timeline entity.</summary>
    public class EntityData
    {
        /// <summary>Initializes Timeline entity metadata.</summary>
        /// <param name="id">The RIDE entity identifier.</param>
        /// <param name="hierarchy">The entity's organizational role.</param>
        /// <param name="team">The entity's team.</param>
        /// <param name="name">The entity name used by Timeline content.</param>
        public EntityData(RideID id, EntityHierarchy hierarchy, Team team, string name)
        {
            m_rideID = id;
            m_hierarchy = hierarchy;
            m_team = team;
            m_name = name;
        }

        /// <summary>The entity's RIDE identifier.</summary>
        public RideID m_rideID;
        /// <summary>The entity's organizational role.</summary>
        public EntityHierarchy m_hierarchy;
        /// <summary>The entity's team.</summary>
        public Team m_team;
        /// <summary>The entity name used by Timeline content.</summary>
        public string m_name;
    }

    /// <summary>Identifies organizational roles represented by Timeline entities.</summary>
    public enum EntityHierarchy
    {
        /// <summary>An individual agent.</summary>
        Agent,
        /// <summary>A fireteam.</summary>
        Fireteam,
        /// <summary>A squad.</summary>
        Squad,
        /// <summary>A company.</summary>
        Company,
        /// <summary>A platoon.</summary>
        Platoon,
        /// <summary>A mounted platform.</summary>
        Mount,
    }

    /// <summary>Identifies group-level behaviors that can be represented by Timeline content.</summary>
    public enum GroupBehaviour
    {
        /// <summary>Move while maintaining the configured formation.</summary>
        MoveInFormation = 0,
        /// <summary>Begin an ambush.</summary>
        Ambush_Initiate = 1,
        /// <summary>React to an ambush.</summary>
        Ambush_React = 2,
    }

    /// <summary>Identifies mounted platform presets referenced by Timeline content.</summary>
    public enum MountTypes
    {
        /// <summary>An M4A1 Sherman tank.</summary>
        M4A1Sherman = 0,
        /// <summary>An M777 howitzer.</summary>
        M777 = 1,
        /// <summary>A High Mobility Multipurpose Wheeled Vehicle.</summary>
        Humvee = 2,
    }
}


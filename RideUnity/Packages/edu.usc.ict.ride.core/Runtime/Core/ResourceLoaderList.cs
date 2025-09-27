using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Registers predefined scene GameObjects and AudioClips with the <see cref="ResourceLoaderSystem"/>.
    /// 
    /// Attach this script to a GameObject in your scene and populate the serialized lists to ensure
    /// those assets are available for lookup and instantiation via IResourceLoaderSystem at runtime.
    /// 
    /// This is typically used to register non-addressable resources such as scene-placed prefabs or
    /// preloaded audio clips.
    /// </summary>
    public class ResourceLoaderList : RideMonoBehaviour
    {
        /// <summary>
        /// List of GameObjects in the scene that should be registered with the ResourceLoaderSystem.
        /// These can later be accessed or instantiated by name or item type.
        /// </summary>
        [SerializeField] List<GameObject> m_sceneResources;

        /// <summary>
        /// List of AudioClips to be registered with the ResourceLoaderSystem.
        /// These can later be retrieved by name.
        /// </summary>
        [SerializeField] List<AudioClip> m_sceneAudioClips;


        /// <summary>
        /// On Start, registers all scene resources and audio clips with the ResourceLoaderSystem.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            var resourceLoader = Systems.Get<ResourceLoaderSystem>();
            foreach (var obj in m_sceneResources)
                if (obj != null)
                    resourceLoader.AddSceneObject(obj);
            foreach (var clip in m_sceneAudioClips)
                if (clip != null)
                    resourceLoader.AddAudioClip(clip);
        }
    }
}

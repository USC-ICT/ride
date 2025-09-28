using UnityEngine;
using Ride.UI;
using Ride.IO;

namespace Ride.Terrain
{
    /// <summary>
    /// Handles input and UI for direct destruction.
    /// "Direct destruction" refers to destruction that the user's inputs directly create the destruction, as opposed a unit causing it.
    /// </summary>
    public class DirectDestructionMenu : MenuMono
    {
        [SerializeField]
        GameObject explosionParticleSystem = default;
        [SerializeField]
        AudioSource[] explosionAudioSources = default;
        [SerializeField]
        AudioClip explosionSound = default;

        public float radiusStartValue = 1;
        public float powerStartValue = 300;

        float radius;
        float power;

        [Header("UI Settings")]
        [SerializeField]
        RideVector2 powerRange = default;
        [SerializeField]
        RideText powerText = default;
        [SerializeField]
        RideSlider powerSlider = default;
        [SerializeField]
        RideVector2 rangeRange = default;
        [SerializeField]
        RideText rangeText = default;
        [SerializeField]
        RideSlider rangeSlider = default;

        public bool mouseDestructionEnabled = true;
        public int mouseDestructionButton = 0;
        public RideKeyCode keyboardDestructionButton = default;

        protected override void Start()
        {
            base.Start();

            powerSlider.minValue = powerRange.x;
            powerSlider.maxValue = powerRange.y;
            powerSlider.value = powerStartValue;
            SetBlastPower(powerSlider.value);
            rangeSlider.minValue = rangeRange.x;
            rangeSlider.maxValue = rangeRange.y;
            rangeSlider.value = radiusStartValue;
            SetBlastRadius(rangeSlider.value);
            powerSlider.AddOnValueChanged(SetBlastPower);
            rangeSlider.AddOnValueChanged(SetBlastRadius);
        }

        public void SetBlastRadius(float value)
        {
            radius = value;
            rangeText.text = value.ToString("f2");
        }

        public void SetBlastPower(float value)
        {
            power = value;
            powerText.text = value.ToString("f0");
        }

        protected override void Update()
        {
            base.Update();

            if (RideUtils.IsMouseOverUI())
                return;
            if ((Globals.api.inputSystem.GetMouseButtonDown(mouseDestructionButton, RideInputLayer.Player) && mouseDestructionEnabled) || Globals.api.inputSystem.GetKeyDown(keyboardDestructionButton, RideInputLayer.Player))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Destruct(hit.point, ray.direction, radius, power);
                }
            }
        }

        /// <summary>
        /// Displays the destruction effect and destructs and the terrain
        /// </summary>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="radius"></param>
        /// <param name="power"></param>
        public void Destruct(RideVector3 point, RideVector3 direction, float radius, float power)
        {
            DisplayDestructEffect(point, direction, radius, power);
            Globals.api.terrainSystem.DestructTerrain(point, radius, power);
        }

        /// <summary>
        /// This will only display the terrain destruction effect, not destruct the terrain mesh
        /// </summary>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="radius"></param>
        /// <param name="power"></param>
        public void DisplayDestructEffect(RideVector3 point, RideVector3 direction, float radius, float power)
        {
            if (explosionParticleSystem != null)
            {
                var particle = Instantiate(explosionParticleSystem);
                particle.transform.position = point;
                particle.transform.localScale *= radius;
            }
            point += direction * .2F;

            if (explosionAudioSources.Length > 0)
            {
                foreach (AudioSource boomSource in explosionAudioSources)
                {
                    if (boomSource != null)
                    {
                        if (!boomSource.isPlaying)
                        {
                            boomSource.transform.position = point;
                            if (boomSource.outputAudioMixerGroup != null && boomSource.outputAudioMixerGroup.audioMixer != null)
                            {
                                boomSource.outputAudioMixerGroup.audioMixer.SetFloat("Pitch", 1f * Random.Range(0.9f, 1.1f));
                            }
                            boomSource.clip = explosionSound;
                            AudioSource.PlayClipAtPoint(explosionSound, point);
                            //boomSource.PlayOneShot(boomSource.clip);
                            break;
                        }
                    }
                    else
                    {
                        AudioSource.PlayClipAtPoint(explosionSound, point);
                    }
                }
            }
            else
            {
                AudioSource.PlayClipAtPoint(explosionSound, point);
            }
        }
    }
}

using UnityEngine;
using Ride.UI;
using Ride.IO;

namespace Ride.Terrain
{
    /// <summary>
    /// Handles input and UI for direct destruction.
    /// "Direct destruction" refers to destruction that the user's inputs directly create the destruction, as opposed a unit causing it.
    /// </summary>
    public class DirectDestructionMenu : MenuUnity
    {
        [Tooltip("Particle-system prefab instantiated at the impact point to visualize the destruction effect.")]
        [SerializeField]
        GameObject explosionParticleSystem = default;
        [Tooltip("Audio sources available for playing destruction sounds at the impact location.")]
        [SerializeField]
        AudioSource[] explosionAudioSources = default;
        [Tooltip("Audio clip played when a destruction action is triggered.")]
        [SerializeField]
        AudioClip explosionSound = default;

        [Tooltip("Initial blast radius value assigned to the radius slider when the menu starts.")]
        public float radiusStartValue = 1;
        [Tooltip("Initial blast power value assigned to the power slider when the menu starts.")]
        public float powerStartValue = 300;

        float radius;
        float power;

        [Header("UI Settings")]
        [Tooltip("Minimum and maximum values allowed for the blast power slider.")]
        [SerializeField]
        RideVector2 powerRange = default;
        [Tooltip("UI text element that displays the current blast power value.")]
        [SerializeField]
        RideText powerText = default;
        [Tooltip("Slider used to control the blast power.")]
        [SerializeField]
        RideSlider powerSlider = default;
        [Tooltip("Minimum and maximum values allowed for the blast radius slider.")]
        [SerializeField]
        RideVector2 rangeRange = default;
        [Tooltip("UI text element that displays the current blast radius value.")]
        [SerializeField]
        RideText rangeText = default;
        [Tooltip("Slider used to control the blast radius.")]
        [SerializeField]
        RideSlider rangeSlider = default;

        [Tooltip("Enables mouse-click terrain destruction when true.")]
        public bool mouseDestructionEnabled = true;
        [Tooltip("Mouse button index used to trigger direct terrain destruction.")]
        public int mouseDestructionButton = 0;
        [Tooltip("Keyboard key that can also trigger direct terrain destruction.")]
        public RideKeyCode keyboardDestructionButton = default;

        /// <summary>
        /// Initializes the destruction UI controls and synchronizes the displayed power and radius values.
        /// </summary>
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

        /// <summary>
        /// Updates the current blast radius and refreshes the radius text display.
        /// </summary>
        /// <param name="value">The new blast radius.</param>
        public void SetBlastRadius(float value)
        {
            radius = value;
            rangeText.text = value.ToString("f2");
        }

        /// <summary>
        /// Updates the current blast power and refreshes the power text display.
        /// </summary>
        /// <param name="value">The new blast power.</param>
        public void SetBlastPower(float value)
        {
            power = value;
            powerText.text = value.ToString("f0");
        }

        /// <summary>
        /// Monitors the configured input bindings and triggers terrain destruction where the user clicks.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (RideUtils.IsMouseOverUI())
                return;

            if ((Systems.Input.GetMouseButtonDown(mouseDestructionButton, RideInputLayer.Player) && mouseDestructionEnabled) || 
                 Systems.Input.GetKeyDown(keyboardDestructionButton, RideInputLayer.Player))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                    Destruct(hit.point, ray.direction, radius, power);
            }
        }

        /// <summary>
        /// Displays the destruction effect and applies terrain destruction at the specified location.
        /// </summary>
        /// <param name="point">The world-space impact point.</param>
        /// <param name="direction">The incoming direction of the destruction event.</param>
        /// <param name="radius">The radius of the blast effect.</param>
        /// <param name="power">The strength of the blast effect.</param>
        public void Destruct(RideVector3 point, RideVector3 direction, float radius, float power)
        {
            DisplayDestructEffect(point, direction, radius, power);
            Systems.Terrain.DestructTerrain(point, radius, power);
        }

        /// <summary>
        /// Displays the visual and audio destruction effects without modifying the terrain mesh.
        /// </summary>
        /// <param name="point">The world-space impact point.</param>
        /// <param name="direction">The incoming direction of the destruction event.</param>
        /// <param name="radius">The radius used to scale the visual effect.</param>
        /// <param name="power">The blast strength value associated with the effect.</param>
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
                                boomSource.outputAudioMixerGroup.audioMixer.SetFloat("Pitch", 1f * Random.Range(0.9f, 1.1f));

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

using UnityEngine;
using UnityEngine.UI;

namespace Ride.UI
{
    /// <summary>
    /// A speaker icon that shows whether application audio is on and lets the user change it.
    /// </summary>
    /// <remarks>
    /// Reflects and drives <see cref="RideAppAudio.Muted"/>, which is the application's own
    /// mute state rather than any device setting. On platforms where a hardware silent switch
    /// would otherwise silence the application, this control is how the user turns sound on.
    ///
    /// Assign either a pair of sprites for a single <see cref="Image"/> or a pair of
    /// GameObjects to show and hide; whichever is assigned is used.
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class RideAudioMuteToggle : MonoBehaviour
    {
        [Header("Option A - swap the sprite on one Image")]
        [SerializeField] private Image m_icon;
        [SerializeField] private Sprite m_soundOnSprite;
        [SerializeField] private Sprite m_soundOffSprite;

        [Header("Option B - show one of two GameObjects")]
        [SerializeField] private GameObject m_soundOnObject;
        [SerializeField] private GameObject m_soundOffObject;

        [Header("Behaviour")]
        [Tooltip("Hide this control on platforms where the hardware volume controls are sufficient.")]
        [SerializeField] private bool m_onlyShowOnMobile = false;

        private Button m_button;
        private RideAppAudio m_appAudio;

        private void Awake()
        {
            m_button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (m_onlyShowOnMobile && !RideUtils.IsIOS() && !RideUtils.IsAndroid())
            {
                gameObject.SetActive(false);
                return;
            }

            if (m_appAudio != null) Bind();
        }

        private void Start()
        {
            m_appAudio = Systems.Get<RideAppAudio>();
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind()
        {
            if (m_appAudio == null) return;

            m_button.onClick.RemoveListener(m_appAudio.ToggleMuted);
            m_button.onClick.AddListener(m_appAudio.ToggleMuted);
            m_appAudio.OnMutedChanged -= Refresh;
            m_appAudio.OnMutedChanged += Refresh;
            Refresh(m_appAudio.Muted);
        }

        private void Unbind()
        {
            if (m_appAudio == null) return;

            if (m_button != null) m_button.onClick.RemoveListener(m_appAudio.ToggleMuted);
            m_appAudio.OnMutedChanged -= Refresh;
        }

        private void Refresh(bool muted)
        {
            if (m_icon != null)
            {
                var sprite = muted ? m_soundOffSprite : m_soundOnSprite;
                if (sprite != null) m_icon.sprite = sprite;
            }

            if (m_soundOnObject != null) m_soundOnObject.SetActive(!muted);
            if (m_soundOffObject != null) m_soundOffObject.SetActive(muted);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    /// <summary>
    /// Provides a simple world-space text billboard backed by a <see cref="TextMesh"/>.
    /// </summary>
    public class WorldLabelBillboard : MonoBehaviour, IWorldLabelBillboard
    {
        [Tooltip("TextMesh used to render the billboard's world-space label text.")]
        [SerializeField] TextMesh m_TextMesh = null;

        /// <summary>Gets or sets the text displayed by the billboard.</summary>
        public string Text { get => m_TextMesh.text; set => m_TextMesh.text = value; }

        /// <summary>Gets or sets the world-space position of the billboard.</summary>
        public RideVector3 Position { get => gameObject.transform.position; set => gameObject.transform.position = value; }

        /// <summary>
        /// Activates or deactivates the billboard GameObject.
        /// </summary>
        /// <param name="value">True to activate the billboard; otherwise, false.</param>
        public void SetActive(bool value) => gameObject.SetActive(value);
    }
}

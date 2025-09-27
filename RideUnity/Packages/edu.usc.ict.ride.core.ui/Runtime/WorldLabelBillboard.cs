using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.UI
{
    public class WorldLabelBillboard : MonoBehaviour, IWorldLabelBillboard
    {
        [SerializeField] TextMesh m_TextMesh = null;

        public string Text { get { return m_TextMesh.text; } set { m_TextMesh.text = value; } }
        public RideVector3 Position { get { return gameObject.transform.position; } set { gameObject.transform.position = value; } }
        public void SetActive(bool value) { gameObject.SetActive(value); }
    }
}

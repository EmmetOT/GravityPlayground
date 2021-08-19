using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GravityPlayground.GravityStuff
{
    public class GravityParticleDebuggerUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI m_text;

        [SerializeField]
        private Transform m_playerTransform;

        [SerializeField]
        private Color m_positiveColour;

        [SerializeField]
        private Color m_negativeColour;

        private void FixedUpdate()
        {
            Color polarityCol = GetPolarityColour(out float polarity);

            m_text.text = $"Particle Count = {Gravity.ParticleSystem.ActiveParticleCount}".AddColour(Color.green) + $"\nBody Count = {Gravity.BodiesCount}".AddColour(Color.cyan) + $"\nPolarity at Player = {polarity:F2}".AddColour(polarityCol);
        }

        private Color GetPolarityColour(out float polarity)
        {
            polarity = Gravity.GetGravityPolarity(m_playerTransform.position);
            return Color.Lerp(m_negativeColour, m_positiveColour, Mathf.InverseLerp(-1f, 1f, polarity));
        }
    }
}
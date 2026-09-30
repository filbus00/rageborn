using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ARPG
{
    /// <summary>Makes a prop's light flicker like a flame (<see cref="LightingRules.Flicker"/>).</summary>
    public class FlickerLight : MonoBehaviour
    {
        Light2D target;
        float baseIntensity;
        float phase;

        public void Init(Light2D light, float phaseOffset)
        {
            target = light;
            baseIntensity = light.intensity;
            phase = phaseOffset;
        }

        void Update()
        {
            if (target != null)
                target.intensity = baseIntensity * LightingRules.Flicker(Time.time, phase);
        }
    }
}

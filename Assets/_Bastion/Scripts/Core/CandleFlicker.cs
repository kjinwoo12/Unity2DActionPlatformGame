using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Bastion.Core
{
    /// <summary>
    /// 촛불 흔들림: 펄린 노이즈로 불꽃 크기와 빛의 세기를 불규칙하게 바꾼다.
    /// </summary>
    public sealed class CandleFlicker : MonoBehaviour
    {
        [SerializeField] Transform flame;
        [SerializeField] Light2D glow;
        [SerializeField, Min(0f)] float speed = 6f;
        [SerializeField, Range(0f, 0.5f)] float scaleAmount = 0.15f;
        [SerializeField, Range(0f, 1f)] float intensityAmount = 0.35f;

        Vector3 baseScale;
        float baseIntensity;
        float seed;

        void Awake()
        {
            if (flame != null) baseScale = flame.localScale;
            if (glow != null) baseIntensity = glow.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * speed;
            // -1..1 범위의 노이즈 두 개로 세로 길이와 가로 폭을 따로 흔든다.
            float n1 = Mathf.PerlinNoise(seed, t) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(seed + 37f, t * 1.3f) * 2f - 1f;

            if (flame != null)
                flame.localScale = new Vector3(baseScale.x * (1f + n2 * scaleAmount * 0.5f), baseScale.y * (1f + n1 * scaleAmount), baseScale.z);
            if (glow != null)
                glow.intensity = baseIntensity * (1f + n1 * intensityAmount);
        }
    }
}

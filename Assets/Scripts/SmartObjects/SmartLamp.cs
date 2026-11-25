using System.Collections;
using UnityEngine;

// A lamp that fades its light (and optionally a bulb's emissive material) on and off.
[RequireComponent(typeof(Light))]
public class SmartLamp : SmartObject
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Renderer bulbRenderer; // optional: a bulb mesh with an emissive material
    [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(1f, 0.85f, 0.6f);

    private Light lampLight;
    private Material bulbMaterial; // single cached instance
    private Coroutine fadeRoutine;

    private void Awake()
    {
        lampLight = GetComponent<Light>();
        lampLight.intensity = 0f;

        if (bulbRenderer != null)
        {
            // Accessing .material clones the material; do it once and destroy it ourselves
            // (calling it every frame leaks a new material each time).
            bulbMaterial = bulbRenderer.material;
            bulbMaterial.EnableKeyword("_EMISSION");
        }
        SetEmission(0f);
    }

    private void OnDestroy()
    {
        if (bulbMaterial != null) Destroy(bulbMaterial);
    }

    protected override void TurnOn()
    {
        base.TurnOn();
        StartFade(maxIntensity);
    }

    protected override void TurnOff()
    {
        base.TurnOff();
        StartFade(0f);
    }

    private void StartFade(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        // Coroutines can't run on inactive objects (and would throw); snap instead.
        if (!isActiveAndEnabled || fadeDuration <= 0f)
        {
            Apply(target);
            return;
        }
        fadeRoutine = StartCoroutine(FadeTo(target));
    }

    private IEnumerator FadeTo(float target)
    {
        float start = lampLight.intensity;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            Apply(Mathf.Lerp(start, target, t / fadeDuration));
            yield return null;
        }
        Apply(target);
        fadeRoutine = null;
    }

    private void Apply(float intensity)
    {
        lampLight.intensity = intensity;
        SetEmission(maxIntensity > 0f ? intensity / maxIntensity : 0f);
    }

    private void SetEmission(float strength)
    {
        if (bulbMaterial == null) return;
        bulbMaterial.SetColor(EmissionColorId, glowColor * strength);
    }
}

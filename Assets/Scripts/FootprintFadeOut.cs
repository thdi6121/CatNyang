using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FootprintFadeOut : MonoBehaviour
{
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float fadeDuration = 1.5f;

    private readonly List<ColorProperty> colorProperties = new();

    private struct ColorProperty
    {
        public Material Material;
        public int PropertyId;
        public Color OriginalColor;
        public bool FadeRgb;
    }

private void Awake()
{
    foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
    {
        foreach (Material material in renderer.materials)
        {
            if (material.HasProperty("_BaseColor"))
                AddColorProperty(material, "_BaseColor", false);
            else if (material.HasProperty("_Color"))
                AddColorProperty(material, "_Color", false);

            if (material.HasProperty("_EmissionColor"))
                AddColorProperty(material, "_EmissionColor", true);
        }
    }
}

private void AddColorProperty(Material material, string propertyName, bool fadeRgb)
{
    int propertyId = Shader.PropertyToID(propertyName);

    colorProperties.Add(new ColorProperty
    {
        Material = material,
        PropertyId = propertyId,
        OriginalColor = material.GetColor(propertyId),
        FadeRgb = fadeRgb
    });
}

    private IEnumerator Start()
    {
        if (fadeDuration <= 0f)
        {
            Destroy(gameObject, lifetime);
            yield break;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, lifetime - fadeDuration));

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            SetAlpha(elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(1f);
        Destroy(gameObject);
    }

private void SetAlpha(float fadeProgress)
{
    float visibility = 1f - fadeProgress;

    foreach (ColorProperty colorProperty in colorProperties)
    {
        Color color = colorProperty.OriginalColor;

        if (colorProperty.FadeRgb)
        {
            color.r *= visibility;
            color.g *= visibility;
            color.b *= visibility;
        }

        color.a *= visibility;
        colorProperty.Material.SetColor(colorProperty.PropertyId, color);
    }
}
}
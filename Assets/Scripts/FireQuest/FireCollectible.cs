using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class FireCollectible : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed = 2.2f;

    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField] private float effectLifetime = 5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Vector3 basePosition;
    public bool Collected { get; private set; }

    private void Awake()
    {
        basePosition = transform.position;
        var trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.2f;
    }

    private void Start()
    {
        FireQuestManager.Instance?.Register(this);
    }

    private void Update()
    {
        if (Collected)
            return;

        float wave = Mathf.Sin(Time.time * bobSpeed + transform.position.x) * bobHeight;
        transform.position = basePosition + Vector3.up * wave;
        transform.Rotate(Vector3.up, 65f * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Collected &&
            (other.GetComponentInParent<KeyboardCatMove>() != null || other.name == "고양이"))
        {
            FireQuestManager.Instance?.Collect(this);
        }
    }

    public void MarkCollected()
    {
        if (Collected)
            return;

        Collected = true;

        if (collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                collectEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(effect, effectLifetime);
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Material[] materials = new Material[0];

        foreach (Renderer renderer in renderers)
        {
            Material[] rendererMaterials = renderer.materials;
            int previousLength = materials.Length;
            System.Array.Resize(ref materials, previousLength + rendererMaterials.Length);
            rendererMaterials.CopyTo(materials, previousLength);
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = 1f - elapsed / fadeDuration;

            foreach (Material material in materials)
            {
                SetAlpha(material, "_Color", alpha);
                SetAlpha(material, "_OutlineColor", alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (Material material in materials)
        {
            SetAlpha(material, "_Color", 0f);
            SetAlpha(material, "_OutlineColor", 0f);
        }

        Destroy(gameObject);
    }

    private static void SetAlpha(Material material, string propertyName, float alpha)
    {
        if (!material.HasProperty(propertyName))
            return;

        Color color = material.GetColor(propertyName);
        color.a = alpha;
        material.SetColor(propertyName, color);
    }
}
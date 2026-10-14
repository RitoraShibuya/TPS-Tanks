using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusionFade : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private Camera targetCamera;

    [Header("検出設定")]
    [SerializeField]
    private LayerMask obstacleLayers = -1;

    [SerializeField]
    private float rayOffset = 0.2f;

    [Header("透明度")]
    [SerializeField, Range(0.05f, 1f)]
    private float transparentAlpha = 0.25f;

    private readonly Dictionary<Renderer, Material[]> originalMaterials =
        new Dictionary<Renderer, Material[]>();

    private readonly Dictionary<Renderer, Material[]> fadedMaterials =
        new Dictionary<Renderer, Material[]>();

    private readonly HashSet<Renderer> currentRenderers =
        new HashSet<Renderer>();

    private readonly HashSet<Renderer> previousRenderers =
        new HashSet<Renderer>();

    private void LateUpdate()
    {
        if (target == null || targetCamera == null)
        {
            return;
        }

        currentRenderers.Clear();

        Vector3 cameraPosition = targetCamera.transform.position;
        Vector3 targetPosition =
            target.position + Vector3.up * rayOffset;

        Vector3 direction = targetPosition - cameraPosition;
        float distance = direction.magnitude;

        if (distance <= 0f)
        {
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            cameraPosition,
            direction.normalized,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == target ||
                hit.transform.IsChildOf(target))
            {
                continue;
            }

            Renderer renderer = hit.collider.GetComponent<Renderer>();

            if (renderer == null)
            {
                renderer =
                    hit.collider.GetComponentInParent<Renderer>();
            }

            if (renderer == null ||
                renderer.transform == target ||
                renderer.transform.IsChildOf(target))
            {
                continue;
            }

            currentRenderers.Add(renderer);

            if (!fadedMaterials.ContainsKey(renderer))
            {
                CreateTransparentMaterials(renderer);
            }

            if (fadedMaterials.TryGetValue(
                renderer, out Material[] materials))
            {
                renderer.sharedMaterials = materials;
            }
        }

        // 遮蔽物でなくなったものを元に戻す
        foreach (Renderer renderer in previousRenderers)
        {
            if (renderer == null ||
                currentRenderers.Contains(renderer))
            {
                continue;
            }

            RestoreMaterials(renderer);
        }

        previousRenderers.Clear();

        foreach (Renderer renderer in currentRenderers)
        {
            previousRenderers.Add(renderer);
        }
    }

    private void CreateTransparentMaterials(Renderer renderer)
    {
        Material[] originals = renderer.sharedMaterials;
        Material[] transparent = new Material[originals.Length];

        for (int i = 0; i < originals.Length; i++)
        {
            if (originals[i] == null)
            {
                continue;
            }

            Material material = new Material(originals[i]);

            // URP Lit / 一般的なURP透明対応シェーダー
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", 5f);
                material.SetFloat("_DstBlend", 10f);
                material.SetFloat("_ZWrite", 0f);

                material.SetOverrideTag(
                    "RenderType", "Transparent");

                material.SetInt(
                    "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

                material.SetInt(
                    "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                material.renderQueue =
                    (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // Standard Shader 用
            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", 5);
                material.SetInt("_DstBlend", 10);
                material.SetInt("_ZWrite", 0);

                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                material.renderQueue = 3000;
            }

            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = transparentAlpha;
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                Color color = material.color;
                color.a = transparentAlpha;
                material.color = color;
            }

            transparent[i] = material;
        }

        originalMaterials[renderer] = originals;
        fadedMaterials[renderer] = transparent;
    }

    private void RestoreMaterials(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        if (originalMaterials.TryGetValue(
            renderer, out Material[] originals))
        {
            renderer.sharedMaterials = originals;
        }
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Renderer, Material[]> pair in fadedMaterials)
        {
            foreach (Material material in pair.Value)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        foreach (KeyValuePair<Renderer, Material[]> pair in originalMaterials)
        {
            if (pair.Key != null)
            {
                pair.Key.sharedMaterials = pair.Value;
            }
        }
    }
}

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageFlash : MonoBehaviour
{
    [Header("References")]
    [Tooltip("피격 점멸을 적용할 SpriteRenderer입니다.")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Flash Settings")]
    [Tooltip("피격 시 적용할 색상입니다.")]
    [SerializeField]
    private Color flashColor = Color.red;

    [Tooltip("한 번 빨갛게 유지되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float flashDuration = 0.08f;

    [Tooltip("빨간색 점멸 횟수입니다.")]
    [SerializeField, Min(1)]
    private int flashCount = 2;

    [Tooltip("점멸 사이에 원래 색상으로 돌아가는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float intervalDuration = 0.05f;

    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;

    private void Reset()
    {
        spriteRenderer =
            GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }
    }

    private void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(
                flashCoroutine
            );

            flashCoroutine = null;
        }

        RestoreColor();
    }

    public void Play()
    {
        if (spriteRenderer == null)
            return;

        if (flashCoroutine != null)
        {
            StopCoroutine(
                flashCoroutine
            );
        }

        flashCoroutine =
            StartCoroutine(
                FlashRoutine()
            );
    }

    private IEnumerator FlashRoutine()
    {
        int count =
            Mathf.Max(
                1,
                flashCount
            );

        for (int i = 0; i < count; i++)
        {
            spriteRenderer.color =
                flashColor;

            if (flashDuration > 0f)
            {
                yield return new WaitForSeconds(
                    flashDuration
                );
            }

            spriteRenderer.color =
                originalColor;

            if (i < count - 1 &&
                intervalDuration > 0f)
            {
                yield return new WaitForSeconds(
                    intervalDuration
                );
            }
        }

        RestoreColor();

        flashCoroutine = null;
    }

    private void RestoreColor()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color =
            originalColor;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        flashDuration =
            Mathf.Max(
                0f,
                flashDuration
            );

        flashCount =
            Mathf.Max(
                1,
                flashCount
            );

        intervalDuration =
            Mathf.Max(
                0f,
                intervalDuration
            );
    }
#endif
}
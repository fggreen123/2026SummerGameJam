using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
[DisallowMultipleComponent]
public sealed class DamagePopup : MonoBehaviour
{
    private static DamagePopup prefab;

    [Header("Text")]
    [SerializeField]
    private TextMeshPro damageText;

    [Header("Animation")]
    [Tooltip("숫자가 위로 올라가는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float moveDistance = 0.7f;

    [Tooltip("좌우로 흩어지는 최대 거리입니다.")]
    [SerializeField, Min(0f)]
    private float horizontalRandomDistance = 0.15f;

    [Tooltip("숫자가 표시되는 시간입니다.")]
    [SerializeField, Min(0.01f)]
    private float duration = 0.65f;

    [Tooltip("숫자가 처음 튀어나올 때의 크기입니다.")]
    [SerializeField, Min(0.01f)]
    private float startScaleMultiplier = 1.35f;

    [Tooltip("적 위치를 기준으로 한 생성 위치입니다.")]
    [SerializeField]
    private Vector3 spawnOffset = new Vector3(0f, 0.6f, 0f);

    private Vector3 originalScale;

    private void Reset()
    {
        damageText = GetComponent<TextMeshPro>();
    }

    private void Awake()
    {
        if (damageText == null)
            damageText = GetComponent<TextMeshPro>();

        originalScale = transform.localScale;

        /*
         * 씬에 직접 배치된 오브젝트를 원본 프리팹처럼 사용합니다.
         * 처음 등록된 오브젝트는 화면에서 숨겨 둡니다.
         */
        if (prefab == null)
        {
            prefab = this;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 지정한 월드 위치에 데미지 숫자를 생성합니다.
    /// </summary>
    public static void Show(int damage, Vector3 worldPosition)
    {
        if (damage <= 0)
            return;

        if (prefab == null)
        {
            Debug.LogWarning(
                "씬에 DamagePopup 오브젝트가 없습니다."
            );

            return;
        }

        DamagePopup popup = Instantiate(
            prefab,
            worldPosition + prefab.spawnOffset,
            Quaternion.identity
        );

        popup.gameObject.SetActive(true);
        popup.Initialize(damage);
    }

    private void Initialize(int damage)
    {
        if (damageText == null)
            damageText = GetComponent<TextMeshPro>();

        damageText.text = damage.ToString();

        Color color = damageText.color;
        color.a = 1f;
        damageText.color = color;

        originalScale = transform.localScale;

        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        Vector3 startPosition = transform.position;

        float randomX = Random.Range(
            -horizontalRandomDistance,
            horizontalRandomDistance
        );

        Vector3 targetPosition =
            startPosition +
            new Vector3(randomX, moveDistance, 0f);

        Vector3 normalScale = originalScale;

        transform.localScale =
            normalScale * startScaleMultiplier;

        Color startColor = damageText.color;
        Color endColor = startColor;
        endColor.a = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float time = Mathf.Clamp01(
                elapsedTime / duration
            );

            float moveTime =
                1f - Mathf.Pow(1f - time, 3f);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                moveTime
            );

            if (time < 0.2f)
            {
                float scaleTime = time / 0.2f;

                transform.localScale = Vector3.Lerp(
                    normalScale * startScaleMultiplier,
                    normalScale,
                    scaleTime
                );
            }
            else
            {
                float scaleTime = Mathf.InverseLerp(
                    0.2f,
                    1f,
                    time
                );

                transform.localScale = Vector3.Lerp(
                    normalScale,
                    normalScale * 0.8f,
                    scaleTime
                );
            }

            float fadeTime = Mathf.InverseLerp(
                0.55f,
                1f,
                time
            );

            damageText.color = Color.Lerp(
                startColor,
                endColor,
                fadeTime
            );

            yield return null;
        }

        Destroy(gameObject);
    }
}
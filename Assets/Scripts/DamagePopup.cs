using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public sealed class DamagePopup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("비워 두면 같은 오브젝트의 Enemy를 자동으로 찾습니다.")]
    [SerializeField]
    private Enemy enemy;

    [Tooltip("위치 계산에 사용할 적의 SpriteRenderer입니다. 비워 두면 자동으로 찾습니다.")]
    [SerializeField]
    private SpriteRenderer targetRenderer;

    [Header("Font")]
    [Tooltip("데미지 숫자에 사용할 TextMeshPro 도트 폰트 에셋입니다.")]
    [SerializeField]
    private TMP_FontAsset fontAsset;

    [Tooltip("데미지 숫자의 글자 크기입니다.")]
    [SerializeField, Min(0.01f)]
    private float fontSize = 5f;

    [SerializeField]
    private Color textColor = Color.white;

    [SerializeField]
    private Color outlineColor = Color.black;

    [SerializeField, Range(0f, 1f)]
    private float outlineWidth = 0.2f;

    [Header("Sorting")]
    [Tooltip("비워 두면 Default Sorting Layer를 사용합니다.")]
    [SerializeField]
    private string sortingLayerName = "Default";

    [SerializeField]
    private int sortingOrder = 1000;

    [Header("Spawn")]
    [Tooltip("적 스프라이트 중심을 기준으로 한 생성 위치입니다.")]
    [SerializeField]
    private Vector3 spawnOffset =
        new Vector3(0f, 0.6f, 0f);

    [Tooltip("좌우로 무작위로 흩어지는 최대 거리입니다.")]
    [SerializeField, Min(0f)]
    private float randomHorizontalDistance = 0.15f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)]
    private float duration = 0.65f;

    [SerializeField, Min(0f)]
    private float moveDistance = 0.7f;

    [SerializeField, Min(0.01f)]
    private float startScaleMultiplier = 1.35f;

    [SerializeField, Min(0.01f)]
    private float endScaleMultiplier = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float fadeStartTime = 0.55f;

    [Header("Pool")]
    [Tooltip("게임 시작 시 미리 생성할 숫자 개수입니다.")]
    [SerializeField, Min(1)]
    private int initialPoolSize = 15;

    private int previousHp;
    private bool subscribed;

    private static readonly Queue<TextMeshPro> popupPool =
        new Queue<TextMeshPro>();

    private static DamagePopupRunner runner;
    private static bool poolInitialized;

    private void Reset()
    {
        enemy = GetComponent<Enemy>();
        targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        if (targetRenderer == null)
        {
            targetRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        CreateRunner();
        InitializePool();
    }

    private void Start()
    {
        if (enemy == null)
        {
            Debug.LogError(
                $"{name}: Enemy 컴포넌트를 찾을 수 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        previousHp = enemy.CurrentHp;

        Subscribe();
    }

    private void OnEnable()
    {
        if (enemy != null &&
            enemy.CurrentHp > 0)
        {
            previousHp = enemy.CurrentHp;
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed ||
            enemy == null)
        {
            return;
        }

        enemy.HpChanged += OnHpChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed ||
            enemy == null)
        {
            return;
        }

        enemy.HpChanged -= OnHpChanged;
        subscribed = false;
    }

    private void OnHpChanged(
        int currentHp,
        int maxHp
    )
    {
        int damage =
            previousHp - currentHp;

        previousHp = currentHp;

        if (damage <= 0)
            return;

        ShowPopup(damage);
    }

    private void ShowPopup(int damage)
    {
        CreateRunner();
        InitializePool();

        TextMeshPro popup =
            GetPopupFromPool();

        if (popup == null)
            return;

        Vector3 basePosition =
            targetRenderer != null
                ? targetRenderer.bounds.center
                : transform.position;

        popup.transform.SetParent(null);
        popup.transform.position =
            basePosition + spawnOffset;

        popup.transform.rotation =
            Quaternion.identity;

        ApplyTextSettings(popup);

        popup.gameObject.SetActive(true);

        runner.Play(
            popup,
            damage,
            duration,
            moveDistance,
            randomHorizontalDistance,
            startScaleMultiplier,
            endScaleMultiplier,
            fadeStartTime
        );
    }

    private void InitializePool()
    {
        if (poolInitialized)
            return;

        poolInitialized = true;

        int count =
            Mathf.Max(
                1,
                initialPoolSize
            );

        for (int i = 0; i < count; i++)
        {
            TextMeshPro popup =
                CreatePopupObject();

            popupPool.Enqueue(popup);
        }
    }

    private TextMeshPro GetPopupFromPool()
    {
        while (popupPool.Count > 0)
        {
            TextMeshPro popup =
                popupPool.Dequeue();

            if (popup != null)
                return popup;
        }

        return CreatePopupObject();
    }

    private TextMeshPro CreatePopupObject()
    {
        GameObject popupObject =
            new GameObject(
                "Damage Popup Text"
            );

        popupObject.transform.SetParent(
            runner.transform
        );

        TextMeshPro popup =
            popupObject.AddComponent<TextMeshPro>();

        popup.alignment =
            TextAlignmentOptions.Center;

        popup.enableWordWrapping =
            false;

        popup.raycastTarget =
            false;

        ApplyTextSettings(popup);

        popup.text = string.Empty;
        popupObject.SetActive(false);

        return popup;
    }

    private void ApplyTextSettings(
        TextMeshPro popup
    )
    {
        if (popup == null)
            return;

        if (fontAsset != null)
        {
            popup.font =
                fontAsset;
        }

        popup.fontSize =
            fontSize;

        popup.color =
            textColor;

        popup.outlineColor =
            outlineColor;

        popup.outlineWidth =
            outlineWidth;

        popup.sortingLayerID =
            SortingLayer.NameToID(
                sortingLayerName
            );

        popup.sortingOrder =
            sortingOrder;

        popup.alignment =
            TextAlignmentOptions.Center;

        popup.enableWordWrapping =
            false;
    }

    private static void CreateRunner()
    {
        if (runner != null)
            return;

        GameObject runnerObject =
            new GameObject(
                "DamagePopupPool"
            );

        DontDestroyOnLoad(
            runnerObject
        );

        runner =
            runnerObject.AddComponent<DamagePopupRunner>();
    }

    public static void ReturnToPool(
        TextMeshPro popup
    )
    {
        if (popup == null)
            return;

        popup.text =
            string.Empty;

        popup.transform.localScale =
            Vector3.one;

        if (runner != null)
        {
            popup.transform.SetParent(
                runner.transform
            );
        }

        popup.gameObject.SetActive(false);

        popupPool.Enqueue(
            popup
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fontSize =
            Mathf.Max(
                0.01f,
                fontSize
            );

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        moveDistance =
            Mathf.Max(
                0f,
                moveDistance
            );

        randomHorizontalDistance =
            Mathf.Max(
                0f,
                randomHorizontalDistance
            );

        startScaleMultiplier =
            Mathf.Max(
                0.01f,
                startScaleMultiplier
            );

        endScaleMultiplier =
            Mathf.Max(
                0.01f,
                endScaleMultiplier
            );

        initialPoolSize =
            Mathf.Max(
                1,
                initialPoolSize
            );
    }
#endif
}

public sealed class DamagePopupRunner : MonoBehaviour
{
    public void Play(
        TextMeshPro popup,
        int damage,
        float duration,
        float moveDistance,
        float randomHorizontalDistance,
        float startScaleMultiplier,
        float endScaleMultiplier,
        float fadeStartTime
    )
    {
        StartCoroutine(
            PlayRoutine(
                popup,
                damage,
                duration,
                moveDistance,
                randomHorizontalDistance,
                startScaleMultiplier,
                endScaleMultiplier,
                fadeStartTime
            )
        );
    }

    private IEnumerator PlayRoutine(
        TextMeshPro popup,
        int damage,
        float duration,
        float moveDistance,
        float randomHorizontalDistance,
        float startScaleMultiplier,
        float endScaleMultiplier,
        float fadeStartTime
    )
    {
        if (popup == null)
            yield break;

        popup.text =
            damage.ToString();

        Color startColor =
            popup.color;

        startColor.a = 1f;

        popup.color =
            startColor;

        Vector3 baseScale =
            Vector3.one;

        Vector3 startPosition =
            popup.transform.position;

        Vector3 targetPosition =
            startPosition +
            new Vector3(
                Random.Range(
                    -randomHorizontalDistance,
                    randomHorizontalDistance
                ),
                moveDistance,
                0f
            );

        popup.transform.localScale =
            baseScale *
            startScaleMultiplier;

        Color transparentColor =
            startColor;

        transparentColor.a = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (popup == null)
                yield break;

            elapsedTime +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / duration
                );

            float moveProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f
                );

            popup.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    moveProgress
                );

            if (progress < 0.2f)
            {
                float scaleProgress =
                    progress / 0.2f;

                popup.transform.localScale =
                    Vector3.Lerp(
                        baseScale *
                        startScaleMultiplier,
                        baseScale,
                        scaleProgress
                    );
            }
            else
            {
                float scaleProgress =
                    Mathf.InverseLerp(
                        0.2f,
                        1f,
                        progress
                    );

                popup.transform.localScale =
                    Vector3.Lerp(
                        baseScale,
                        baseScale *
                        endScaleMultiplier,
                        scaleProgress
                    );
            }

            float fadeProgress =
                Mathf.InverseLerp(
                    fadeStartTime,
                    1f,
                    progress
                );

            popup.color =
                Color.Lerp(
                    startColor,
                    transparentColor,
                    fadeProgress
                );

            yield return null;
        }

        popup.color =
            startColor;

        DamagePopup.ReturnToPool(
            popup
        );
    }
}
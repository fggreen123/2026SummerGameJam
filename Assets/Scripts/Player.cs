using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Player : MonoBehaviour, ICardEffectTarget
{
    [Header("Player Data")]
    [SerializeField]
    private PlayerData data;

    [Header("Visual References")]
    [Tooltip("사망할 때 기울일 자식 Visual 오브젝트입니다.")]
    [SerializeField]
    private Transform visualTransform;

    [Tooltip("피격 점멸을 적용할 자식 스프라이트입니다.")]
    [SerializeField]
    private SpriteRenderer visualSpriteRenderer;

    [Header("Hit Flash")]
    [Tooltip("피격 시 점멸할 색상입니다.")]
    [SerializeField]
    private Color hitFlashColor =
        Color.red;

    [Tooltip("한 번 빨갛게 유지되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float hitFlashDuration = 0.08f;

    [Tooltip("빨간색으로 점멸하는 횟수입니다.")]
    [SerializeField, Min(1)]
    private int hitFlashCount = 2;

    [Tooltip("점멸 사이에 원래 색으로 돌아가는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float hitFlashInterval = 0.05f;

    [Header("Death Visual")]
    [Tooltip("Visual 중심을 기준으로 회전축을 둘 위치입니다. 보통 Y에 음수를 넣어 발밑으로 설정합니다.")]
    [SerializeField]
    private Vector2 deathPivotOffset =
        new Vector2(0f, -0.5f);

    [Tooltip("사망할 때 기울어지는 각도입니다. 음수는 시계 방향, 양수는 반시계 방향입니다.")]
    [SerializeField]
    private float deathTiltAngle = -75f;

    [Tooltip("사망 자세로 기울어지는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float deathTiltDuration = 0.35f;

    public PlayerData Data =>
        data;

    public string PlayerName =>
        data != null
            ? data.playerName
            : string.Empty;

    public string SkillNum =>
        data != null
            ? data.skillNum
            : string.Empty;

    private int BaseMaxHp =>
        data != null
            ? Mathf.Max(
                1,
                data.healthPoint
            )
            : 1;

    private int BaseDamage =>
        data != null
            ? Mathf.Max(
                0,
                data.damage
            )
            : 0;

    public int MaxHp =>
        BaseMaxHp + additionalMaxHp;

    public int Damage =>
        CurrentDamage;

    public float AttackSpeed =>
        data != null
            ? Mathf.Max(
                0f,
                data.attackSpeed
            )
            : 0f;

    public float AttackRange =>
        data != null
            ? Mathf.Max(
                0f,
                data.attackRange
            )
            : 0f;

    public float MoveSpeed =>
        data != null
            ? Mathf.Max(
                0f,
                data.moveSpeed
            )
            : 0f;

    public int CurrentHp { get; private set; }
    public int CurrentDamage { get; private set; }

    public bool IsDead =>
        CurrentHp <= 0;

    public event Action<int, int> HpChanged;
    public event Action Died;

    private Vector3 visualStartLocalPosition;
    private Quaternion visualStartLocalRotation;

    private Color visualOriginalColor =
        Color.white;

    private Coroutine hitFlashCoroutine;
    private Coroutine deathVisualCoroutine;
    private int additionalMaxHp;

    private void Reset()
    {
        visualSpriteRenderer =
            GetComponentInChildren<SpriteRenderer>();

        if (visualSpriteRenderer != null)
        {
            visualTransform =
                visualSpriteRenderer.transform;
        }
    }

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError(
                $"{name}: PlayerData가 연결되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }

        FindVisualReferences();

        if (visualTransform == null)
        {
            Debug.LogError(
                $"{name}: 사망 연출에 사용할 Visual Transform이 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        visualStartLocalPosition =
            visualTransform.localPosition;

        visualStartLocalRotation =
            visualTransform.localRotation;

        if (visualSpriteRenderer != null)
        {
            visualOriginalColor =
                visualSpriteRenderer.color;
        }

        CurrentHp =
            MaxHp;

        CurrentDamage =
            BaseDamage;
    }

    private void Start()
    {
        HpChanged?.Invoke(
            CurrentHp,
            MaxHp
        );
    }

    private void OnDisable()
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );

            hitFlashCoroutine = null;
        }

        if (deathVisualCoroutine != null)
        {
            StopCoroutine(
                deathVisualCoroutine
            );

            deathVisualCoroutine = null;
        }

        RestoreVisualColor();
    }

    private void FindVisualReferences()
    {
        if (visualSpriteRenderer == null)
        {
            visualSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (visualTransform == null &&
            visualSpriteRenderer != null)
        {
            visualTransform =
                visualSpriteRenderer.transform;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        HpChanged?.Invoke(CurrentHp, MaxHp);

        PlayHitFlash();
        Debug.Log(
            $"{PlayerName}이(가) {damage}의 피해를 받았습니다. 현재 HP: {CurrentHp}/{MaxHp}",
            this
        );

        if (IsDead)
        {
            Die();
        }
    }
    
    public void ApplySpade(int rank)
    {
        int amount = CardEffect.GetAmount(rank, CurrentHp, 0.5f, 0.6f, 0.7f);
        TakeDamage(amount);
    }

    public void ApplyClub(int rank)
    {
        if (IsDead)
        {
            return;
        }

        int amount = CardEffect.GetAmount(rank, CurrentDamage, 0.1f, 0.2f, 0.3f);

        if (rank <= 10)
        {
            CurrentDamage = Mathf.Max(0, CurrentDamage - amount);
            return;
        }

        StartCoroutine(ChangeDamageTemporarily(-amount));
    }

    public void ApplyHeart(int rank)
    {
        int amount = CardEffect.GetAmount(rank, BaseMaxHp, 0.3f, 0.6f, 1f);

        if (rank <= 10)
        {
            Heal(amount);
            return;
        }

        IncreaseMaxHp(amount);
    }

    public void ApplyDiamond(int rank)
    {
        if (IsDead)
        {
            return;
        }

        int amount = CardEffect.GetAmount(rank, CurrentDamage, 1f, 2f, 3f);

        if (rank <= 10)
        {
            CurrentDamage += amount;
            return;
        }

        StartCoroutine(ChangeDamageTemporarily(amount));
    }

    private void IncreaseMaxHp(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        additionalMaxHp += amount;
        CurrentHp += amount;
        HpChanged?.Invoke(CurrentHp, MaxHp);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        HpChanged?.Invoke(CurrentHp, MaxHp);
    }

    private IEnumerator ChangeDamageTemporarily(int amount)
    {
        int previousDamage = CurrentDamage;
        CurrentDamage = Mathf.Max(0, CurrentDamage + amount);
        int appliedAmount = CurrentDamage - previousDamage;

        yield return new WaitForSeconds(10f);

        CurrentDamage = Mathf.Max(0, CurrentDamage - appliedAmount);
    }

    private void PlayHitFlash()
    {
        if (visualSpriteRenderer == null)
            return;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );
        }

        hitFlashCoroutine =
            StartCoroutine(
                HitFlashRoutine()
            );
    }

    private IEnumerator HitFlashRoutine()
    {
        int flashCount =
            Mathf.Max(
                1,
                hitFlashCount
            );

        for (int i = 0;
             i < flashCount;
             i++)
        {
            visualSpriteRenderer.color =
                hitFlashColor;

            if (hitFlashDuration > 0f)
            {
                yield return new WaitForSeconds(
                    hitFlashDuration
                );
            }

            RestoreVisualColor();

            if (i < flashCount - 1 &&
                hitFlashInterval > 0f)
            {
                yield return new WaitForSeconds(
                    hitFlashInterval
                );
            }
        }

        RestoreVisualColor();

        hitFlashCoroutine = null;
    }

    private void RestoreVisualColor()
    {
        if (visualSpriteRenderer == null)
            return;

        visualSpriteRenderer.color =
            visualOriginalColor;
    }

    private void Die()
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );

            hitFlashCoroutine = null;
        }

        RestoreVisualColor();

        if (deathVisualCoroutine != null)
        {
            StopCoroutine(
                deathVisualCoroutine
            );
        }

        deathVisualCoroutine =
            StartCoroutine(
                PlayDeathVisual()
            );

        Died?.Invoke();

        Debug.Log(
            $"{PlayerName}가 사망했습니다.",
            this
        );
    }

    private IEnumerator PlayDeathVisual()
    {
        if (visualTransform == null)
            yield break;

        Vector3 startLocalPosition =
            visualTransform.localPosition;

        Quaternion startLocalRotation =
            visualTransform.localRotation;

        Quaternion targetLocalRotation =
            visualStartLocalRotation *
            Quaternion.Euler(
                0f,
                0f,
                deathTiltAngle
            );

        Vector3 pivotLocalPosition =
            visualStartLocalPosition +
            new Vector3(
                deathPivotOffset.x,
                deathPivotOffset.y,
                0f
            );

        if (deathTiltDuration <= 0f)
        {
            ApplyRotationAroundLocalPivot(
                visualStartLocalPosition,
                visualStartLocalRotation,
                targetLocalRotation,
                pivotLocalPosition
            );

            deathVisualCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime <
               deathTiltDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    deathTiltDuration
                );

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f
                );

            Quaternion currentLocalRotation =
                Quaternion.Slerp(
                    startLocalRotation,
                    targetLocalRotation,
                    easedProgress
                );

            ApplyRotationAroundLocalPivot(
                startLocalPosition,
                startLocalRotation,
                currentLocalRotation,
                pivotLocalPosition
            );

            yield return null;
        }

        ApplyRotationAroundLocalPivot(
            visualStartLocalPosition,
            visualStartLocalRotation,
            targetLocalRotation,
            pivotLocalPosition
        );

        deathVisualCoroutine = null;
    }

    private void ApplyRotationAroundLocalPivot(
        Vector3 startLocalPosition,
        Quaternion startLocalRotation,
        Quaternion currentLocalRotation,
        Vector3 pivotLocalPosition
    )
    {
        if (visualTransform == null)
            return;

        Vector3 originalOffset =
            startLocalPosition -
            pivotLocalPosition;

        Quaternion rotationDifference =
            currentLocalRotation *
            Quaternion.Inverse(
                startLocalRotation
            );

        Vector3 rotatedOffset =
            rotationDifference *
            originalOffset;

        visualTransform.localRotation =
            currentLocalRotation;

        visualTransform.localPosition =
            pivotLocalPosition +
            rotatedOffset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        hitFlashDuration =
            Mathf.Max(
                0f,
                hitFlashDuration
            );

        hitFlashCount =
            Mathf.Max(
                1,
                hitFlashCount
            );

        hitFlashInterval =
            Mathf.Max(
                0f,
                hitFlashInterval
            );

        deathTiltDuration =
            Mathf.Max(
                0f,
                deathTiltDuration
            );

        if (data == null)
            return;

        if (string.IsNullOrWhiteSpace(
            data.playerName))
        {
            Debug.LogWarning(
                $"{name}: PlayerData의 playerName이 비어 있습니다.",
                this
            );
        }
    }
#endif
}

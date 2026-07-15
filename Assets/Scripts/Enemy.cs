using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Enemy : MonoBehaviour, ICardEffectTarget
{
    [Header("Enemy Data")]
    [SerializeField]
    private EnemyData data;

    [Header("Visual References")]
    [Tooltip("사망할 때 기울일 자식 Visual 오브젝트입니다.")]
    [SerializeField]
    private Transform visualTransform;

    [Tooltip("피격 점멸을 적용할 자식 Visual의 SpriteRenderer입니다.")]
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


    [Tooltip("사망할 때 기울어지는 각도입니다.")]
    [SerializeField]
    private float deathTiltAngle = -75f;

    [Tooltip("사망 자세로 기울어지는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float deathTiltDuration = 0.35f;

    [Tooltip("쓰러진 뒤 오브젝트가 삭제되기 전까지 유지되는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float deathRemainDuration = 0.4f;

    public EnemyData Data =>
        data;

    public string EnemyName =>
        data != null
            ? data.enemyName
            : string.Empty;

    private int BaseMaxHp =>
        data != null
            ? Mathf.Max(
                1,
                data.hp
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

    private Color visualOriginalColor =
        Color.white;


    private Coroutine hitFlashCoroutine;
    private Coroutine deathCoroutine;

    private bool deathStarted;
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
                $"{name}: EnemyData가 연결되지 않았습니다.",
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

        if (deathCoroutine != null)
        {
            StopCoroutine(
                deathCoroutine
            );

            deathCoroutine = null;
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
            $"{EnemyName}이(가) {damage}의 피해를 받았습니다. 현재 HP: {CurrentHp}/{MaxHp}",
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

    public void ApplyJoker()
    {
        if (CurrentHp <= 1)
        {
            return;
        }

        int percentage = UnityEngine.Random.Range(1, 100);
        int amount = Mathf.CeilToInt(CurrentHp * percentage * 0.01f);
        TakeDamage(Mathf.Min(amount, CurrentHp - 1));
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

    private IEnumerator ChangeDamageTemporarily(int amount)
    {
        int previousDamage = CurrentDamage;
        CurrentDamage = Mathf.Max(0, CurrentDamage + amount);
        int appliedAmount = CurrentDamage - previousDamage;

        yield return new WaitForSeconds(10f);

        CurrentDamage = Mathf.Max(0, CurrentDamage - appliedAmount);
    }

    public void Heal(int amount)
    {
        if (IsDead ||
            amount <= 0)
        {
            return;
        }

        CurrentHp =
            Mathf.Min(
                MaxHp,
                CurrentHp + amount
            );

        HpChanged?.Invoke(
            CurrentHp,
            MaxHp
        );
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
            if (visualSpriteRenderer == null)
                yield break;

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
        if (deathStarted)
            return;

        deathStarted = true;

        Died?.Invoke();

        Debug.Log(
            $"{EnemyName}이 사망했습니다.",
            this
        );

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;

            rb.simulated =
                false;
        }

        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>();

        foreach (Collider2D collider in colliders)
        {
            collider.enabled =
                false;
        }

        EnemyAI enemyAI =
            GetComponent<EnemyAI>();

        if (enemyAI != null)
        {
            enemyAI.enabled =
                false;
        }

        EnemyAttack enemyAttack =
            GetComponent<EnemyAttack>();

        if (enemyAttack != null)
        {
            enemyAttack.enabled =
                false;
        }

        deathCoroutine =
            StartCoroutine(
                DeathRoutine()
            );
    }

    private IEnumerator DeathRoutine()
    {
        float totalFlashTime =
            Mathf.Max(
                1,
                hitFlashCount
            ) *
            Mathf.Max(
                0f,
                hitFlashDuration
            );

        totalFlashTime +=
            Mathf.Max(
                0,
                hitFlashCount - 1
            ) *
            Mathf.Max(
                0f,
                hitFlashInterval
            );

        if (totalFlashTime > 0f)
        {
            yield return new WaitForSeconds(
                totalFlashTime
            );
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(
                hitFlashCoroutine
            );

            hitFlashCoroutine = null;
        }

        RestoreVisualColor();

        yield return PlayDeathVisual();

        if (deathRemainDuration > 0f)
        {
            yield return new WaitForSeconds(
                deathRemainDuration
            );
        }

        deathCoroutine = null;

        Destroy(
            gameObject
        );
    }
private IEnumerator PlayDeathVisual()
    {
        if (visualTransform == null)
            yield break;

        Vector3 startPosition =
            visualTransform.localPosition;

        Quaternion startRotation =
            visualTransform.localRotation;

        Quaternion targetRotation =
            startRotation *
            Quaternion.Euler(
                0f,
                0f,
                deathTiltAngle
            );
        Vector3 targetPosition =
            startPosition +
            Vector3.right * 0.1f +
            Vector3.down * 0.07f;


        if (deathTiltDuration <= 0f)
        {
            visualTransform.localRotation =
                targetRotation;

            visualTransform.localPosition =
                targetPosition;

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < deathTiltDuration)
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

            visualTransform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    easedProgress
                );

            visualTransform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    easedProgress
                );

            yield return null;
        }

        visualTransform.localRotation =
            targetRotation;

        visualTransform.localPosition =
            targetPosition;
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

        Vector3 correctedPosition =
            pivotLocalPosition +
            rotatedOffset;

        correctedPosition.x =
            startLocalPosition.x;

        visualTransform.localRotation =
            currentLocalRotation;

        visualTransform.localPosition =
            correctedPosition;
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

        deathRemainDuration =
            Mathf.Max(
                0f,
                deathRemainDuration
            );

        if (data == null)
            return;

        if (string.IsNullOrWhiteSpace(
            data.enemyName))
        {
            Debug.LogWarning(
                $"{name}: EnemyData의 enemyName이 비어 있습니다.",
                this
            );
        }
    }
#endif
}

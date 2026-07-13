using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Player : MonoBehaviour, IDamageable
{
    [Header("Player Data")]
    [SerializeField]
    private PlayerData data;

    public PlayerData Data => data;

    public string PlayerName =>
        data != null
            ? data.playerName
            : string.Empty;

    public string SkillNum =>
        data != null
            ? data.skillNum
            : string.Empty;

    public int MaxHp =>
        data != null
            ? Mathf.Max(
                1,
                data.healthPoint
            )
            : 1;

    public int Damage =>
        data != null
            ? Mathf.Max(
                0,
                data.damage
            )
            : 0;

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

    public bool IsDead =>
        CurrentHp <= 0;

    public event Action<int, int> HpChanged;
    public event Action Died;

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

        CurrentHp =
            MaxHp;
    }

    private void Start()
    {
        HpChanged?.Invoke(
            CurrentHp,
            MaxHp
        );
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        CurrentHp =
            Mathf.Max(
                0,
                CurrentHp -
                damage
            );

        HpChanged?.Invoke(
            CurrentHp,
            MaxHp
        );

        Debug.Log(
            $"{PlayerName}이 {damage} 피해를 받았습니다. " +
            $"현재 HP: {CurrentHp}/{MaxHp}",
            this
        );

        if (CurrentHp <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHp =
            Mathf.Min(
                MaxHp,
                CurrentHp +
                amount
            );

        HpChanged?.Invoke(
            CurrentHp,
            MaxHp
        );
    }

    private void Die()
    {
        Died?.Invoke();

        Debug.Log(
            $"{PlayerName}이 사망했습니다.",
            this
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
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
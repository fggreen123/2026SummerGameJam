using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
[DisallowMultipleComponent]
public sealed class HealthBarUI : MonoBehaviour
{
    [Header("Health Target")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private Enemy enemy;

    private Slider slider;
    private bool isSubscribed;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;

        FindTarget();
    }

    private void Start()
    {
        FindTarget();
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        FindTarget();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void FindTarget()
    {
        if (player == null)
            player = GetComponentInParent<Player>();

        if (enemy == null)
            enemy = GetComponentInParent<Enemy>();
    }

    private void Subscribe()
    {
        if (isSubscribed)
            return;

        if (player != null)
        {
            player.HpChanged += UpdateHealthBar;
            isSubscribed = true;
            return;
        }

        if (enemy != null)
        {
            enemy.HpChanged += UpdateHealthBar;
            isSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
            return;

        if (player != null)
            player.HpChanged -= UpdateHealthBar;

        if (enemy != null)
            enemy.HpChanged -= UpdateHealthBar;

        isSubscribed = false;
    }

    private void Refresh()
    {
        if (player != null)
        {
            UpdateHealthBar(
                player.CurrentHp,
                player.MaxHp
            );

            return;
        }

        if (enemy != null)
        {
            UpdateHealthBar(
                enemy.CurrentHp,
                enemy.MaxHp
            );

            return;
        }

        slider.value = 0f;

        Debug.LogWarning(
            $"{name}: 체력바에 Player 또는 Enemy가 연결되지 않았습니다.",
            this
        );
    }

    private void UpdateHealthBar(
        int currentHp,
        int maxHp
    )
    {
        if (slider == null)
            return;

        if (maxHp <= 0)
        {
            slider.value = 0f;
            return;
        }

        slider.value = Mathf.Clamp01(
            (float)currentHp / maxHp
        );
    }
}
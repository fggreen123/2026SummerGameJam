using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CardSuit
{
    Spade,
    Club,
    Heart,
    Diamond
}

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CardSystem : MonoBehaviour
{
    private const float DragScale = 0.5f;
    private const float EnemyTargetDistance = 2f;

    private CardDistribution cardDistribution;
    private Button button;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Enemy targetEnemy;
    private bool selected;
    private bool dragging;
    private Vector3 dragOffset;
    private Vector3 cardScale;

    public Coroutine MoveCoroutine { get; set; }
    public CardSuit Suit { get; private set; }
    public int Rank { get; private set; }

    private string RankName => Rank switch
    {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => Rank.ToString()
    };

    public void Initialize(CardDistribution distribution, CardSuit suit, int rank)
    {
        cardDistribution = distribution;
        Suit = suit;
        Rank = rank;

        gameObject.name = $"{Suit} {RankName}";
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        cardScale = transform.localScale;
        RegisterButtonClick();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(Select);
    }

    private void OnMouseDown()
    {
        if (!selected)
        {
            Select();
            return;
        }

        dragging = true;
        cardDistribution.StopCardMovement(gameObject);
        SetSortingOrder(100);

        Vector3 mousePosition = GetMouseWorldPosition();
        dragOffset = (transform.position - mousePosition) * DragScale;
        transform.localScale = cardScale * DragScale;
        transform.position = mousePosition + dragOffset;
    }

    private void OnMouseEnter()
    {
        if (selected)
        {
            cardDistribution.SetCardHovered(gameObject, true);
        }
    }

    private void OnMouseExit()
    {
        if (selected && !dragging)
        {
            cardDistribution.SetCardHovered(gameObject, false);
        }
    }

    private void OnMouseDrag()
    {
        if (dragging)
        {
            transform.position = GetMouseWorldPosition() + dragOffset;
            UpdateTargetEnemy();
        }
    }

    private void OnMouseUp()
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;
        AttackTargetEnemy();
        SetTargetEnemy(null);
        transform.localScale = cardScale;
        cardDistribution.SnapCardToHand(gameObject);
    }

    private void RegisterButtonClick()
    {
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
    }

    private void Select()
    {
        if (selected)
        {
            return;
        }

        cardDistribution.CardSelected(gameObject);
        Debug.Log($"Selected Card: {Suit} {RankName} ({Rank})", this);
    }

    public void SetSortingOrder(int sortingOrder)
    {
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPosition);
    }

    private void UpdateTargetEnemy()
    {
        Enemy closestEnemy = null;
        float closestDistance = EnemyTargetDistance;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, EnemyTargetDistance))
        {
            if (!hit.CompareTag("Enemy") || !hit.TryGetComponent(out Enemy enemy))
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestEnemy = enemy;
        }

        SetTargetEnemy(closestEnemy);
    }

    private void SetTargetEnemy(Enemy enemy)
    {
        if (targetEnemy == enemy)
        {
            return;
        }

        if (targetEnemy != null)
        {
            SetEnemyOutline(targetEnemy, false);
        }

        targetEnemy = enemy;

        if (targetEnemy != null)
        {
            SetEnemyOutline(targetEnemy, true);
        }
    }

    private void SetEnemyOutline(Enemy enemy, bool visible)
    {
        EnemyOutline eo;
        eo = enemy.GetComponent<EnemyOutline>();
        eo.outlineSize = visible ? 16 : 0;
    }

    private void AttackTargetEnemy()
    {
        if (targetEnemy == null || Rank >= 11)
        {
            return;
        }

        targetEnemy.TakeDamage(Rank);
    }
}

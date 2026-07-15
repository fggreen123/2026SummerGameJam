using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CardSuit
{
    Spade,
    Club,
    Heart,
    Diamond,
    Joker
}

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CardSystem : MonoBehaviour
{
    private const float DragScale = 0.5f;
    private const float TargetDistance = 2f;

    private CardDistribution cardDistribution;
    private Button button;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer suitRenderer;
    private SpriteRenderer numberRenderer;
    private GameObject target;
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
    private string CardName => Suit == CardSuit.Joker ? "Joker" : $"{Suit} {RankName}";
    private string SuitName => Suit switch
    {
        CardSuit.Spade => "스페이드",
        CardSuit.Club => "클로버",
        CardSuit.Heart => "하트",
        CardSuit.Diamond => "다이아몬드",
        _ => "조커"
    };

    public void Initialize(CardDistribution distribution, CardSuit suit, int rank, Sprite suitSprite, Sprite numberSprite)
    {
        cardDistribution = distribution;
        Suit = suit;
        Rank = rank;

        SetCardVisuals(suitSprite, numberSprite);
        gameObject.name = CardName;
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
        suitRenderer = CreateCardRenderer("Suit");
        numberRenderer = CreateCardRenderer("Number");
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
            UpdateTarget();
        }
    }

    private void OnMouseUp()
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;
        if (UseCard())
        {
            return;
        }

        SetTarget(null);
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
        Debug.Log($"Selected Card: {CardName} ({Rank})", this);
    }

    public void SetSortingOrder(int sortingOrder)
    {
        spriteRenderer.sortingOrder = sortingOrder;
        suitRenderer.sortingOrder = sortingOrder + 1;
        numberRenderer.sortingOrder = sortingOrder + 1;
    }

    private SpriteRenderer CreateCardRenderer(string objectName)
    {
        SpriteRenderer renderer = new GameObject(objectName).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(transform, false);
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        return renderer;
    }

    private void SetCardVisuals(Sprite suitSprite, Sprite numberSprite)
    {
        suitRenderer.sprite = suitSprite;
        numberRenderer.sprite = numberSprite;
        suitRenderer.transform.localScale = Vector3.one;

        if (Suit == CardSuit.Joker)
        {
            suitRenderer.transform.localPosition = -suitSprite.bounds.center;
            numberRenderer.transform.localPosition = new Vector2(-0.065f, 0f);
            numberRenderer.transform.localScale = Vector3.one;
            return;
        }

        suitRenderer.transform.localPosition = Vector3.zero;
        numberRenderer.transform.localPosition = new Vector2(Rank == 10 ? -0.065f : -0.075f, 0.12f);
        numberRenderer.transform.localScale = Vector3.one;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPosition);
    }

    private void UpdateTarget()
    {
        GameObject closestTarget = null;
        float closestDistance = TargetDistance;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, TargetDistance))
        {
            if (!CanTarget(hit))
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestTarget = hit.gameObject;
        }

        SetTarget(closestTarget);
    }

    private bool CanTarget(Collider2D hit)
    {
        return hit.CompareTag("Enemy") ||
               (Suit != CardSuit.Joker && hit.CompareTag("Player"));
    }

    private void SetTarget(GameObject newTarget)
    {
        if (target == newTarget)
        {
            return;
        }

        if (target != null)
        {
            SetOutline(target, false);
        }

        target = newTarget;

        if (target != null)
        {
            SetOutline(target, true);
        }
    }

    private void SetOutline(GameObject cardTarget, bool visible)
    {
        if (cardTarget.CompareTag("Enemy"))
        {
            EnemyOutline outline = cardTarget.GetComponentInChildren<EnemyOutline>();
            if (outline == null)
            {
                outline = cardTarget.GetComponentInChildren<SpriteRenderer>()
                    .gameObject.AddComponent<EnemyOutline>();
            }

            outline.outlineSize = visible ? 16 : 0;
            return;
        }

        cardTarget.GetComponentInChildren<PlayerOutline>().outlineSize = visible ? 1 : 0;
    }

    private bool UseCard()
    {
        if (target == null)
        {
            return false;
        }
        GameObject usedTarget = target;
        ICardEffectTarget cardTarget = usedTarget.GetComponent<ICardEffectTarget>();

        LogCardUse(usedTarget);
        SetTarget(null);
        switch (Suit)
        {
            case CardSuit.Spade:
                cardTarget.ApplySpade(Rank);
                PlayCameraShake();
                break;
            case CardSuit.Club:
                cardTarget.ApplyClub(Rank);
                break;
            case CardSuit.Heart:
                cardTarget.ApplyHeart(Rank);
                break;
            case CardSuit.Diamond:
                cardTarget.ApplyDiamond(Rank);
                break;
            case CardSuit.Joker:
                usedTarget.GetComponent<Enemy>().ApplyJoker();
                break;
        }
        cardDistribution.RemoveCard(gameObject);
        cardDistribution.SetPlayerMoveable(true);
        return true;
    }

    private void PlayCameraShake()
    {
        CameraShake shake = mainCamera.GetComponent<CameraShake>()
            ?? mainCamera.gameObject.AddComponent<CameraShake>();

        shake.ShakeCamera(0.2f, 0.15f);
    }

    private void LogCardUse(GameObject usedTarget)
    {
        string targetName = usedTarget.CompareTag("Enemy") ? "적" : "플레이어";
        string rankName = Suit == CardSuit.Joker ? string.Empty : $" {RankName}";

        Debug.Log($"{targetName}에게 {SuitName}{rankName} 사용!", this);
    }
}

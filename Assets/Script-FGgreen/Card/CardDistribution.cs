using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardDistribution: MonoBehaviour
{
    [SerializeField] private CardInformationTextSystem CardInformationText;
    [SerializeField] private PlayerMovement Player;
    [SerializeField] private Gamemanager GameManager; 
    [SerializeField] private Transform HandRoot;
    [SerializeField] private Gamemanager gamemanager;
    public Sprite[] CardSuits = { };
    public Sprite[] CardNumbers = { };
    public GameObject Card;
    public GameObject CardWindow;
    public float speed = 5f;
    public int TargetCardAmount;
    public int CurrentCardAmount;
    public Vector2 HandCenter = new Vector2(0f, -8.5f);
    public float HandSpacing = 1.8f;
    public float HoverHeight = 1f;
    public GameObject HoveredDistributedCard { get; private set; }

    public List<GameObject> CurrentCardList = new List<GameObject>();
    private readonly List<GameObject> DistributedCardList = new List<GameObject>();
    private static readonly List<CardData> PreservedHand = new List<CardData>();
    private readonly CardSuit[] CardSuitOrder =
    {
        CardSuit.Spade,
        CardSuit.Club,
        CardSuit.Heart,
        CardSuit.Diamond
    };
    private int jokerCardIndex;
    public bool HandCenterToggle=true;
    private Camera mainCamera;

    private void Start()
    {
        if (CardInformationText == null)
        {
            CardInformationText = FindFirstObjectByType<CardInformationTextSystem>(FindObjectsInactive.Include);
        }

        CardInformationText.Hide();
        mainCamera = Camera.main;
        RestoreHand();
        jokerCardIndex = Random.value < 0.05f
            ? Random.Range(0, CardSuitOrder.Length)
            : -1;
        StartCoroutine(StartCardDistribute());
    }

    private void Update()
    {
        DetectDistributedCardHover();

        if (Keyboard.current.tabKey.wasPressedThisFrame && CurrentCardList.Count > 0)
        {
            Player.Moveable = HandCenterToggle ? true : false;
            HandCenter = HandCenterToggle ? new Vector2(0f, -6f) : new Vector2(0f, -4f);
            HandCenterToggle = !HandCenterToggle;
            UpdateHandLayout();
        }
    }

    private void DetectDistributedCardHover()
    {
        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(mousePosition);

        if (hit != null &&
            DistributedCardList.Contains(hit.gameObject) &&
            !CurrentCardList.Contains(hit.gameObject))
        {
            SetHoveredDistributedCard(hit.gameObject);
            return;
        }

        SetHoveredDistributedCard(null);
    }

    private void SetHoveredDistributedCard(GameObject card)
    {
        if (HoveredDistributedCard == card)
        {
            return;
        }

        HoveredDistributedCard = card;

        if (card == null)
        {
            CardInformationText.Hide();
            return;
        }

        CardInformationText.Show(
            card.GetComponent<CardSystem>().Suit,
            DistributedCardList.IndexOf(card) == 3
        );
    }

    IEnumerator StartCardDistribute()
    {
        switch (GameManager.CurrentFloor)
        {
            case 1:
                TargetCardAmount = 2;
                break;
            case 2:
                TargetCardAmount = 3;
                break;
            case 3:
                TargetCardAmount = 4;
                break;
        }
        CurrentCardAmount = 0;
        Player.Moveable = false;
        GameObject CardWindowClone = Instantiate(CardWindow);
        Vector3 targetScale = CardWindowClone.transform.localScale;
        CardWindowClone.transform.localScale = Vector3.zero;
        StartCoroutine(SmoothScale(CardWindowClone.transform, targetScale, 1f));


        CardDistribute(new Vector2(-4f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(-1.35f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(1.35f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(4f, 0));

        yield return new WaitUntil(() => CurrentCardAmount == TargetCardAmount);
        HideUnselectedCards();
        StartCoroutine(SmoothScale(CardWindowClone.transform, Vector2.zero, 1f));
        StartCoroutine(SmoothMove(CardWindowClone, new Vector2(0, -6), null));
        HandCenterToggle = false;
        HandCenter = new Vector2(0f, -11f);
        UpdateHandLayout();
        Player.Moveable = true;
        yield return new WaitForSeconds(3f);
        Destroy(CardWindowClone);
    }
    public void CardDistribute(Vector2 Locate)
    {
        Player.Moveable = false;
        GameObject CardClone = Instantiate(Card);
        CardClone.transform.position = new Vector2(0, 10f);

        CardSystem cardSystem = CardClone.GetComponent<CardSystem>();
        int cardIndex = DistributedCardList.Count;
        bool isJoker = cardIndex == jokerCardIndex;
        CardSuit suit = isJoker ? CardSuit.Joker : CardSuitOrder[cardIndex];
        int rank = isJoker ? 14 : Random.Range(1, 14);

        DistributedCardList.Add(CardClone);
        cardSystem.Initialize(this, suit, rank, CardSuits[(int)suit], CardNumbers[rank - 1]);
        MoveCard(CardClone, Locate);
    }

    public void CardSelected(GameObject selectedCard)
    {
        CurrentCardAmount++;
        AddCardToHand(selectedCard);
        UpdateHandLayout();
    }

    public void PreserveHandForNextScene()
    {
        PreservedHand.Clear();

        foreach (GameObject card in CurrentCardList)
        {
            CardSystem cardSystem = card.GetComponent<CardSystem>();
            PreservedHand.Add(new CardData(cardSystem.Suit, cardSystem.Rank));
        }
    }

    private void RestoreHand()
    {
        if (PreservedHand.Count == 0)
        {
            return;
        }

        foreach (CardData cardData in PreservedHand)
        {
            GameObject card = Instantiate(Card);
            card.GetComponent<CardSystem>().Initialize(
                this,
                cardData.Suit,
                cardData.Rank,
                CardSuits[(int)cardData.Suit],
                CardNumbers[cardData.Rank - 1]
            );
            AddCardToHand(card);
        }

        PreservedHand.Clear();
        HandCenterToggle = false;
        HandCenter = new Vector2(0f, -11f);
        UpdateHandLayout();
    }

    private void AddCardToHand(GameObject card)
    {
        CurrentCardList.Add(card);
        card.transform.SetParent(HandRoot, true);
        card.GetComponent<CardSystem>().SetSelected(true);
        card.GetComponent<Button>().interactable = false;
    }

    public void SetPlayerMoveable(bool moveable)
    {
        Player.Moveable = moveable;
    }

    private void UpdateHandLayout()
    {
        if (CurrentCardList.Count <= 0) return;
        for (int i = 0; i < CurrentCardList.Count; i++)
        {
            GameObject card = CurrentCardList[i];
            CardSystem cardSystem = card.GetComponent<CardSystem>();

            cardSystem.SetSortingOrder(10 + (i * 2));
            MoveCard(card, GetHandPosition(i));
        }
    }

    public void SetCardHovered(GameObject card, bool hovered)
    {
        int index = CurrentCardList.IndexOf(card);
        Vector2 targetPosition = GetHandPosition(index) + Vector2.up * (hovered ? HoverHeight : 0f);
        MoveCard(card, targetPosition);
    }

    public void StopCardMovement(GameObject card)
    {
        StopCardMovement(card.GetComponent<CardSystem>());
    }

    public void SnapCardToHand(GameObject card)
    {
        int index = CurrentCardList.IndexOf(card);
        CardSystem cardSystem = card.GetComponent<CardSystem>();

        StopCardMovement(cardSystem);
        cardSystem.SetSortingOrder(10 + (index * 2));
        card.transform.localPosition = GetHandPosition(index);
    }

    public void RemoveCard(GameObject card)
    {
        StopCardMovement(card.GetComponent<CardSystem>());
        CurrentCardList.Remove(card);
        Destroy(card);
        UpdateHandLayout();
    }

    private void HideUnselectedCards()
    {
        foreach (GameObject card in DistributedCardList)
        {
            if (CurrentCardList.Contains(card))
            {
                continue;
            }

            CardSystem cardSystem = card.GetComponent<CardSystem>();
            Button button = card.GetComponent<Button>();
            button.interactable = false;

            StopCardMovement(cardSystem);
            cardSystem.MoveCoroutine = StartCoroutine(ReturnAndDestroy(card, cardSystem));
        }
    }

    private Vector2 GetHandPosition(int index)
    {
        float startX = HandCenter.x - (HandSpacing * (CurrentCardList.Count - 1) * 0.5f);
        return new Vector2(startX + (HandSpacing * index), HandCenter.y);
    }

    private void MoveCard(GameObject card, Vector2 targetPosition)
    {
        CardSystem cardSystem = card.GetComponent<CardSystem>();
        StopCardMovement(cardSystem);
        cardSystem.MoveCoroutine = StartCoroutine(SmoothMove(card, targetPosition, cardSystem));
    }

    private void StopCardMovement(CardSystem cardSystem)
    {
        if (cardSystem.MoveCoroutine == null)
        {
            return;
        }

        StopCoroutine(cardSystem.MoveCoroutine);
        cardSystem.MoveCoroutine = null;
    }

    private IEnumerator SmoothMove(GameObject target, Vector2 location, CardSystem cardSystem)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 targetPosition = new Vector3(location.x, location.y, target.transform.localPosition.z);
        while (target != null && Vector2.Distance(target.transform.localPosition, targetPosition) > 0.01f)
        {
            target.transform.localPosition = Vector2.Lerp(
                target.transform.localPosition,
                targetPosition,
                speed * Time.deltaTime
            );

            yield return null;
        }

        if (target == null)
        {
            yield break;
        }

        target.transform.localPosition = targetPosition;
        if (cardSystem != null)
        {
            cardSystem.MoveCoroutine = null;
        }
    }

    private IEnumerator ReturnAndDestroy(GameObject card, CardSystem cardSystem)
    {
        yield return StartCoroutine(SmoothMove(card, new Vector2(0,10f), cardSystem));
        CurrentCardList.Remove(card);
        Destroy(card);
        Player.Moveable = true;
        HandCenter = new Vector2(0f, -11f);
        UpdateHandLayout();
    }

    private IEnumerator SmoothScale(Transform target, Vector3 targetScale, float duration)
    {
        target.transform.position = Vector2.zero;
        Vector3 startScale = target.localScale;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            target.localScale = Vector3.Lerp(startScale, targetScale, t / duration);
            yield return null;
        }

        target.localScale = targetScale;
    }

    private readonly struct CardData
    {
        public readonly CardSuit Suit;
        public readonly int Rank;

        public CardData(CardSuit suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }
    }
}

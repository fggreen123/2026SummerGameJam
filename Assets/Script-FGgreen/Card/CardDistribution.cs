using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardDistribution: MonoBehaviour
{
    [SerializeField] private PlayerMove Player;
    [SerializeField] private Gamemanager GameManager; 
    [SerializeField] private Transform HandRoot;
    public GameObject Card;
    public GameObject CardWindow;
    public float speed = 5f;
    public int TargetCardAmount;
    public int CurrentCardAmount;
    public Vector2 HandCenter = new Vector2(0f, -8.5f);
    public float HandSpacing = 1.8f;
    public float HoverHeight = 0.7f;

    public List<GameObject> CurrentCardList = new List<GameObject>();
    private readonly List<GameObject> DistributedCardList = new List<GameObject>();
    private readonly CardSuit[] CardSuitOrder =
    {
        CardSuit.Spade,
        CardSuit.Club,
        CardSuit.Heart,
        CardSuit.Diamond
    };
    private bool HandCenterToggle=true;

    private void Start()
    {
        StartCoroutine(StartCardDistribute());
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            HandCenter = HandCenterToggle ? new Vector2(0f, -11f) : new Vector2(0f, -8.5f);
            HandCenterToggle = !HandCenterToggle;
            UpdateHandLayout();
        }
    }

    IEnumerator StartCardDistribute()
    {
        switch (GameManager.CurrentFloor)
        {
            case 1:
                TargetCardAmount = 1;
                break;
            case 2:
                TargetCardAmount = 2;
                break;
            case 3:
                TargetCardAmount = 4;
                break;
        }
        CurrentCardAmount = 0;
        GameObject CardWindowClone = Instantiate(CardWindow);
        Vector3 targetScale = CardWindowClone.transform.localScale;
        CardWindowClone.transform.localScale = Vector3.zero;
        StartCoroutine(SmoothScale(CardWindowClone.transform, targetScale, 1f));


        CardDistribute(new Vector2(-2.5f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(-7.5f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(2.5f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(7.5f, 0));

        yield return new WaitUntil(() => CurrentCardAmount == TargetCardAmount);
        HideUnselectedCards();
        StartCoroutine(SmoothScale(CardWindowClone.transform, Vector2.zero, 1f));
        yield return new WaitForSeconds(2f);
        Destroy(CardWindowClone);
        Player.Moveable = true;
    }
    public void CardDistribute(Vector2 Locate)
    {
        Player.Moveable = false;
        GameObject CardClone = Instantiate(Card);
        CardClone.transform.position = new Vector2(0, 10f);

        CardSystem cardSystem = CardClone.GetComponent<CardSystem>();
        CardSuit suit = CardSuitOrder[DistributedCardList.Count];
        DistributedCardList.Add(CardClone);
        cardSystem.Initialize(this, suit, Random.Range(1, 14));
        MoveCard(CardClone, Locate);
    }

    public void CardSelected(GameObject selectedCard)
    {
        CurrentCardAmount++;

        CurrentCardList.Add(selectedCard);
        selectedCard.transform.SetParent(HandRoot, true);

        CardSystem cardSystem = selectedCard.GetComponent<CardSystem>();
        cardSystem.SetSelected(true);

        Button button = selectedCard.GetComponent<Button>();
        button.interactable = false;

        UpdateHandLayout();
    }

    private void UpdateHandLayout()
    {
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
        Vector3 targetPosition = new Vector3(location.x, location.y, target.transform.localPosition.z);

        while (Vector2.Distance(target.transform.localPosition, targetPosition) > 0.01f)
        {
            target.transform.localPosition = Vector2.Lerp(
                target.transform.localPosition,
                targetPosition,
                speed * Time.deltaTime
            );

            yield return null;
        }

        target.transform.localPosition = targetPosition;
        cardSystem.MoveCoroutine = null;
    }

    private IEnumerator ReturnAndDestroy(GameObject card, CardSystem cardSystem)
    {
        yield return StartCoroutine(SmoothMove(card, new Vector2(0,10f), cardSystem));
        Destroy(card);
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
}
 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardDistribution: MonoBehaviour
{
    [SerializeField] private PlayerMove Player;
    [SerializeField] private Gamemanager GameManager; 
    public GameObject Card;
    public GameObject CardWindow;
    public float speed = 5f;
    public int TargetCardAmount;
    public int CurrentCardAmount;
    public Vector2 HandCenter = new Vector2(0f, -8.5f);
    public float HandSpacing = 1.8f;

    public List<GameObject> CurrentCardList = new List<GameObject>();
    private readonly List<GameObject> DistributedCardList = new List<GameObject>();
    private readonly Vector2 CardStartPosition = new Vector2(0f, 10f);
    private readonly Vector2 HiddenHandCenter = new Vector2(0f, -8.5f);
    private readonly Vector2 VisibleHandCenter = new Vector2(0f, -4.5f);
    private bool HandCenterToggle=false;

    private void Start()
    {
        StartCoroutine(StartCardDistribute());
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            HandCenter = HandCenterToggle ? HiddenHandCenter : VisibleHandCenter;
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
        CardClone.transform.position = new Vector3(CardStartPosition.x, CardStartPosition.y, CardClone.transform.position.z);
        DistributedCardList.Add(CardClone);
        CardSystem cardSystem = InitializeCard(CardClone);
        cardSystem.MoveCoroutine = StartCoroutine(SmoothMove(CardClone, Locate, cardSystem));
    }

    public void CardSelected(GameObject selectedCard)
    {
        CurrentCardAmount++;

        CurrentCardList.Add(selectedCard);
        selectedCard.transform.SetParent(Camera.main.transform, true);

        CardSystem cardSystem = selectedCard.GetComponent<CardSystem>();
        cardSystem.SetSelected(true);

        Button button = selectedCard.GetComponent<Button>();
        button.interactable = false;

        UpdateHandLayout();
    }

    private void UpdateHandLayout()
    {
        int cardCount = CurrentCardList.Count;
        float startX = HandCenter.x - (HandSpacing * (cardCount - 1) * 0.5f);

        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = CurrentCardList[i];

            SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = i + 10;

            Vector2 targetPosition = new Vector2(startX + (HandSpacing * i), HandCenter.y);
            CardSystem cardSystem = card.GetComponent<CardSystem>();
            if (cardSystem.MoveCoroutine != null)
            {
                StopCoroutine(cardSystem.MoveCoroutine);
            }

            cardSystem.MoveCoroutine = StartCoroutine(SmoothMove(card, targetPosition, cardSystem));
        }
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

            if (cardSystem.MoveCoroutine != null)
            {
                StopCoroutine(cardSystem.MoveCoroutine);
            }

            cardSystem.MoveCoroutine = StartCoroutine(ReturnAndDestroy(card, cardSystem));
        }
    }

    private CardSystem InitializeCard(GameObject card)
    {
        CardSystem cardSystem = card.GetComponent<CardSystem>();
        cardSystem.Initialize(this);
        return cardSystem;
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
        yield return StartCoroutine(SmoothMove(card, CardStartPosition, cardSystem));
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
 

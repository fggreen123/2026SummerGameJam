using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardDistribution: MonoBehaviour
{
    public GameObject Card;
    public GameObject CardWindow;
    public float speed = 5f;
    public Vector2 HandCenter = new Vector2(0f, -4f);
    public float HandSpacing = 1.8f;

    public List<GameObject> CurrentCardList = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(StartCardDistribute());
    }
    public void CardDistribute(Vector2 Locate)
    {
        GameObject CardClone = Instantiate(Card);
        CardClone.transform.position = new Vector3(0f, 10f, CardClone.transform.position.z);
        CardSystem cardSystem = InitializeCard(CardClone);
        cardSystem.MoveCoroutine = StartCoroutine(SmoothMove(CardClone, Locate, cardSystem));
    }
    IEnumerator StartCardDistribute()
    {

        Vector3 targetScale = CardWindow.transform.localScale;
        CardWindow.transform.localScale = Vector3.zero;
        StartCoroutine(SmoothScale(CardWindow.transform, targetScale, 2f));
        

        CardDistribute(new Vector2(-2.5f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(-7.5f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(2.5f, 0));
        yield return new WaitForSeconds(0.3f);
        CardDistribute(new Vector2(7.5f, 0));
    }

    public void CardSelected(GameObject selectedCard)
    {
        if (CurrentCardList.Contains(selectedCard))
        {
            return;
        }

        CurrentCardList.Add(selectedCard);

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

    private CardSystem InitializeCard(GameObject card)
    {
        CardSystem cardSystem = card.GetComponent<CardSystem>();
        cardSystem.Initialize(this);
        return cardSystem;
    }

    private IEnumerator SmoothMove(GameObject target, Vector2 location, CardSystem cardSystem)
    {
        Vector3 targetPosition = new Vector3(location.x, location.y, target.transform.position.z);

        while (Vector2.Distance(target.transform.position, targetPosition) > 0.01f)
        {
            target.transform.position = Vector2.Lerp(
                target.transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            yield return null;
        }

        target.transform.position = targetPosition;
        cardSystem.MoveCoroutine = null;
    }

    private IEnumerator SmoothScale(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            target.localScale = Vector3.Lerp(startScale, targetScale, t / duration);
            yield return null;
        }

        target.localScale = targetScale;
    }
}
 

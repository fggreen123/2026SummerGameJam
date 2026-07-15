using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardInformationTextSystem : MonoBehaviour
{
    [SerializeField] private TMP_Text CardType;
    [SerializeField] private TMP_Text CardEffect;

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        canvasRect = (RectTransform)canvas.transform;

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "CardType")
            {
                CardType = text;
            }
            else if (CardEffect == null)
            {
                CardEffect = text;
            }
        }
    }

    private void Update()
    {
        MoveToMouse();
    }

    public void Show(CardSuit suit, bool alignRight)
    {
        gameObject.SetActive(true);
        rectTransform.pivot = new Vector2(alignRight ? 1f : 0f, 1f);
        MoveToMouse();

        switch (suit)
        {
            case CardSuit.Spade:
                SetText(
                    "스페이드",
                    "카드 숫자만큼 사용한 대상의 체력을 감소시킵니다. 만약 J,Q,K일 경우 각각 대상의 체력을 50%, 60%, 70% 감소시킵니다.",
                    Color.yellow
                );
                break;
            case CardSuit.Club:
                SetText(
                    "클로버",
                    "카드 숫자만큼 사용한 대상의 공격력을 10초동안 감소시킵니다. 만약 J,Q,K일 경우 각각 대상의 공격력을 10초동안 10%, 20%, 30% 감소시킵니다.",
                    Color.green
                );
                break;
            case CardSuit.Heart:
                SetText(
                    "하트",
                    "카드 숫자만큼 사용한 대상의 체력을 회복시킵니다. 만약 J,Q,K일 경우 각각 대상의 hp 추가 체력을 30%, 60%, 100% 증가시킵니다.",
                    Color.red
                );
                break;
            case CardSuit.Diamond:
                SetText(
                    "다이아몬드",
                    "카드 숫자만큼 사용한 대상의 공격력을 10초동안 증가시킵니다. 만약 J,Q,K일 경우 각각 대상의 공격력이 10초동안 2배, 3배, 4배가 됩니다.",
                    Color.blue
                );
                break;
            case CardSuit.Joker:
                SetText(
                    "조커",
                    "적에게만 대상으로 사용할 수 있으며, 즉시 사용한 대상의 체력을 1~99%의 랜덤한 hp만큼 감소시킵니다.",
                    new Color(1f, 0.5f, 0f)
                );
                break;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetText(string type, string effect, Color color)
    {
        CardType.text = type;
        CardType.color = color;
        CardEffect.text = effect;
    }

    private void MoveToMouse()
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            Mouse.current.position.ReadValue(),
            canvas.worldCamera,
            out Vector3 mousePosition
        );

        rectTransform.position = mousePosition;
    }
}

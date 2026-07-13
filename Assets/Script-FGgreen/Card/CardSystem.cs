using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(BoxCollider2D))]
public class CardSystem : MonoBehaviour
{
    private CardDistribution cardDistribution;
    private Button button;
    private bool selected;

    public Coroutine MoveCoroutine { get; set; }

    public void Initialize(CardDistribution distribution)
    {
        cardDistribution = distribution;
        RegisterButtonClick();
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    private void Awake()
    {
        RegisterButtonClick();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(Select);
    }

    private void OnMouseDown()
    {
        Select();
    }

    private void RegisterButtonClick()
    {
        button = GetComponent<Button>();
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
    }
}

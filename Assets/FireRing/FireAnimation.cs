using Unity.VisualScripting;
using UnityEngine;

//파이어링 애니메이션

public class FireAnimation : MonoBehaviour
{
    [UnitHeaderInspectable("Fire images                                                                             ")]
    public Sprite[] sprite;

    [Header("image change speed")]
    public float changeSpeed = 0.15f;

    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex = 0;
    private float timer = 0f;

    private Vector3 initialLocalPosition;
    private bool isFirstFrame = true;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        initialLocalPosition = transform.localPosition;
        spriteRenderer.sprite = sprite[0];
        
    }

    
    void Update()
    {
        if (sprite.Length == 0 || spriteRenderer == null) return;

        timer += Time.deltaTime;

        if (timer >= changeSpeed)
        {
            timer = 0f;

            currentSpriteIndex = (currentSpriteIndex + 1) % sprite.Length;

            spriteRenderer.sprite = sprite[currentSpriteIndex];

            SetRendererToBottomPivot();
        }
    }

    private void SetRendererToBottomPivot()
    {
        if (spriteRenderer == null) return;

        float currentHeight = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;

        float baseHeight = sprite[0].rect.height / sprite[0].pixelsPerUnit;

        float yOffset = (currentHeight - baseHeight) / 2f;

        transform.localPosition = new Vector3(initialLocalPosition.x, initialLocalPosition.y + yOffset, initialLocalPosition.z);
    }
}

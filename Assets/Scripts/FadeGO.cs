using System.Collections;
using UnityEngine;

public class FadeGO : MonoBehaviour
{
    public CanvasGroup gameOverCanvasGroup;

    public float fadeDuration = 1.0f;

    private void OnEnable()
    {
        if (gameOverCanvasGroup != null)
        {
            StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float currentTime = 0f;

        gameOverCanvasGroup.alpha = 0f;

        while (currentTime < fadeDuration)
        {
            currentTime+= Time.deltaTime;

            gameOverCanvasGroup.alpha = Mathf.Lerp(0f, 1f, currentTime / fadeDuration);

            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f;
    }
}

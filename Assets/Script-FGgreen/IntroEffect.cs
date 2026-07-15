using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroEffect : MonoBehaviour
{

    private void Start()
    {
        StartCoroutine(FadeInOut(gameObject));
    }
    IEnumerator FadeInOut(GameObject target)
    {
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();

        for (float a = 0; a < 1; a += Time.deltaTime)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
            yield return null;
        }

        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1);

        yield return new WaitForSeconds(1f);

        for (float a = 1; a > 0; a -= Time.deltaTime)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
            yield return null;
        }

        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0);
        SceneManager.LoadScene("TitleScene");
    }
    
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShinyEffect : MonoBehaviour
{
    [Header("References")]
    public RectTransform sheenRect; // The Image moving across (Child of the Mask)
    
    [Header("Animation Settings")]
    public float speed = 1.5f;
    public float pauseDuration = 3.0f; // Delay between shines
    public float startPos = -150f;     // Left side of card
    public float endPos = 150f;        // Right side of card

    private void OnEnable()
    {
        if (sheenRect != null) StartCoroutine(ShineLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator ShineLoop()
    {
        while (true)
        {
            // 1. Reset Position
            sheenRect.anchoredPosition = new Vector2(startPos, sheenRect.anchoredPosition.y);
            
            // 2. Wait
            yield return new WaitForSeconds(pauseDuration);

            // 3. Animate Move
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                float x = Mathf.Lerp(startPos, endPos, t);
                sheenRect.anchoredPosition = new Vector2(x, sheenRect.anchoredPosition.y);
                yield return null;
            }
        }
    }
}
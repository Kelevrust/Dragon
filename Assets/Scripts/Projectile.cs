using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour
{
    public void Initialize(Vector3 startPos, Vector3 endPos, float duration)
    {
        transform.position = startPos;
        
        // Calculate rotation to face target
        Vector3 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        StartCoroutine(MoveRoutine(startPos, endPos, duration));
    }

    private IEnumerator MoveRoutine(Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Linear movement (can be changed to curved later)
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = end;
        Destroy(gameObject);
    }
}
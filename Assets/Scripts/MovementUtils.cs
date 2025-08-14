using System.Collections;
using UnityEngine;

public static class MovementUtils
{
    // precondition: direction is a normalized vector
    public static IEnumerator TimedMovement(Rigidbody rb, Vector3 direction, float distance, float speed, System.Action onComplete = null)
    { // IEnumerator executes actions across multiple frames
        Vector3 start = rb.position;
        Vector3 end = start + direction * distance;
        float elapsed = 0f;
        float duration = distance / speed;

        RaycastHit hit;
        if (Physics.Raycast(start, direction, out hit, distance))
        {
            MovableBlock block = hit.collider.GetComponent<MovableBlock>();
            if (block != null)
            {
                block.Push(direction);
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rb.MovePosition(Vector3.Lerp(start, end, elapsed / duration));
            yield return null; // advances to next frame
        }

        rb.MovePosition(end); // ensures the final position of the block is in the correct place
        onComplete?.Invoke(); // invokes optional callback function
    }
}
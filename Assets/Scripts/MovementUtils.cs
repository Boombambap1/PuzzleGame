using System.Collections;
using UnityEngine;

public static class MovementUtils
{
    const float SMOOTH_TIME = 0.08f; // used to make movement more smooth

    // precondition: direction is a normalized vector
    public static IEnumerator TimedMovement(Rigidbody rb, Vector3 direction,
    float distance, float speed, System.Action onComplete = null)
    { // IEnumerator executes actions across multiple frames
        Vector3 start = rb.position;
        Vector3 end = start + direction * distance;
        // float elapsed = 0f;
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

        Vector3 currentVelocity = Vector3.zero; // needed for SmoothDamp
        Vector3 currentPos = start;

        while (Vector3.Distance(currentPos, end) > distance / 50f)
        {
            currentPos = Vector3.SmoothDamp(currentPos, end, ref currentVelocity, SMOOTH_TIME);
            rb.MovePosition(currentPos);
            yield return null; // advances to next frame
        }

        rb.MovePosition(end); // snaps to final position when close enough
        onComplete?.Invoke(); // invokes optional callback function
    }
}
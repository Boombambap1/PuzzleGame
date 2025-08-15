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
        float duration = distance / speed;

        // foreach (Transform child in rb.transform)
        // {
        //     if (child.GetComponent<Properties>() == null) continue;
        //     Debug.Log("child: " + child.gameObject.name + ", parent: " + rb.gameObject.name);
        //     if (child.GetComponent<Properties>().TryMoveWithParent(direction, distance))
        //     {
        //         child.GetComponent<MovableBlock>().Push(direction);
        //     }
        // }

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
            Vector3 newPos = Vector3.SmoothDamp(currentPos, end, ref currentVelocity, SMOOTH_TIME);
            Vector3 frameOffset = newPos - currentPos;
            currentPos = newPos;
            rb.MovePosition(currentPos);

            // MoveChildren(rb.transform, frameOffset);

            yield return null; // advances to next frame
        }

        rb.MovePosition(end); // snaps to final position when close enough
        onComplete?.Invoke(); // invokes optional callback function
    }

    private static void MoveChildren(Transform parent, Vector3 offset)
    {
        foreach (Transform child in parent)
        {
            if (child.GetComponent<Properties>() == null) continue;
            child.position += offset;
            MoveChildren(child, offset);
        }
    }
}
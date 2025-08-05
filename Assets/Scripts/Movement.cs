using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    // Start is called before the first frame update
    public float speed;
    public float moveDistance;
    private Rigidbody rb;
    private bool isMoving = false;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetMouseButton(1) && (!Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))){
            if (isMoving) return;

            Vector3 direction = Vector3.zero;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                direction = Vector3.forward;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                direction = Vector3.back;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                direction = Vector3.left;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                direction = Vector3.right;

            if (direction != Vector3.zero)
            {
                StartCoroutine(TimedMovement(direction));
            }
        }
    }
    IEnumerator TimedMovement(Vector3 direction){
        isMoving = true;
        Vector3 start = rb.position;
        Vector3 end = start + direction * moveDistance;
        float elapsed = 0f;
        float duration = moveDistance / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 nextPosition = Vector3.Lerp(start, end, elapsed / duration);
            rb.MovePosition(nextPosition);
            yield return null;
        }

        rb.MovePosition(end);
        isMoving = false;
    }
}

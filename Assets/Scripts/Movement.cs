using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 5f;
    public float moveDistance = 1f;
    public Transform cameraTransform; // assign your camera here

    public enum SnapMode { None, Nearest, Floor, Ceil }
    public SnapMode snapMode = SnapMode.Nearest;

    public bool useWorldAxes = false;    // true = W always moves world-forward (Z), ignores camera
    public bool blockMultipleKeys = true; // prevent diagonal from multiple key presses

    private Rigidbody rb;
    private bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Only move when not holding right mouse and not holding either Shift
        if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;

        if (isMoving) return;

        bool w = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool s = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        bool a = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool d = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

        int pressed = (w?1:0) + (s?1:0) + (a?1:0) + (d?1:0);
        if (pressed == 0) return;

        // Prevent diagonal by choosing one key when multiple are pressed (priority: W > S > A > D).
        if (blockMultipleKeys && pressed > 1)
        {
            if (w) { s = a = d = false; }
            else if (s) { a = d = false; }
            else if (a) { d = false; }
        }

        Vector3 dir = Vector3.zero;
        float baseYaw;

        if (useWorldAxes)
        {
            baseYaw = 0f; // world-forward = 0°
        }
        else
        {
            float cameraYaw = cameraTransform ? cameraTransform.eulerAngles.y : 0f;
            baseYaw = (snapMode == SnapMode.None) ? cameraYaw : SnapAngle(cameraYaw);
        }

        if (w) dir = Quaternion.Euler(0f, baseYaw + 0f, 0f) * Vector3.forward;
        if (s) dir = Quaternion.Euler(0f, baseYaw + 180f, 0f) * Vector3.forward;
        if (a) dir = Quaternion.Euler(0f, baseYaw - 90f, 0f) * Vector3.forward;
        if (d) dir = Quaternion.Euler(0f, baseYaw + 90f, 0f) * Vector3.forward;

        dir.y = 0f;
        dir.Normalize();

        StartCoroutine(TimedMovement(dir));
    }

    float SnapAngle(float angle)
    {
        float unit = 90f;
        float ratio = angle / unit;
        float snappedRatio = 0f;
        switch (snapMode)
        {
            case SnapMode.Nearest: snappedRatio = Mathf.Round(ratio); break;
            case SnapMode.Floor:   snappedRatio = Mathf.Floor(ratio); break;
            case SnapMode.Ceil:    snappedRatio = Mathf.Ceil(ratio); break;
            default: snappedRatio = ratio; break;
        }
        return snappedRatio * unit;
    }

    IEnumerator TimedMovement(Vector3 direction)
    {
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




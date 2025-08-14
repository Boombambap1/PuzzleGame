using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float lookSensitivity = 1.5f;
    public float maxLookAngle = 90f;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y; // rotation about the vertical axis (turning left and right)
        pitch = angles.x; // rotation about the horizontal axis (looking up and down)
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetMouseButton(1))
        {
            HandleLook();
            HandleMovement();
        }
    }

    void HandleLook() // NOTE: this entire function is not needed if we're doing orthographic perspective
    {
        if (Input.GetMouseButton(1)) // right mouse button
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        );

        Vector3 move = transform.TransformDirection(input).normalized;
        transform.position += moveSpeed * Time.deltaTime * move;
    }
}

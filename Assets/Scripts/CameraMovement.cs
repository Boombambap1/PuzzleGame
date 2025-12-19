using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSensitivity = 1.5f;
    public float maxLookAngle = 90f;

    public Vector3 cameraSpawn = new Vector3(-6, 15, -20);
    public Vector3 cameraAngle = new Vector3(30, 15, 0);
    public float fov = 20f;
    private float pitch = 30f; // make sure to change this and yaw corresponding to the cameraAngle
    private float yaw = 15f;

    public float zoomSpeed = 10f;
    public float minHeight = 8f;
    public float maxHeight = 20f;

    void Start()
    {
        transform.rotation = Quaternion.Euler(cameraAngle);

        Camera cam = GetComponent<Camera>();
        if (cam != null)
            cam.fieldOfView = fov;

        transform.position = cameraSpawn;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleZoom();
    }

    void HandleMovement()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            return;

        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        );

        if (input.sqrMagnitude == 0f)
            return;

        // Move relative to camera's rotation
        Vector3 moveDirection = transform.TransformDirection(input);
        moveDirection.y = 0;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    void HandleLook()
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

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll == 0f)
            return;

        Vector3 move = transform.forward * scroll * zoomSpeed;
        Vector3 nextPos = transform.position + move;

        if (nextPos.y < minHeight || nextPos.y > maxHeight)
            return;

        transform.position = nextPos;
    }
}

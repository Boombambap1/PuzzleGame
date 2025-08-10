using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerMovement : MonoBehaviour
{
    public float moveSpeed;
    public float lookSensitivity;
    public float maxLookAngle;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift)|| Input.GetKey(KeyCode.RightShift)|| Input.GetMouseButton(1)){
            HandleLook();
            HandleMovement();
        }
    }

    void HandleLook()
    {
        if (Input.GetMouseButton(1)) 
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

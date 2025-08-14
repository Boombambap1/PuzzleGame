using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovableBlock : MonoBehaviour
{
    public float speed = 5f;
    public float moveDistance = 1f;
    public bool IsMoving { get; private set; } = false;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Push(Vector3 direction)
    {
        if (!IsMoving)
        {
            IsMoving = true;
            StartCoroutine(MovementUtils.TimedMovement(rb, direction, moveDistance, speed, () => IsMoving = false));
        }
    }
}
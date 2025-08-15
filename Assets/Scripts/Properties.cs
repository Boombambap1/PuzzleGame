using UnityEngine;

public class Properties : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1f, 0f); // height offset above parent
    private Rigidbody rb;
    private Transform currentParent;
    private bool isStuck = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Unstick();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || !collision.gameObject.CompareTag("Stickable")) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Debug.Log("ray: " + rayOrigin);
        float rayLength = 1.5f;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength))
        {
            if (hit.collider.CompareTag("Stickable") && transform.gameObject != hit.collider.gameObject)
            {
                Debug.Log("ids: " + transform.gameObject.name + ": " + transform.GetInstanceID() + ", " + hit.collider.gameObject.name + ": " + hit.collider.transform.GetInstanceID());
                Transform parent = hit.collider.transform;
                Vector3 targetPos = parent.position + offset;

                if (/*!Physics.CheckBox(
                    targetPos,
                    GetComponent<Collider>().bounds.extents * 0.9f
                )*/ true)
                {
                    Debug.Log("they have passed the test");
                    StickTo(parent);

                    Physics.IgnoreCollision(
                        GetComponent<Collider>(),
                        parent.GetComponent<Collider>()
                    );
                }
            }
        }
    }

    public bool TryMoveWithParent(Vector3 parentMoveDelta, float distance)
    {
        return true;
        // Vector3 targetPos = transform.position + parentMoveDelta * distance;
        // // returns half the size of the collider, required for the code following it
        // Vector3 halfExtents = GetComponent<Collider>().bounds.extents * 0.8f;
        // // checks if future position of block overlaps with any colliders
        // if (!Physics.CheckBox(targetPos, halfExtents))
        //     return true;
        // Unstick();
        // Debug.Log("what is this bruh");
        // return false;
    }

    public void StickTo(Transform newParent)
    {
        isStuck = true;
        currentParent = newParent;
        transform.SetParent(newParent);
        transform.localPosition = offset;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Unstick()
    {
        isStuck = false;
        currentParent = null;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}

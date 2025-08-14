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

    void LateUpdate()
    {
        if (isStuck && currentParent != null)
        {
            transform.position = currentParent.position + offset;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isStuck && collision.gameObject.CompareTag("Stickable"))
        {
            // check all contact points
            foreach (ContactPoint contact in collision.contacts)
            {
                // if contact normal points mostly upward relative to this block (we landed on top of the other block)
                if (Vector3.Dot(contact.normal, Vector3.up) > 0.7f)
                {
                    StickTo(collision.transform);
                    Debug.Log("enter");
                    break;
                }
            }
        }
        else if (isStuck && collision.transform != currentParent)
        {
            Unstick();
        }
    }

    public void StickTo(Transform newParent)
    {
        isStuck = true;
        currentParent = newParent;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Unstick()
    {
        isStuck = false;
        currentParent = null;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}

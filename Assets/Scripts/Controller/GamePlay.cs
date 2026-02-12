using UnityEngine;

public class InputProcessing : MonoBehaviour
{
    [SerializeField] private GamePhysics gamePhysics;

    void Awake()
    {
        gamePhysics = new GamePhysics();  // Inject or create instance
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            gamePhysics.StartStep(Vector3Int.forward);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            gamePhysics.StartStep(Vector3Int.left);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            gamePhysics.StartStep(Vector3Int.back);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            gamePhysics.StartStep(Vector3Int.right);
        }
    }
}


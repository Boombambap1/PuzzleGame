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
            gamePhysics.StartStep(Direction.Forward);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            gamePhysics.StartStep(Direction.Left);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            gamePhysics.StartStep(Direction.Backward);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            gamePhysics.StartStep(Direction.Right);
        }
    }
}


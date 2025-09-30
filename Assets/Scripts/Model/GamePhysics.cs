using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePhysics : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void StartStep(Direction dir)
    {
        if (Direction.Forward == dir)
        {
            Debug.Log("Up");
        }
        else if (Direction.Backward == dir)
        {
            Debug.Log("Down");
        }
        else if (Direction.Left == dir)
        {
            Debug.Log("Left");
        }
        else if (Direction.Right == dir)
        {
            Debug.Log("Right");
        }
    }
    void step()
    {

    }
    void tick()
    {
        
    }
}

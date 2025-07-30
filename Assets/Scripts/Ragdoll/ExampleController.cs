using UnityEngine;

public class ExampleController : MonoBehaviour
{
    [SerializeField] private Enemy enemy;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            enemy.Kill();
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            enemy.StandUp();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            enemy.StartRun();
        }
    }
}

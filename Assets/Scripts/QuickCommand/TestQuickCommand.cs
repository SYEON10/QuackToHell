using UnityEngine;
using QuickCmd;
public class TestQuickCommand : MonoBehaviour
{
    [Command]
    public void PrintHello()
    {
        Debug.Log("Hello");
    }
}

using Unity.Netcode;
using UnityEngine;

public class SceneController : MonoBehaviour
{
    public void QLoadScene(string gotoScene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gotoScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}

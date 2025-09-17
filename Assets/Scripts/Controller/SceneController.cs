using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 로딩을 중앙에서 관리하는 컨트롤러
/// 모든 씬 전환은 이 클래스를 통해 처리
/// </summary>
public class SceneController : Singleton<SceneController>
{
    /// <summary>
    /// 네트워크 씬 로딩 (서버에서 호출)
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    public void LoadNetworkScene(string sceneName)
    {
        if (!DebugUtils.Assert(NetworkManager.Singleton.IsHost, "Only host can load network scenes", this))
            return;

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// 로컬 씬 로딩 (클라이언트에서 호출)
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    public void LoadLocalScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// 게임 씬으로 이동 (서버 RPC)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void LoadGameSceneServerRpc(string sceneName)
    {
        LoadNetworkScene(sceneName);
    }

    /// <summary>
    /// 로비 씬으로 이동
    /// </summary>
    public void LoadLobbyScene()
    {
        LoadLocalScene(GameScenes.Lobby);
    }

    /// <summary>
    /// 마을 씬으로 이동 (서버 RPC)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void LoadVillageSceneServerRpc()
    {
        LoadNetworkScene(GameScenes.Village);
    }

    /// <summary>
    /// 재판장 씬으로 이동 (서버 RPC)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void LoadCourtSceneServerRpc()
    {
        LoadNetworkScene(GameScenes.Court);
    }
}

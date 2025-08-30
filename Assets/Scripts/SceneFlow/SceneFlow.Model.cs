using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class SceneNames
{
    public const string Boot = "BootScene";
    public const string Village = "VillageScene";
    public const string Court = "CourtScene";
}

public interface ISceneService
{
    bool IsReady { get; }
    void SwitchScene(string sceneName);
}

/// Netcode 세션이면 서버를 통해 네트워크 씬 전환,
/// 아니면 로컬 씬 전환으로 처리하는 서비스

public class NgoSceneService : ISceneService
{
    public bool IsReady =>
        NetSceneController.Instance != null            // 네트워크 컨트롤러 준비됨
        || NetworkManager.Singleton == null;           // 아직 네트워크 시작 전(로컬 전환 OK)

    public void SwitchScene(string sceneName)
    {
        // 세션 중: 서버가 네트워크 씬 로드 → 전원 동기화
        if (NetSceneController.Instance != null)
        {
            NetSceneController.Instance.SwitchSceneFromUI(sceneName);
            return;
        }

        // 오프라인: 로컬 씬 로드
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class SceneSwitchPresenter
{
    private readonly ISceneService _service;
    public SceneSwitchPresenter(ISceneService service) { _service = service; }

    public bool CanSwitch() => _service.IsReady;
    public void RequestSwitch(string sceneName) => _service.SwitchScene(sceneName);
}

public class QuickNetPresenter
{
    private readonly ISceneService _service;
    public QuickNetPresenter(ISceneService service) { _service = service; }

    void ApplyTransport(string address, ushort port)
    {
        var utp = NetworkManager.Singleton?.NetworkConfig?.NetworkTransport as UnityTransport;
        if (utp != null) utp.SetConnectionData(address, port);
    }

    public void StartHost(string address, ushort port, string firstScene)
    {
        ApplyTransport(address, port);
        NetworkManager.Singleton.StartHost();
        _service.SwitchScene(firstScene); // 서버가 네트워크 로드
    }

    public void StartClient(string address, ushort port)
    {
        ApplyTransport(address, port);
        NetworkManager.Singleton.StartClient();
    }

    public void Shutdown() => NetworkManager.Singleton.Shutdown();
}

public class BootLoaderPresenter
{
    private readonly ISceneService _service;
    public BootLoaderPresenter(ISceneService service) { _service = service; }
    public void Jump(string nextScene) => _service.SwitchScene(nextScene);
}

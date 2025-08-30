using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Threading;

public class LobbyController : NetworkBehaviour
{
    #region 카드데이터 로드

    [Header("Google Sheets CSV URLs")]
    [SerializeField] string cardCsvUrl;     // Card_Table
    [SerializeField] string stringCsvUrl;   // String_Table
    [SerializeField] string resourceCsvUrl; // Resource_Table

    private bool isCardDataLoaded = false;
    private CancellationTokenSource _cancellationTokenSource;

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //호스트만 데이터 로드
        if (!IsHost)
        {
            return;
        }

        // DeckManager가 초기화될 때까지 대기
        while (DeckManager.Instance == null)
        {
            await Task.Yield();
        }

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // DeckManager를 통해 직접 CSV 데이터 로드
            await DeckManager.Instance.LoadCardDataFromCsv(cardCsvUrl, stringCsvUrl, resourceCsvUrl, _cancellationTokenSource.Token);
            
            // 데이터 로딩 완료까지 대기
            await DeckManager.Instance.WhenDataReadyAsync();
            
            isCardDataLoaded = true;
            Debug.Log("[LobbyController] Card data loaded successfully through DeckManager");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyController] Card data loading failed: {ex.Message}");
        }
    }

    void OnDestroy() 
    { 
        _cancellationTokenSource?.Cancel(); 
        _cancellationTokenSource?.Dispose(); 
    }
 
    #endregion

    #region 게임 버튼

    public void OnJoinAsClientButton()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 클라이언트로 세션에 참여
        NetworkManager.Singleton.StartClient();
    }

    public void OnJoinAsHostButton()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 호스트(서버+클라이언트)로 세션 생성 및 참여
        NetworkManager.Singleton.StartHost();
    }

    public void OnStartGameButton()
    {
        //호스트만 게임 시작 가능
        if (!IsHost)
        {
            Debug.LogError("Only the host can start the game!");
            return;
        }

        // 2명 미만이면 시작 못 함
        if (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
        {
            Debug.LogError("Need at least 2 players to start the game!");
            return;
        }
        
        if (!isCardDataLoaded)
        {
            Debug.LogError("Card data is not loaded!");
            return;
        }
       
        //본인 데이터가 모두 초기화되면, 씬 이동.
        LoadVillageSceneServerRpc();
    }





    private void OnClientConnected(ulong clientId)
    {

        // 자신의 클라이언트가 연결되었을 때만 플레이어 스폰
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // 이벤트 구독 해제 (한 번만 실행되도록)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

            PlayerSpawn();
        }


    }

    [ServerRpc]
    private void LoadVillageSceneServerRpc()
    {
        // 모든 클라이언트를 VillageScene으로 이동
        NetworkManager.Singleton.SceneManager.LoadScene("VillageScene", LoadSceneMode.Single);
    }

    private void PlayerSpawn()
    {
        PlayerFactoryManager playerFactory = FindAnyObjectByType<PlayerFactoryManager>();
        if (playerFactory != null)
        {
            playerFactory.SpawnPlayerServerRpc();
        }
        else
        {
            Debug.LogError("PlayerFactoryManager not found in the scene.");
        }
    }
    #endregion
}

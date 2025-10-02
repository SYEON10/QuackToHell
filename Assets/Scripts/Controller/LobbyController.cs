using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

public class LobbyController : NetworkBehaviour
{
    [SerializeField]
    private TMP_Dropdown colorDropdown;
    
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

        //버튼 이벤트 바인딩
        if (DebugUtils.AssertNotNull(colorDropdown, "colorDropdown", this))
        {
            colorDropdown.onValueChanged.AddListener(OnColorDropdownButton);
        }
        
        //역할부여 연출이벤트 바인딩
        GameManager.Instance.onRoleAssignDirectionEnd += LoadVillageSceneServerRpc;

        
        //호스트만 데이터 로드
        if (!IsHost)
        {
            return;
        }

        // DeckManager가 초기화될 때까지 대기
        while (!DebugUtils.AssertNotNull(DeckManager.Instance, "DeckManager.Instance", this))
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

    #region 색깔 선택 버튼


    public void OnColorDropdownButton(Int32 colorIndex)
    {
        PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).ChangeColorServerRpc(colorIndex, NetworkManager.Singleton.LocalClientId);
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
        if (NetworkManager.Singleton.ConnectedClientsList.Count < GameConstants.Network.MinPlayersToStart)
        {
            Debug.LogError("Need at least 2 players to start the game!");
            return;
        }
        
        if (!isCardDataLoaded)
        {
            Debug.LogError("Card data is not loaded!");
            return;
        }

        //플레이어 역할 부여
        AssignPlayerRolesServerRpc();
       
        //본인 데이터가 모두 초기화되면, 씬 이동 : 바인딩 콜백
        
    }


    [ServerRpc]
    private void AssignPlayerRolesServerRpc()
    {
        //모든 플레이어 가져오기
        PlayerPresenter[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers();
        int totalPlayers = allPlayers.Length;
        //농장 수 결정
        int farmerCount = GetFarmerCountByPlayerCount(totalPlayers);

        //플레이어 목록 섞기
        //TODO: 주석풀기
        MathUtils.ShuffleArray(allPlayers);

        //역할 부여
        for(int i=0;i<allPlayers.Length;i++){
            if(i<farmerCount){
                allPlayers[i].ChangeRole(PlayerJob.Farmer);
            }
            else{
                allPlayers[i].ChangeRole(PlayerJob.Animal);
            }
        }

        //연출 시퀀스 시작
        GameManager.Instance.StartRoleRevealSequenceClientRpc();
    }
    

    private int GetFarmerCountByPlayerCount(int playerCount){
        return playerCount switch
        {
            5 or 6 => 1,
            7 or 8 or 9 => 2,
            10 or 11 or 12 => UnityEngine.Random.Range(0, 2) == 0 ? 2 : 3, // 2 or 3 랜덤
            13 or 14 or 15 => 3,
            _ => 1 // 기본값
        };
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
        NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Village, LoadSceneMode.Single);
    }

    private void PlayerSpawn()
    {
        PlayerFactoryManager playerFactory = PlayerFactoryManager.Instance;
        if (DebugUtils.AssertNotNull(playerFactory, "PlayerFactoryManager", this))
        {
            playerFactory.SpawnPlayerServerRpc();
        }
    }
    #endregion
}

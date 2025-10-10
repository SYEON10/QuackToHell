using UnityEngine;
using static PlayerView;
using System;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;
using Unity.Services.Lobbies.Models;

/// <summary>
/// View와 Model 간 중개자 역할
/// - View의 이벤트를 받아서 Model에 전달
/// - Model의 데이터 변경을 받아서 View에 전달
/// - 외부 클래스는 Presenter를 통해 Player와 소통
/// </summary>
public class PlayerPresenter : NetworkBehaviour
{
    [Header("Components")]
    private PlayerModel playerModel;
    private PlayerView playerView;
    private RoleController _roleController;
    private PlayerInput playerInput;
    [Header("")]
    [SerializeField]    
    private GameObject corpsePrefab;
    [SerializeField] 
    private SpriteRenderer playerSpriteRenderer;

    [Header("UI")] 
    private InteractionHUDController interactionHUDController;

    // 외부 접근 제한 - 메시지 기반 인터페이스만 사용

    private void Start()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 이벤트 바인딩 (중개자 역할)
        BindEvents();
        
        // 초기 설정
        SetupInitialState();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            //내 오너캐릭터만 입력받기
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
        if(scene.name==GameScenes.Village){
            GameObject interactionHUD = GameObject.FindGameObjectWithTag(GameTags.UI_InteractionHUD);
            interactionHUDController = interactionHUD.GetComponent<InteractionHUDController>();
            interactionHUDController.InitializeInteractionHUDUI();
            playerView.InjectHUDController(interactionHUDController);
        }
    }

    private void PlayerView_OnPlayerExited()
    {
        //현재 역할 확인
        PlayerJob playerJob = GetPlayerJob();
        if(interactionHUDController!=null)
        {
            interactionHUDController.SetPlayerInteractionUI(playerJob, false);
        }
    }

    private void PlayerView_OnPlayerDetected(GameObject player)
    {
        //현재 역할 확인
        PlayerJob playerJob = GetPlayerJob();
        if(interactionHUDController!=null)
        {
            interactionHUDController.SetPlayerInteractionUI(playerJob, true);
        }
    }


    public override void OnDestroy()
    {
        UnbindEvents();
        
        base.OnDestroy();
    }

    /// <summary>
    /// 클라이언트 연결 시 캐시 무효화
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        PlayerHelperManager.Instance.InvalidateCache();
        Debug.Log($"[PlayerPresenter] Client {clientId} connected - Cache invalidated");
    }

    /// <summary>
    /// 클라이언트 연결 해제 시 캐시 무효화
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        PlayerHelperManager.Instance.InvalidateCache();
        Debug.Log($"[PlayerPresenter] Client {clientId} disconnected - Cache invalidated");
    }

    public void SetAllPlayerIgnoreMoveInput(bool value)
    {
        playerView.SetIgnoreAllPlayerMoveInputServerRpc(value);
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        playerModel = GetComponent<PlayerModel>();
        playerView = GetComponent<PlayerView>();
        _roleController = GetComponent<RoleController>();
        playerInput = GetComponent<PlayerInput>();
        
        DebugUtils.AssertComponent(playerModel, "PlayerModel", this);
        DebugUtils.AssertComponent(playerView, "PlayerView", this);
        DebugUtils.AssertComponent(_roleController, "RoleManager", this);
        DebugUtils.AssertComponent(playerInput, "PlayerInput", this);
    }
    
    /// <summary>
    /// 이벤트 바인딩 (중개자 역할)
    /// </summary>
    private void BindEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        SceneManager.sceneLoaded += OnSceneLoaded;

        // View -> Presenter -> Model
        playerView.OnMovementInput += HandleMovementInput;
        playerView.OnKillTryInput += HandleKillInput;
        playerView.OnInteractInput += HandleInteractInput;
        playerView.OnCorpseReported += HandleCorpseReported;
        playerView.OnVentTryInput += HandleVentInput;
        playerView.OnSavotageTryInput += HandleSavotageInput;

        // Model -> Presenter -> View
        playerModel.PlayerStatusData.OnValueChanged += HandleStatusChanged;
        playerModel.PlayerAppearanceData.OnValueChanged += HandleAppearanceChanged;
        playerModel.PlayerStateData.OnValueChanged += HandleStateChanged;
    
        // View -> Presenter
        playerView.onPlayerDetected += PlayerView_OnPlayerDetected;
        playerView.onPlayerExited += PlayerView_OnPlayerExited;
        playerView.OnObjectEntered += HandleObjectEntered;
        playerView.OnObjectExited += HandleObjectExited;

        //model -> presenter
        playerModel.PlayerTag.OnValueChanged += HandleTagChanged;
    }

    private void UnbindEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // static event는 null 체크 필요 없음
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (playerView != null)
        {
            playerView.OnMovementInput -= HandleMovementInput;
            playerView.OnKillTryInput -= HandleKillInput;
            playerView.OnInteractInput -= HandleInteractInput;
            playerView.OnCorpseReported -= HandleCorpseReported;
            playerView.OnVentTryInput -= HandleVentInput;
            playerView.OnSavotageTryInput -= HandleSavotageInput;
        }

        if (playerModel != null)
        {
            playerModel.PlayerStatusData.OnValueChanged -= HandleStatusChanged;
            playerModel.PlayerAppearanceData.OnValueChanged -= HandleAppearanceChanged;
            playerModel.PlayerStateData.OnValueChanged -= HandleStateChanged;
        }

        if (playerModel.PlayerTag != null)
        {
            playerModel.PlayerTag.OnValueChanged -= HandleTagChanged;
        }
    }

    private void HandleObjectEntered(Collider2D collision)
    {
        if(interactionHUDController == null) return;

        if (collision.CompareTag(GameTags.PlayerCorpse))
        {   
            if (IsOwner)
            {
                if(GetPlayerJob()==PlayerJob.Ghost){
                    return;
                }
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.CorpseReport);
            }
        }

        //상호작용 오브젝트 감지
        if (collision.CompareTag(GameTags.ConvocationOfTrial))
        {
            if (IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.ConvocationOfTrial);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.TrialConvocation);
            }
            
        }
        if(collision.CompareTag(GameTags.Vent)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.Vent);

                PlayerJob playerJob = GetPlayerJob(); // 현재 역할 확인
                
                if(playerJob == PlayerJob.Animal)
                {
                    // Animal: Interact 버튼 비활성화
                    interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Vent);
                }
                else if(playerJob == PlayerJob.Farmer)
                {
                    // Farmer: Interact 버튼 활성화
                    interactionHUDController.EnableButton(InteractionHUDController.ButtonName.Vent);
                }
            }
        }
        if(collision.CompareTag(GameTags.RareCardShop)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.RareCardShop);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.RareCardShop);
            }
        }
        if(collision.CompareTag(GameTags.Exit)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.Exit);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.Exit);
            }
        }
        if(collision.CompareTag(GameTags.MiniGame)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.MiniGame);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.MiniGame);
            }
        }
        if(collision.CompareTag(GameTags.Teleport)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.Teleport);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.Teleport);
            }
        }
        
        
   }
    private void HandleObjectExited(Collider2D collision)
    {
        if(interactionHUDController == null) return;

        if (collision.CompareTag(GameTags.PlayerCorpse))
        {
            if (IsOwner)
            {
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.CorpseReport);
            }
        }
        
        //상호작용 오브젝트 종류에서 Trigger Exit되면, 기본 상호작용 버튼 이미지로 변경
        //vent, rarecardshop, exit, minigame, teleport, convocationoftrial
        if(collision.CompareTag(GameTags.Vent))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Vent);
            }
        }
        if(collision.CompareTag(GameTags.RareCardShop))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.RareCardShop);
            }
        }
        if(collision.CompareTag(GameTags.Exit))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Exit);
            }
        }
        if(collision.CompareTag(GameTags.MiniGame))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.MiniGame);
            }
        }
        if(collision.CompareTag(GameTags.Teleport))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Teleport);
            }
            
        }
        if (collision.CompareTag(GameTags.ConvocationOfTrial))
        {
            if (IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.TrialConvocation);
            }
        }   
    }


    private void HandleTagChanged(FixedString64Bytes previousTag, FixedString64Bytes newTag)
    {
        gameObject.tag = newTag.ToString();
        Debug.Log($"Player tag changed from {previousTag} to {newTag}");
    }
    
    /// <summary>
    /// 초기 설정
    /// </summary>
    private void SetupInitialState()
    {
        // 초기 닉네임 설정
        playerView.UpdateNickname(playerModel.PlayerStatusData.Value.Nickname);
        
        // 초기 색상 설정
        HandleAppearanceChanged(playerModel.PlayerAppearanceData.Value, playerModel.PlayerAppearanceData.Value);
    }


    #region 이벤트 핸들러들 (중개자 역할)
    
    /// <summary>
    /// View에서 받은 움직임 입력을 Model에 전달
    /// </summary>
    private void HandleMovementInput(object sender, EventArgs e)
    {
        OnMovementInputEventArgs onMovementInputEventArgs = (OnMovementInputEventArgs)e;
        playerModel.MovePlayerServerRpc(onMovementInputEventArgs.XDirection, onMovementInputEventArgs.YDirection);
    }
    
    /// <summary>
    /// View에서 받은 킬 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleKillInput()
    {
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        
        _roleController.CurrentStrategy?.TryKill();
    }

    
    /// <summary>
    /// View에서 받은 상호작용 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleInteractInput()
    {
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        _roleController.CurrentStrategy?.TryInteract();
    }

    


    
    /// <summary>
    /// View에서 받은 시체 리포트를 RoleStrategy에 전달
    /// </summary>
    private void HandleCorpseReported(ulong reporterClientId)
    {
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;

        _roleController.CurrentStrategy?.TryReportCorpse();
    }
    
    /// <summary>
    /// View에서 받은 벤트 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleVentInput()
    {
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        _roleController.CurrentStrategy?.TryVent();
    }
    
    /// <summary>
    /// View에서 받은 사보타지 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleSavotageInput()
    {
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        _roleController.CurrentStrategy?.TrySabotage();
    }
    
    /// <summary>
    /// Model에서 받은 상태 변경을 View에 전달
    /// </summary>
    private void HandleStatusChanged(PlayerStatusData previousValue, PlayerStatusData newValue)
    {
        // 닉네임 업데이트
        if(newValue.nickname != previousValue.nickname){
            playerView.UpdateNickname(newValue.nickname);
        }
        
        // 역할 변경 감지
        if (previousValue.job != newValue.job)
        {
            _roleController?.ChangeRole(newValue.job);
        }
    }
    
    /// <summary>
    /// Model에서 받은 외관 변경을 View에 전달
    /// </summary>
    private void HandleAppearanceChanged(PlayerAppearanceData previousValue, PlayerAppearanceData newValue)
    {
        playerView.ChangeApprearence(newValue);
    }
    
    /// <summary>
    /// Model에서 받은 상태 변경 처리 (MVP 패턴 준수)
    /// </summary>
    private void HandleStateChanged(PlayerStateData previousValue, PlayerStateData newValue)
    {
        // 사망 상태로 변경되었을 때 가시성 업데이트
        if (previousValue.AliveState != newValue.AliveState && 
            newValue.AliveState == PlayerLivingState.Dead)
        {
            UpdateVisibilityForAllPlayers();
        }
    }
    
    #endregion

    #region 외부에서 호출 가능한 메서드들 (중개자 역할)
    
    public void ChangeRole(PlayerJob newRole){
        playerModel.ChangeRole(newRole);
    }


  
    public void TryVent()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.Vent))
            {
                VentController ventController = collider.GetComponent<VentController>();
                if (ventController != null)
                {
                    ventController.SpaceInput = true;
                    Debug.Log("Space 인풋 들어옴!");
                    return;
                }
            }
        }
    }


    /// <summary>
    /// 킬 시도 서버 RPC
    /// </summary>
    [ServerRpc]
    public void TryKillServerRpc()
    {
        // 서버 검증
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        
        if (_roleController.CurrentStrategy?.CanKill() != true)
        {
            Debug.LogWarning($"[Server] Player {OwnerClientId} cannot kill");
            return;
        }
        
        // 킬 범위 내의 플레이어 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.Player) && collider.gameObject != gameObject)
            {
                PlayerPresenter targetPlayer = collider.GetComponent<PlayerPresenter>();
                if (targetPlayer != null && targetPlayer.GetPlayerAliveState() == PlayerLivingState.Alive)
                {
                    if (targetPlayer.GetPlayerJob() != PlayerJob.Animal)
                    {
                        Debug.Log("Animal이 아니어서 못 죽임");
                        return;
                    }
                    // 대상 플레이어를 죽임
                    targetPlayer.HandlePlayerDeathServerRpc();
                    Debug.Log($"[Server] Player {OwnerClientId} killed Player {targetPlayer.OwnerClientId}");
                    return;
                }
            }
        }
        
        Debug.LogWarning($"[Server] No valid target found for Player {OwnerClientId}");
    }
    
    /// <summary>
    /// 상호작용 시도 서버 RPC
    /// </summary>
    [ServerRpc]
    public void TryInteractServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // 서버 검증
        DebugUtils.AssertNotNull(_roleController, "RoleManager", this);
        
        if (_roleController.CurrentStrategy?.CanInteract() != true)
        {
            Debug.LogWarning($"[Server] Player {OwnerClientId} cannot interact");
            return;
        }
        
        // 상호작용 범위 내의 오브젝트 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.Player))
            {
                continue;
            }
            // 기존 패턴: IInteractable 인터페이스 활용
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                interactable.Interact(gameObject);
                Debug.Log($"[Server] Player {OwnerClientId} interacted with {collider.name}");
                return;
            }
            
            // 태그별 직접 처리 (기존 방식 유지)
            if (collider.CompareTag(GameTags.Exit))
            {
                HandleExitInteraction(collider);
                return;
            }
            
            if (collider.CompareTag(GameTags.Teleport))
            {
                HandleTeleportInteraction(collider);
                return;
            }
            if (collider.CompareTag(GameTags.RareCardShop))
            {
                HandleRareCardShopInteraction(collider);
                return;
            }
            if (collider.CompareTag(GameTags.MiniGame))
            {
                HandleMiniGameInteraction(collider);
                return;
            }

            if (collider.CompareTag(GameTags.ConvocationOfTrial))
            {
                HandleTrialConvocationInteraction(collider);
                return;
            }

            if (collider.CompareTag(GameTags.Vent))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { serverRpcParams.Receive.SenderClientId }
                    }
                };
                HandleVentInteractionClientRpc(clientRpcParams);
                return;
            }
            
        }
        
        Debug.LogWarning($"[Server] No interactable object found for Player {OwnerClientId}");
    }

    /// <summary>
    /// 벤트 상호작용 처리 (농장주 전용)
    /// </summary>
    [ClientRpc]
    private void HandleVentInteractionClientRpc( ClientRpcParams  clientRpcParams)
    {
        // 역할 검증
        PlayerJob currentRole = GetPlayerJob();
        if (currentRole != PlayerJob.Farmer)
        {
            Debug.LogWarning($"[Server] Only Farmer can use vents. Current role: {currentRole}");
            return;
        }
        
        _roleController.CurrentStrategy?.TryVent();
    }

    /// <summary>
    /// TODO: 출입구 상호작용 처리
    /// </summary>
    private void HandleExitInteraction(Collider2D exitCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used exit");
    }

    /// <summary>
    /// TODO: 텔레포트 상호작용 처리
    /// </summary>
    private void HandleTeleportInteraction(Collider2D teleportCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used teleport");
        
    }

    /// <summary>
    /// TODO: 희귀카드상점 상호작용 처리
    /// </summary>
    private void HandleRareCardShopInteraction(Collider2D shopCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used rare card shop");
    }

    /// <summary>
    /// 미니게임 상호작용 처리
    /// </summary>
    private void HandleMiniGameInteraction(Collider2D miniGameCollider)
    {
        MinigameController minigameController = miniGameCollider.GetComponent<MinigameController>();
        if (minigameController != null)
        {
            minigameController.OpenUi(); 
        }
    }

    /// <summary>
    /// 재판소집 상호작용 처리
    /// </summary>
    private void HandleTrialConvocationInteraction(Collider2D trialCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used trial convocation");
        
        // 기존 재판소집 로직 활용
        ConvocationOfTrialController trialController = trialCollider.GetComponent<ConvocationOfTrialController>();
        if (trialController != null)
        {
            trialController.Interact(gameObject); 
            
        }
    }
    

    #endregion

    [ServerRpc]
    public void TryTrialServerRpc(ulong reporterClientId)
    {
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
    }


    /// <summary>
    /// 시체 리포트 서버 RPC (검증 포함)
    /// </summary>
    [ServerRpc]
    public void ReportCorpseServerRpc(ulong reporterClientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 1. 요청자가 실제로 이 플레이어의 소유자인지 검증
        if (OwnerClientId != requesterClientId)
        {
            ReportCorpseResultClientRpc(false, "Unauthorized request", requesterClientId);
            return;
        }
        
        // note cba0898: Assert 상황에서 코드 실행 사유 체크 필요. Assert가 아니라 일반적인 if문으로 변경하는게 맞아 보임.
        // 2. 서버에서 플레이어 상태 검증 (유령은 시체 리포트 불가)
        if (!DebugUtils.AssertNotNull(playerModel != null, "playerModel", this))
        {
            ReportCorpseResultClientRpc(false, "PlayerModel not found", requesterClientId);
            return;
        }
        
        if (playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            ReportCorpseResultClientRpc(false, "Ghost cannot report corpses", requesterClientId);
            return;
        }
        
        // 3. 서버에서 실제로 시체가 근처에 있는지 검증
        if (!IsCorpseNearby())
        {
            ReportCorpseResultClientRpc(false, "No corpse nearby", requesterClientId);
            return;
        }
        
        // 4. 모든 플레이어의 움직임 멈추기
        StopAllPlayerMovementServerRpc();
        
        // 5. 서버에게 처리해달라고 하기 (책임클래스는 TrialManager)
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
        
        // 6. 성공 결과 전달
        ReportCorpseResultClientRpc(true, "Corpse reported successfully", requesterClientId);
    }
    
    /// <summary>
    /// 시체 리포트 결과 클라이언트 RPC
    /// </summary>
    [ClientRpc]
    private void ReportCorpseResultClientRpc(bool success, string reason, ulong targetClientId)
    {
        if (IsOwner)
        {
            if (success)
            {
                // 성공 시 UI 업데이트 등
            }
            else
            {
                Debug.LogWarning($"Corpse report failed: {reason}");
            }
        }
    }
    
    /// <summary>
    /// 서버에서 시체가 근처에 있는지 검증
    /// </summary>
    private bool IsCorpseNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.PlayerCorpse))
            {
                return true;
            }
        }
        return false;
    }


    private void PlayerView_OnMovementInput(object sender, EventArgs e)
    {
        //이벤트 인자 캐스팅
        OnMovementInputEventArgs onMovementInputEventArgs = (OnMovementInputEventArgs)e;

        //model에게 방향 이벤트 전달
        playerModel.MovePlayerServerRpc(onMovementInputEventArgs.XDirection, onMovementInputEventArgs.YDirection);
    }
    
 

    /// <summary>
    /// 농장주 UI 표시
    /// </summary>
    public void ShowFarmerUI()
    {
    }

    /// <summary>
    /// 동물 UI 표시
    /// </summary>
    public void ShowAnimalUI()
    {
    }

    /// <summary>
    /// 플레이어 사망 처리 (죽은 플레이어의 presenter에서 실행)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HandlePlayerDeathServerRpc(ServerRpcParams rpcParams=default)
    {
        // 1. 서버에서 플레이어 상태 검증 (이미 죽었는지 확인)
        DebugUtils.AssertNotNull(playerModel, "playerModel", this);
        
        if (playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogWarning($"Server: Player {OwnerClientId} is already dead");
            return;
        }
        
        
        // 시체 프리팹 생성 (서버가 권위적 정보로 처리)
        CreateCorpseServerRpc(transform.position, playerModel.PlayerAppearanceData.Value.ColorIndex);
        
        // 죽은 플레이어에게만 유령 상태로 변경하라고 알림
        ChangeToGhostStateServerRpc();
        
        // 로컬: 유령 상태로 전환 (레이어..)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerModel.ClientId }
            }
        };
        ChangeToGhostStateClientRpc(clientRpcParams);
        
        //시각처리: Model의 State.OnValueChanged에서
       
    }


       


    /// <summary>
    /// 유령 상태로 전환 (서버전용함수)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeToGhostStateServerRpc()
    {
        DebugUtils.AssertNotNull(playerModel, "playerModel", this);

        PlayerStatusData statusData = playerModel.PlayerStatusData.Value;
        // 서버에서 속도 변경 (NetworkVariable로 동기화됨)
        statusData.moveSpeed = statusData.moveSpeed * GameConstants.Player.GhostSpeedMultiplier; // 유령 속도로 설정
        // 서버에서 job변경
        statusData.job =  PlayerJob.Ghost;
        playerModel.PlayerStatusData.Value = statusData;
        // 서버에서 상태 변경
        PlayerStateData newState = new PlayerStateData
        {
            AliveState = PlayerLivingState.Dead,
            AnimationState = playerModel.PlayerStateData.Value.AnimationState  // 기존 값 유지
        };
        playerModel.PlayerStateData.Value = newState;
        // 서버에서 태그 변경 (NetworkVariable로 동기화됨)
        playerModel.PlayerTag.Value = GameTags.PlayerGhost;
        // 서버에서 투명도 변경
        PlayerAppearanceData currentAppearanceData = playerModel.PlayerAppearanceData.Value;
        currentAppearanceData.AlphaValue = GameConstants.Player.GhostTransparency;
        playerModel.PlayerAppearanceData.Value = currentAppearanceData;
    }

    /// <summary>
    /// 로컬: 유령 상태로 전환 (레이어..)
    /// </summary>
    [ClientRpc]
    private void ChangeToGhostStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // 레이어 변경
        gameObject.layer = GameLayers.GetLayerIndex(GameLayers.PlayerGhost);
    }


    /// <summary>
    /// 시체 생성 (서버 RPC)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void CreateCorpseServerRpc(Vector3 position, int colorIndex)
    {
        DebugUtils.AssertNotNull(corpsePrefab, "CorpsePrefab", this);

        GameObject corpse = Instantiate(corpsePrefab, position, Quaternion.identity);
        // 시체를 네트워크에 스폰
        if (corpse.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            networkObject.Spawn();
            // NetworkVariable을 통해 색상 동기화 (모든 클라이언트에 자동 동기화)
            if (corpse.TryGetComponent<PlayerCorpse>(out PlayerCorpse playerCorpse))
            {
                PlayerAppearanceData appearanceData = new PlayerAppearanceData
                {
                    ColorIndex = colorIndex
                };
                playerCorpse.AppearanceData.Value = appearanceData;
            }
        }
        else
        {
            Debug.LogError("Corpse prefab doesn't have NetworkObject component!");
        }
        
    }


    /// <summary>
    /// 유령 UI 표시
    /// </summary>
    public void ShowGhostUI()
    {
        // 유령 전용 UI 세팅
        interactionHUDController.DisableButton(InteractionHUDController.ButtonName.CorpseReport);        
    }

    /// <summary>
    /// 다른 플레이어가 이 플레이어를 죽일 때 호출되는 메서드
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void KillPlayerServerRpc()
    {
        if (!IsServer) return;
        
        // 사망 처리
        HandlePlayerDeathServerRpc();
    }

    

    /// <summary>
    /// 모든 플레이어의 가시성 업데이트
    /// </summary>
    public void UpdateVisibilityForAllPlayers()
    {
        //죽은애의 오브젝트에서 실행되는 함수임.
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerPresenter playerPresenter =  PlayerHelperManager.Instance.GetPlayerPresenterByClientId(localClientId);
        PlayerLivingState localPlayerLivingState = playerPresenter.GetPlayerAliveState();
        PlayerPresenter[] players= PlayerHelperManager.Instance.GetAllPlayers();
        
        //내가 죽었는지 체크
        if (localPlayerLivingState == PlayerLivingState.Dead)
        {
            //플레이어 다 끌어와서, 모두 보이도록 하기
            foreach (var player in players)
            {
                player.SetPlayerVisibility(true);
            }
        }
        else//내가 살았으면
        {
            //플레이어 다 끌어와서, 죽은 플레이어만 안 보이게 하기
            foreach (var player in players)
            {
                if (player.playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
                {
                    player.SetPlayerVisibility(false);    
                    Debug.Log("죽은애발견 ");
                }
                else
                {
                    player.SetPlayerVisibility(true);    
                    Debug.Log("살은애발견 ");
                }
            }
        }
    }

 

  

    /// <summary>
    /// 플레이어 가시성 설정
    /// </summary>
    private void SetPlayerVisibility(bool visible)
    {
        // 모든 렌더러 컴포넌트 활성화/비활성화
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
        
        // 닉네임 텍스트도 함께 처리
        DebugUtils.AssertNotNull(playerView, "playerView", this);
        playerView.SetNicknameVisibility(visible);

        // 콜라이더는 항상 활성화 (충돌 감지용)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }

    /// <summary>
    /// 모든 플레이어의 움직임을 멈추는 서버 RPC
    /// </summary>
    [ServerRpc]
    private void StopAllPlayerMovementServerRpc()
    {
        // 모든 플레이어의 움직임 멈추기
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            DebugUtils.AssertNotNull(player, "PlayerView", this);
            player.SetIgnoreAllPlayerMoveInputServerRpc(true);
        }
    }

    #region 외부 인터페이스 (메시지 기반)

    public int GetGold()
    {
        return playerModel.PlayerStatusData.Value.gold;
    }
    public void OnOffNickname(bool onOff)
    {
        playerView.SetNicknameVisibility(onOff);
    }
    
    /// <summary>
    /// 플레이어 상태 변경 요청
    /// </summary>
    public void RequestStatusChange(PlayerStatusData newStatus)
    {
        // TODO: PlayerModel에 상태 변경 RPC 메서드 추가 필요
        Debug.Log($"[PlayerPresenter] RequestStatusChange: {newStatus.Nickname}");
    }
    
    /// <summary>
    /// 플레이어 외형 변경 요청
    /// </summary>
    public void RequestAppearanceChange(PlayerAppearanceData newAppearance)
    {
        // TODO: PlayerModel에 외형 변경 RPC 메서드 추가 필요
        Debug.Log($"[PlayerPresenter] RequestAppearanceChange: {newAppearance.ColorIndex}");
    }
    
    /// <summary>
    /// 플레이어 이동 요청
    /// </summary>
    public void RequestMovement(float xDirection, float yDirection)
    {
        if (playerModel != null)
        {
            playerModel.MovePlayerServerRpc((int)xDirection, (int)yDirection);
        }
    }
    
    /// <summary>
    /// 킬 시도 요청: 버튼전용
    /// </summary>
    public void RequestKill()
    {
        if (_roleController?.CurrentStrategy != null)
        {
            _roleController.CurrentStrategy.TryKill();
        }
    }
    
    /// <summary>
    /// 사보타지 시도 요청
    /// </summary>
    public void RequestSabotage()
    {
        if (_roleController?.CurrentStrategy != null)
        {
            _roleController.CurrentStrategy.TrySabotage();
        }
    }
    
    /// <summary>
    /// 상호작용 시도 요청: 버튼용
    /// </summary>
    public void RequestInteract()
    {
        if (_roleController?.CurrentStrategy != null)
        {
            _roleController.CurrentStrategy.TryInteract();
        }
    }
    
    /// <summary>
    /// 시체 리포트 요청
    /// </summary>
    public void RequestReportCorpse()
    {
        if (_roleController?.CurrentStrategy != null)
        {
            _roleController.CurrentStrategy.TryReportCorpse();
        }
    }
    
    /// <summary>
    /// 플레이어 생존 상태 조회
    /// </summary>
    public PlayerLivingState GetPlayerAliveState()
    {
        return playerModel.PlayerStateData.Value.AliveState;
    }
    
    /// <summary>
    /// 플레이어 닉네임 조회
    /// </summary>
    public string GetPlayerNickname()
    {
        return playerModel?.PlayerStatusData.Value.Nickname ?? "";
    }

    public int GetPlayerColorIndex()
    {
        return  playerModel?.PlayerAppearanceData.Value.ColorIndex ?? 0;
    }
    
    /// <summary>
    /// 플레이어 역할 조회
    /// </summary>
    public PlayerJob GetPlayerJob()
    {
        return playerModel?.PlayerStatusData.Value.job ?? PlayerJob.None;
    }

    public void ToggleReady(){
        if(!IsOwner) return;
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        ToggleReadyServerRpc(myClientId);
        
    }

    public bool IsReady(){
        return playerModel?.PlayerStatusData.Value.IsReady??false;
    }
    
    [ServerRpc]
    private void ToggleReadyServerRpc(ulong clientId)
    {
        PlayerPresenter requestPlayer = PlayerHelperManager.Instance.GetPlayerPresenterByClientId(clientId);
        PlayerStatusData statusDataCopy = requestPlayer.GetPlayerStatusData();
        statusDataCopy.IsReady = !statusDataCopy.IsReady;
        requestPlayer.playerModel.PlayerStatusData.Value = statusDataCopy;

    }
    public PlayerStatusData GetPlayerStatusData()
    {
        return playerModel.PlayerStatusData.Value;
    }

    public void SubscribeToPlayerReadyStatusChanges(NetworkVariable<PlayerStatusData>.OnValueChangedDelegate handler){
        playerModel.PlayerStatusData.OnValueChanged += handler;
        Debug.Log($"바인딩된 플레이어의 id는 {NetworkManager.Singleton.LocalClientId}");
        Debug.Log($"바인딩된 함수는 {handler.Method.Name}, 타겟 = {handler.Target}");
    }
    
    #endregion
}

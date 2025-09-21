using UnityEngine;
using static PlayerView;
using System;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;

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
    private RoleManager roleManager;
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

        // Start()에서 네트워크 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    
        //씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
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


    private void OnDestroy()
    {
        // 네트워크 이벤트 구독 해제
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
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
        roleManager = GetComponent<RoleManager>();
        playerInput = GetComponent<PlayerInput>();
        
        DebugUtils.AssertComponent(playerModel, "PlayerModel", this);
        DebugUtils.AssertComponent(playerView, "PlayerView", this);
        DebugUtils.AssertComponent(roleManager, "RoleManager", this);
        DebugUtils.AssertComponent(playerInput, "PlayerInput", this);
    }
    
    /// <summary>
    /// 이벤트 바인딩 (중개자 역할)
    /// </summary>
    private void BindEvents()
    {
        // View -> Presenter -> Model
        playerView.OnMovementInput += HandleMovementInput;
        playerView.OnKillTryInput += HandleKillInput;
        playerView.OnInteractInput += HandleInteractInput;
        playerView.OnCorpseReported += HandleCorpseReported;
        playerView.OnVentTryInput += HandleVentInput;

        // Model -> Presenter -> View
        playerModel.PlayerStatusData.OnValueChanged += HandleStatusChanged;
        playerModel.PlayerAppearanceData.OnValueChanged += HandleAppearanceChanged;
    
        // View -> Presenter
        playerView.onPlayerDetected += PlayerView_OnPlayerDetected;
        playerView.onPlayerExited += PlayerView_OnPlayerExited;
        playerView.OnObjectEntered += HandleObjectEntered;
        playerView.OnObjectExited += HandleObjectExited;

        //model -> presenter
        playerModel.PlayerTag.OnValueChanged += HandleTagChanged;
    }
    private void HandleObjectEntered(Collider2D collision)
    {
        if(interactionHUDController == null) return;
        
        if (collision.CompareTag(GameTags.PlayerCorpse))
        {   
            if (IsOwner)
            {
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
        if (!DebugUtils.AssertNotNull(roleManager, "RoleManager", this))
            return;
        
        roleManager.CurrentStrategy?.TryKill();
    }

    
    /// <summary>
    /// View에서 받은 상호작용 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleInteractInput()
    {
        if (!DebugUtils.AssertNotNull(roleManager, "RoleManager", this))
            return;
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        roleManager.CurrentStrategy?.TryInteract();
    }

    


    
    /// <summary>
    /// View에서 받은 시체 리포트를 RoleStrategy에 전달
    /// </summary>
    private void HandleCorpseReported(ulong reporterClientId)
    {
        if (!DebugUtils.AssertNotNull(roleManager, "RoleManager", this))
            return;
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;

        roleManager.CurrentStrategy?.TryReportCorpse();
    }
    
    /// <summary>
    /// View에서 받은 벤트 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleVentInput()
    {
        if (!DebugUtils.AssertNotNull(roleManager, "RoleManager", this))
            return;
        if (GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        roleManager.CurrentStrategy?.TryVent();
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
            roleManager?.ChangeRole(newValue.job);
        }
    }
    
    /// <summary>
    /// Model에서 받은 외관 변경을 View에 전달
    /// </summary>
    private void HandleAppearanceChanged(PlayerAppearanceData previousValue, PlayerAppearanceData newValue)
    {
        // 색상 변경
        switch (newValue.ColorIndex)
        {
            case 0: playerSpriteRenderer.color = Color.red; break;
            case 1: playerSpriteRenderer.color = Color.orange; break;
            case 2: playerSpriteRenderer.color = Color.yellow; break;
            case 3: playerSpriteRenderer.color = Color.green; break;
            case 4: playerSpriteRenderer.color = Color.blue; break;
            case 5: playerSpriteRenderer.color = Color.purple; break;
        }
    }
    
    #endregion

    #region 외부에서 호출 가능한 메서드들 (중개자 역할)
    
    public void ChangeRole(PlayerJob newRole){
        playerModel.ChangeRole(newRole);
    }


    [ServerRpc]
    public void TryVentServerRpc()
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
        if (!DebugUtils.AssertNotNull(roleManager, "RoleManager", this))
            return;
        
        if (roleManager.CurrentStrategy?.CanKill() != true)
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
    public void TryInteractServerRpc()
    {
        // 서버 검증
        if (!DebugUtils.AssertNotNull(roleManager, "RoleManager", this))
            return;
        
        if (roleManager.CurrentStrategy?.CanInteract() != true)
        {
            Debug.LogWarning($"[Server] Player {OwnerClientId} cannot interact");
            return;
        }
        
        // 상호작용 범위 내의 오브젝트 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
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
        }
        
        Debug.LogWarning($"[Server] No interactable object found for Player {OwnerClientId}");
    }

    /// <summary>
    /// 벤트 상호작용 처리 (농장주 전용)
    /// </summary>
    private void HandleVentInteraction(Collider2D ventCollider)
    {
        // 역할 검증
        PlayerJob currentRole = GetPlayerJob();
        if (currentRole != PlayerJob.Farmer)
        {
            Debug.LogWarning($"[Server] Only Farmer can use vents. Current role: {currentRole}");
            return;
        }
        
        roleManager.CurrentStrategy?.TryVent();
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
        if (!IsOwner || OwnerClientId != requesterClientId)
        {
            ReportCorpseResultClientRpc(false, "Unauthorized request", requesterClientId);
            return;
        }
        
        // 2. 서버에서 플레이어 상태 검증 (유령은 시체 리포트 불가)
        if (!DebugUtils.AssertNotNull(playerModel, "playerModel", this))
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
    
    private void PlayerModel_OnColorChanged(Int32 colorIndex)
    {
        // 모든 클라이언트에서 색상 변경 적용
        switch (colorIndex)
        {
            case 0:
                playerSpriteRenderer.color = Color.red;
                break;
            case 1:
                playerSpriteRenderer.color = Color.orange;
                break;
            case 2:
                playerSpriteRenderer.color = Color.yellow;
                break;
            case 3:
                playerSpriteRenderer.color = Color.green;
                break;
            case 4:
                playerSpriteRenderer.color = Color.blue;
                break;
            case 5:
                playerSpriteRenderer.color = Color.purple;
                break;
        }   
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
    /// 플레이어 사망 처리 (서버에서만 실행)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HandlePlayerDeathServerRpc(ServerRpcParams rpcParams = default)
    {

        
        // 1. 서버에서 플레이어 상태 검증 (이미 죽었는지 확인)
        if (!DebugUtils.AssertNotNull(playerModel, "playerModel", this))
        {
            Debug.LogError("Server: PlayerModel not found");
            return;
        }
        
        if (playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogWarning($"Server: Player {OwnerClientId} is already dead");
            return;
        }
        
        // 2. 서버에서 권위적 정보로 상태 변경
        PlayerStateData currentState = playerModel.PlayerStateData.Value;
        currentState.AliveState = PlayerLivingState.Dead;
        playerModel.PlayerStateData.Value = currentState;
        
        
        // 3. 시체 프리팹 생성 (서버가 권위적 정보로 처리)
        CreateCorpseServerRpc(transform.position, playerModel.PlayerAppearanceData.Value.ColorIndex);
        
        // 4. 죽은 플레이어에게만 유령 상태로 변경하라고 알림
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };
        HandlePlayerDeathClientRpc(clientRpcParams);
        
        // 5. 모든 클라이언트에서 가시성 업데이트
        UpdateVisibilityForAllPlayersClientRpc();
    }

    /// <summary>
    /// 클라이언트에서 사망 처리 (죽은 플레이어에게만 전송)
    /// </summary>
    [ClientRpc]
    private void HandlePlayerDeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        ChangeToGhostStateServerRpc();
        // 이 RPC는 죽은 플레이어에게만 전송되므로 IsOwner 체크 불필요
        // 유령 역할로 변경
        roleManager?.ChangeRole(PlayerJob.Ghost);
    }

    /// <summary>
    /// 유령 상태로 전환 (서버에서 속도 변경, 죽은 플레이어에게만 시각적 효과)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeToGhostStateServerRpc()
    {
        
        if (DebugUtils.AssertNotNull(playerModel, "playerModel", this))
        {
            
            PlayerStatusData statusData = playerModel.PlayerStatusData.Value;
            // 서버에서 속도 변경 (NetworkVariable로 동기화됨)
            statusData.moveSpeed = statusData.moveSpeed * GameConstants.Player.GhostSpeedMultiplier; // 유령 속도로 설정
            playerModel.PlayerStatusData.Value = statusData;
            // 서버에서 태그 변경 (NetworkVariable로 동기화됨)
            playerModel.PlayerTag.Value = GameTags.PlayerGhost;
        }
        
        // 죽은 플레이어에게만 시각적 효과 적용 (OwnerClientId로 제한)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };
        ChangeToGhostVisualStateClientRpc(clientRpcParams);
    }

    /// <summary>
    /// 유령 시각적 상태로 전환 (클라이언트에서만 실행)
    /// </summary>
    [ClientRpc]
    private void ChangeToGhostVisualStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        ChangeToGhostVisualState();
    }

    /// <summary>
    /// 유령 시각적 상태로 전환 (반투명화, 태그 변경, 레이어 변경)
    /// </summary>
    public void ChangeToGhostVisualState()
    {
        // 레이어 변경
        gameObject.layer = GameLayers.GetLayerIndex(GameLayers.PlayerGhost);
        
        // 반투명 효과 
        SpriteRenderer[] spriteRenderer = GetComponentsInChildren<SpriteRenderer>();
        if (DebugUtils.AssertNotNull(spriteRenderer, "SpriteRenderer", this))
        {
            foreach (SpriteRenderer renderer in spriteRenderer)
            {
                Color color = renderer.color;
                color.a = GameConstants.Player.GhostTransparency; // 50% 투명도
                renderer.color = color;
            }
        }
    }

    /// <summary>
    /// 시체 생성 (서버 RPC)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void CreateCorpseServerRpc(Vector3 position, int colorIndex)
    {
        // 시체 프리팹을 Resources에서 로드
        if (!DebugUtils.AssertNotNull(corpsePrefab, "CorpsePrefab", this))
            return;

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
    /// 색상 인덱스에 따른 색상 반환
    /// </summary>
    private Color GetColorByIndex(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0: return Color.red;
            case 1: return Color.orange;
            case 2: return Color.yellow;
            case 3: return Color.green;
            case 4: return Color.blue;
            case 5: return Color.purple;
            default: return Color.white;
        }
    }

    /// <summary>
    /// 유령 UI 표시
    /// </summary>
    public void ShowGhostUI()
    {
        // 유령 전용 UI 활성화
        // TODO: 유령 전용 UI 구현
        
        // TODO: 시체 리포트 버튼 비활성화
        
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
    [ClientRpc]
    public void UpdateVisibilityForAllPlayersClientRpc()
    {
        // 모든 플레이어 찾기
        PlayerPresenter[] allPlayers = FindObjectsByType<PlayerPresenter>(FindObjectsSortMode.None);
        
        foreach (PlayerPresenter player in allPlayers)
        {
            if (player == null) continue;
            
            // 각 플레이어의 가시성 업데이트
            player.UpdatePlayerVisibility();
        }
    }

    /// <summary>
    /// 플레이어의 가시성 업데이트
    /// </summary>
    public void UpdatePlayerVisibility()
    {
        if (!DebugUtils.AssertNotNull(playerModel, "playerModel", this))
            return;
        
        bool isAlive = playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Alive;
        bool isLocalPlayer = IsOwner;
        
        // 로컬 플레이어인 경우
        if (isLocalPlayer)
        {
            // 로컬 플레이어는 항상 보임
            SetPlayerVisibility(true);
        }
        // 다른 플레이어인 경우
        else
        {
            // 로컬 플레이어가 유령이면 모든 사람이 보임
            // 로컬 플레이어가 산 사람이면 산 사람만 보임
            bool localPlayerIsGhost = GetLocalPlayerIsGhost();
            if (localPlayerIsGhost)
            {
                // 유령은 모든 사람을 볼 수 있음
                SetPlayerVisibility(true);
            }
            else
            {
                // 산 사람은 산 사람만 볼 수 있음
                PlayerJob targetPlayerJob = GetPlayerJob();
                bool targetIsGhost = (targetPlayerJob == PlayerJob.Ghost || !isAlive);
                SetPlayerVisibility(!targetIsGhost); // 유령이 아닌 경우만 보임
            }
        }
    }

    /// <summary>
    /// 로컬 플레이어가 유령인지 확인
    /// </summary>
    private bool GetLocalPlayerIsGhost()
    {
        PlayerPresenter localPlayer = GetLocalPlayer();
        if (localPlayer == null) return false;
        
        return localPlayer.GetPlayerAliveState() != PlayerLivingState.Alive;
    }

    /// <summary>
    /// 로컬 플레이어 찾기
    /// </summary>
    private PlayerPresenter GetLocalPlayer()
    {
        PlayerPresenter[] allPlayers = FindObjectsByType<PlayerPresenter>(FindObjectsSortMode.None);
        foreach (PlayerPresenter player in allPlayers)
        {
            if (player.IsOwner) return player;
        }
        return null;
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
        if (DebugUtils.AssertNotNull(playerView, "playerView", this))
        {
            playerView.SetNicknameVisibility(visible);
        }
        
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
            if (DebugUtils.AssertNotNull(player, "PlayerView", this))
            {
                player.SetIgnoreAllPlayerMoveInputServerRpc(true);
            }
        }
    }

    #region 외부 인터페이스 (메시지 기반)
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
    /// 킬 시도 요청
    /// </summary>
    public void RequestKill()
    {
        if (roleManager?.CurrentStrategy != null)
        {
            roleManager.CurrentStrategy.TryKill();
        }
    }
    
    /// <summary>
    /// 사보타지 시도 요청
    /// </summary>
    public void RequestSabotage()
    {
        if (roleManager?.CurrentStrategy != null)
        {
            roleManager.CurrentStrategy.TrySabotage();
        }
    }
    
    /// <summary>
    /// 상호작용 시도 요청
    /// </summary>
    public void RequestInteract()
    {
        if (roleManager?.CurrentStrategy != null)
        {
            roleManager.CurrentStrategy.TryInteract();
        }
    }
    
    /// <summary>
    /// 시체 리포트 요청
    /// </summary>
    public void RequestReportCorpse()
    {
        if (roleManager?.CurrentStrategy != null)
        {
            roleManager.CurrentStrategy.TryReportCorpse();
        }
    }
    
    /// <summary>
    /// 플레이어 생존 상태 조회
    /// </summary>
    public PlayerLivingState GetPlayerAliveState()
    {
        return playerModel?.PlayerStateData.Value.AliveState ?? PlayerLivingState.Alive;
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
    
    #endregion
}

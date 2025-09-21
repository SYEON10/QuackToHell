using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
/// <summary>
/// 시각 및 입력 처리 (완전 Input System 방식)
/// </summary>
public class PlayerView : NetworkBehaviour
{
    // PlayerView.cs에 추가
    [Header("Player Detection")]
    [SerializeField] private float playerDetectionRadius = 2f;
    private HashSet<GameObject> previouslyDetectedPlayers = new HashSet<GameObject>();

    public Action<GameObject> onPlayerDetected;
    public Action onPlayerExited;
    private Camera localCamera = null;

    private NetworkVariable<bool> ignoreMoveInput = new NetworkVariable<bool>(false);
    public bool IgnoreMoveInput
    {
        get { return ignoreMoveInput.Value; }
        set
        {
            if (!IsHost)
            {
                return;
            }
            ignoreMoveInput.Value = value;   
        }
    }

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;

    private InteractionHUDController interactionHUDController;

    protected void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
    }
    private void Update() {
        #region 플레이어감지
        // 소유자만 감지 실행
        if (!IsOwner) return;
        
        
        DetectNearbyPlayers();
        #endregion
    }
    private void DetectNearbyPlayers()
    {
        // 현재 감지된 플레이어들
        HashSet<GameObject> currentlyDetectedPlayers = new HashSet<GameObject>();
        
        // 범위 내 모든 콜라이더 검색
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, playerDetectionRadius);
        
        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.CompareTag(GameTags.Player) && collider.gameObject != gameObject)
            {
                GameObject detectedPlayer = collider.gameObject;
                currentlyDetectedPlayers.Add(detectedPlayer);
                
                // 새로 감지된 플레이어 (Enter 이벤트)
                if (!previouslyDetectedPlayers.Contains(detectedPlayer))
                {
                    onPlayerDetected?.Invoke(detectedPlayer);
                }
            }
        }
        
        // Exit 이벤트 처리 (필요한 경우)
        foreach (GameObject previousPlayer in previouslyDetectedPlayers)
        {
            if (!currentlyDetectedPlayers.Contains(previousPlayer))
            {
                onPlayerExited?.Invoke(); // 필요시 추가
            }
        }
        
        // 이전 상태 업데이트
        previouslyDetectedPlayers = currentlyDetectedPlayers;
    }


    
    protected void Start()
    {
        Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
        if (DebugUtils.AssertNotNull(canvas, "Canvas", this))
        {
            nicknameText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        if (IsOwner)
        {
            SetupLocalCamera();
        }
        
        SetupInputSystem();
    }
    
    private void SetupInputSystem()
    {
        if (!DebugUtils.AssertNotNull(playerInput, "PlayerInput", this))
            return;

        if (!DebugUtils.AssertNotNull(playerInput.actions, "PlayerInput.actions", this))
            return;

        InputAction moveAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Move}"];
        if (DebugUtils.AssertNotNull(moveAction, "MoveAction", this))
        {
            moveAction.performed += OnMoveInput;
            moveAction.canceled += OnMoveInput;
        }

        InputAction interactAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Interact}"];
        if (DebugUtils.AssertNotNull(interactAction, "InteractAction", this))
        {
            interactAction.performed += OnInteractInputHandler;
        }

        InputAction reportAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Report}"];
        if (DebugUtils.AssertNotNull(reportAction, "ReportAction", this))
        {
            reportAction.performed += OnReportInput;
        }

        InputAction killAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Kill}"];
        if (DebugUtils.AssertNotNull(killAction, "KillAction", this))
        {
            killAction.performed += OnKillInput;
        }
        // 벤트 입력 추가 (Space키)
        InputAction ventAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Vent}"];
        if (DebugUtils.AssertNotNull(ventAction, "VentAction", this))
        {
            ventAction.performed += OnVentInput;
        }

    }
    
    public override void OnDestroy()
    {
        if (DebugUtils.AssertNotNull(playerInput, "PlayerInput", this) && 
            DebugUtils.AssertNotNull(playerInput.actions, "PlayerInput.actions", this))
        {
            InputAction moveAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Move}"];
            if (DebugUtils.AssertNotNull(moveAction, "MoveAction", this))
            {
                moveAction.performed -= OnMoveInput;
                moveAction.canceled -= OnMoveInput;
            }

            InputAction interactAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Interact}"];
            if (DebugUtils.AssertNotNull(interactAction, "InteractAction", this))
            {
                interactAction.performed -= OnInteractInputHandler;
            }

            InputAction reportAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Report}"];
            if (DebugUtils.AssertNotNull(reportAction, "ReportAction", this))
            {
                reportAction.performed -= OnReportInput;
            }

            InputAction killAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Kill}"];
            if (DebugUtils.AssertNotNull(killAction, "KillAction", this))
            {
                killAction.performed -= OnKillInput;
            }

            
        }
        
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner)
        {
            // 씬 변경 시 카메라 초기화
            if (localCamera != null)
            {
                Destroy(localCamera.gameObject);
                localCamera = null;
                Debug.Log("[PlayerView] OnSceneLoaded - localCamera is null");
            }
            SetupLocalCamera();
        }
        if(scene.name == GameScenes.Village && IsOwner)
        {
            SetIgnoreAllPlayerMoveInputServerRpc(false);
        }
    }
    
    #region 카메라
 

    private void SetupLocalCamera()
    {
        // Owner인 플레이어만 카메라 설정
        if (!IsOwner) return;
        
        if (localCamera == null)
        {
            // Main Camera를 찾아서 플레이어에 붙이기
            GameObject mainCameraObj = Camera.main.gameObject;
            if (mainCameraObj != null)
            {
                localCamera = mainCameraObj.GetComponent<Camera>();
                mainCameraObj.transform.SetParent(transform);
                mainCameraObj.transform.localPosition = new Vector3(0, 0, -10);
                mainCameraObj.layer = LayerMask.NameToLayer("Player");
                mainCameraObj.SetActive(true);
                localCamera.enabled = true;
            }
        }
        //씬 내에서 Canvas인 오브젝트 모두 찾아서, 렌더모드가 Camera라면 내 카메라 넣어주기
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            canvas.worldCamera = localCamera;
        }
    }
    #endregion

    #region 닉네임
    [SerializeField]
    private TextMeshProUGUI nicknameText;
    
    public void UpdateNickname(string nickname)
    {
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
    }

    /// <summary>
    /// 닉네임 가시성 설정
    /// </summary>
    public void SetNicknameVisibility(bool visible)
    {
        if (nicknameText != null)
        {
            nicknameText.enabled = visible;
        }
    }
    
    #endregion

    #region 움직임 (Input System)
    public EventHandler OnMovementInput;

    public class OnMovementInputEventArgs: EventArgs{
        public int XDirection { get; private set; }
        public int YDirection { get; private set; }

        public OnMovementInputEventArgs(int inputXDirection, int inputYDirection)
        {
            XDirection = inputXDirection;
            YDirection = inputYDirection;
        }
    }

    // Input System을 사용한 이동 입력 처리
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (ignoreMoveInput.Value) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        
        // 정수로 변환하여 전송
        int xDirection = Mathf.RoundToInt(moveInput.x);
        int yDirection = Mathf.RoundToInt(moveInput.y);
        
        OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(xDirection, yDirection));
    }
    #endregion
    
    #region 움직임 제한
    [ServerRpc(RequireOwnership = false)]
    public void SetIgnoreAllPlayerMoveInputServerRpc(bool value)
    {
        //1. 모든 플레이어 객체 가져오기
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            if (player != null)
            {
                player.IgnoreMoveInput = value;
            }
        }
    }
    #endregion

    #region Kill (Input System) - Farmer만 가능
    public Action OnKillTryInput;

    // Input System을 사용한 Kill 입력 처리
    private void OnKillInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
       
        OnKillTryInput?.Invoke();
    }

    
    #endregion
    public Action OnVentTryInput;

    // 핸들러 메서드 구현
    private void OnVentInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        OnVentTryInput?.Invoke();
    }

    #region Interact (Input System) - 모든 사람 가능 (Ghost 제외)
    public Action OnInteractInput;
    
    // Input System을 사용한 Interact 입력 처리
    private void OnInteractInputHandler(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        OnInteractInput?.Invoke();
    }

   
    #endregion

    #region 시체발견 (LeftShift키) - 모든 사람 가능 (Ghost 제외)
    public Action<ulong> OnCorpseReported;

    

    // LeftShift키로 시체 리포트 처리
    private void OnReportInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        
        OnCorpseReported?.Invoke(OwnerClientId);
        
        
    }
    public void InjectHUDController(InteractionHUDController interactionHUDController)
    {
        this.interactionHUDController = interactionHUDController;
    }

    public Action<Collider2D> OnObjectEntered;
    public Action<Collider2D> OnObjectExited;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
    
        OnObjectEntered?.Invoke(collision); 
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;
    
        OnObjectExited?.Invoke(collision); 
    }
    #endregion
}

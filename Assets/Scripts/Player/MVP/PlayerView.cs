using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 시각 및 입력 처리
/// </summary>
public class PlayerView : NetworkBehaviour
{
    //TODO : 입력을 input action으로 관리
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

    //TODO: 역할마다 보이는 버튼 다르게 하도록 Button가서 구현&변수명바꾸기 = 텍스트 / 이미지 달라지게. & 역할마다 다른 기능 invoke하도록 다형성으로 구현
    private GameObject killButtonGameObject;
    private GameObject reportCorpseButtonGameObject;
    private void Start()
    {
        var canvas = gameObject.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            nicknameText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        }

        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (IsOwner)
        {
            SetupLocalCamera();
        }

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner)
        {
            SetupLocalCamera();
        }
        if(scene.name == "VillageScene" && IsOwner)
        {
            SetIgnoreMoveInputServerRpc(false);
            FindAndConnectKillButton();
            FindAndConnectReportCorpseButton();
        }
    }
    #region 카메라
    private Camera localCamera = null;

    private void SetupLocalCamera()
    {
        // 기존 메인 카메라 비활성화
        if (Camera.main != null) GameObject.Find("Main Camera").SetActive(false);

        // 로컬 카메라 생성 및 플레이어 하위로 설정
        if (localCamera == null)
        {
            GameObject cameraObj = new GameObject("LocalCamera");
            localCamera = cameraObj.AddComponent<Camera>();
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = new Vector3(0, 0, -10);
            cameraObj.layer = LayerMask.NameToLayer("Player");
            cameraObj.tag = "MainCamera";
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
    private TextMeshProUGUI nicknameText;
   
    //닉네임
    
    public void UpdateNickname(string nickname)
    {
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
    }
    #endregion

    #region 움직임

    //움직임
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

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        if (ignoreMoveInput.Value)
        {
            return;
        }

        int inputXDirection = 0;
        int inputYDirection = 0;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            inputYDirection = 1;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            inputYDirection = -1;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            inputXDirection = -1;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            inputXDirection = 1;
        }

        if (inputXDirection != 0f || inputYDirection != 0f)
        {
            OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(inputXDirection, inputYDirection));
        }
        else
        {
            OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(0, 0));
        }
    }
    #endregion
    #region 움직임 제한
    [ServerRpc]
    private void SetIgnoreMoveInputServerRpc(bool value)
    {
        //1. 모든 플레이어 객체 가져오기
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            player.IgnoreMoveInput = value;
        }
    }

    
    #endregion


    #region Kill
    public Action OnKillTryInput;

    private void FindAndConnectKillButton()
    {
        killButtonGameObject = GameObject.FindWithTag("KillButton");
        Button killButton = killButtonGameObject.GetComponent<Button>();
        killButton.onClick.AddListener(OnKillButtonDown);
    }

    /// <summary>
    /// 살해 버튼 눌렀을 때 호출: UI에서 버튼에 연결할 것.
    /// </summary>
    public void OnKillButtonDown()
    {
        if (!IsOwner)
        {
            return;
        }

        OnKillTryInput?.Invoke();
    }

    #endregion

    #region 시체발견
    public Action<ulong> OnCorpseReported;
    private void FindAndConnectReportCorpseButton()
    {
        reportCorpseButtonGameObject = GameObject.FindWithTag("ReportCorpseButton");
        Button reportCorpseButton = reportCorpseButtonGameObject.GetComponent<Button>();
        reportCorpseButton.onClick.AddListener(OnReportCorpseTriggered);
        reportCorpseButtonGameObject.SetActive(false);
    }

    public void OnReportCorpseTriggered()
    {
        if(!IsOwner)
        {
            return;
        }
        //시체발견 시그널 보내기
        //reporter, reported
        OnCorpseReported?.Invoke(OwnerClientId);
        Debug.Log("Report Corpse Button Down - View");
    }

    private GameObject corpseObj = null;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerCorpse"))
        {   
            //유령은 버튼 안 보이게
            if (gameObject.tag == "PlayerGhost")
            {
                return;
            }
            if (IsOwner)
            {
                reportCorpseButtonGameObject.SetActive(true);
                corpseObj = collision.gameObject;
            }
        }    
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerCorpse"))
        {
            //유령은 버튼 안 보이게
            if (gameObject.tag == "PlayerGhost")
            {
                return;
            }
            if (IsOwner)
            {
                reportCorpseButtonGameObject.SetActive(false);
                corpseObj = null;
            }
        }
    }

    #endregion
}

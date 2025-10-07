using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TrialManager : NetworkBehaviour
{
    [Header("UI References")]
    private GameObject convocationOfTrialCanvas;
    private GameObject convocationOfTrialPanel;
    private GameObject corpseTextObject;
    private Image reporterImage;
    //TODO:  하드코딩 개선

    private string reporterPlayerText = "Not_Set";
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    #region 싱글톤 코드
    //싱글톤 코드
    private static TrialManager _instance;
    public static TrialManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TrialManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TrialManager");
                    _instance = go.AddComponent<TrialManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion


    [ServerRpc(RequireOwnership = false)]
    public void TryTrialServerRpc(ulong reporterClientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 1. 서버에서 리포터 클라이언트 ID 검증
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(reporterClientId))
        {
            Debug.LogError($"Server: Reporter client {reporterClientId} not found in connected clients");
            return;
        }
        
        // 2. 서버에서 리포터가 실제로 살아있는 플레이어인지 검증
        PlayerModel reporterModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterClientId);
        DebugUtils.AssertNotNull(reporterModel, "ReporterModel", this);
        
        if (reporterModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogError($"Server: Dead player {reporterClientId} cannot start trial");
            return;
        }
        
        // 3. 서버에서 이미 재판이 진행 중인지 검증
        if (convocationOfTrialPanel != null && convocationOfTrialPanel.activeInHierarchy)
        {
            Debug.LogWarning($"Server: Trial already in progress, ignoring request from {reporterClientId}");
            return;
        }
        
        
        // 4. 재판 시작 (서버가 권위적 정보로 처리)
        TrialResultClientRpc(reporterClientId);
    }

    [ClientRpc]
    public void TrialResultClientRpc(ulong reporterClientId)
    {
        convocationOfTrialPanel.SetActive(true);
        
        InjectReporterColor(reporterClientId);
        InjectReporterPlayerText(reporterClientId);
          
        //모든 플레이어의 움직임 멈춤
        PlayerHelperManager.Instance.StopAllPlayerServerRpc();
        //5초뒤 씬 이동
        Invoke("LoadCourtScene", 5f);
    }

    private void LoadCourtScene()
    {
        if (!IsHost)
        {
            return;
        }

        //재판장 씬으로 이동
        NetworkManager.Singleton.SceneManager.LoadScene(GameScenes.Court, LoadSceneMode.Single);
    }
    private void InjectReporterPlayerText(ulong reporterCliendId)
    {
        PlayerModel reporterModel =  PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterCliendId);
        reporterPlayerText = reporterModel.PlayerStatusData.Value.Nickname.ToString();
        TextMeshProUGUI reporterTextTMP = corpseTextObject.GetComponent<TextMeshProUGUI>();
        reporterTextTMP.text = "ReporterPlayer: " + reporterPlayerText;
    }
    private void InjectReporterColor(ulong reporterClientId)
    {
        PlayerModel reporterModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(reporterClientId);
        PlayerAppearanceData playerAppearanceData = reporterModel.PlayerAppearanceData.Value;
        int colorIndex = playerAppearanceData.ColorIndex;
        reporterImage.color = ColorUtils.GetColorByIndex(colorIndex);               
        
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameScenes.Village)
        {
            convocationOfTrialCanvas = GameObject.FindGameObjectWithTag(GameTags.UI_ConvocationOfTrialCanvas);
            if (DebugUtils.EnsureNotNull(convocationOfTrialCanvas, "convocationOfTrialCanvas", this))
            {
                convocationOfTrialPanel = convocationOfTrialCanvas.transform.GetChild(0).gameObject;
                if (convocationOfTrialPanel != null)
                {
                    reporterImage = convocationOfTrialPanel.transform.GetChild(0).GetComponent<Image>();
                    corpseTextObject = convocationOfTrialPanel.transform.GetChild(1).gameObject;
                }
            }
            
            // note cba0898: 이것은 무엇..? 그때그때 검증하는 것으로 바꾸시긔.. convocationOfTrialCanvas는 왜 두번..?
            // 검증
            if (DebugUtils.AssertNotNull(convocationOfTrialCanvas, "ConvocationOfTrialCanvas", this))
            {
                if (DebugUtils.AssertNotNull(convocationOfTrialPanel, "ConvocationOfTrialPanel", this))
                {
                    DebugUtils.AssertNotNull(reporterImage, "ReporterImage", this);
                    DebugUtils.AssertNotNull(corpseTextObject, "CorpseTextObject", this);
                }
            }
        }
    }
}
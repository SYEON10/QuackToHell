using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class TrialManager : NetworkBehaviour
{
    private GameObject convocationOfTrialCanvas;
    private GameObject convocationOfTrialPanel;
    private GameObject corpseTextObject;

    private Image reporterImage;
    //TODO:  하드코딩 개선
    private bool colorInjected = false;
    private bool deadPlayerTextInjected = false;
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
                _instance = FindObjectOfType<TrialManager>();
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
    public void TryTrialServerRpc(ulong reporterClientId)
    {
        Debug.Log($"StartTrialServerRpc called with reporterClientId: {reporterClientId}");
        //TODO: UI띄우기 브로드캐스팅
        TrialResultClientRpc(reporterClientId);
    }

    [ClientRpc]
    public void TrialResultClientRpc(ulong reporterClientId)
    {
        Debug.Log($"[multicast] StartTrialClientRpc called with reporterClientId: {reporterClientId}");
        convocationOfTrialPanel.SetActive(true);
        if (!colorInjected)
        {
            InjectReporterColor(reporterClientId);
            colorInjected = true;
        }
        if (!deadPlayerTextInjected)
        {
            InjectReporterPlayerText(reporterClientId);
            deadPlayerTextInjected = true;
        }
        //모든 플레이어의 움직임 멈춤
        PlayerHelperManager.Instance.StopAllPlayer();
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
        NetworkManager.Singleton.SceneManager.LoadScene("CourtScene", LoadSceneMode.Single);
        
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
        switch (colorIndex)
        {
            case 0:
                reporterImage.color = Color.red;
                break;
            case 1:
                reporterImage.color = Color.orange;
                break;
            case 2:
                reporterImage.color = Color.yellow;
                break;
            case 3:
                reporterImage.color = Color.green;
                break;
            case 4:
                reporterImage.color = Color.blue;
                break;
            case 5:
                reporterImage.color = Color.purple;
                break;
        }

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //TODO: 씬 이름 하드코딩 개선 -> 상수로 관리: VillageScene이라고.
        if (scene.name == "VillageScene")
        {
            convocationOfTrialCanvas = GameObject.FindWithTag("ConvocationOfTrialCanvas");
            convocationOfTrialPanel = convocationOfTrialCanvas.transform.GetChild(0).gameObject;
            
            //TODO: 하드코딩 개선
            reporterImage = convocationOfTrialPanel.transform.GetChild(0).GetComponent<Image>();
            corpseTextObject = convocationOfTrialPanel.transform.GetChild(1).gameObject;
        }
    }
}
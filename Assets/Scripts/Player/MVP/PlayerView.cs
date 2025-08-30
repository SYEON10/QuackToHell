using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 시각 및 입력 처리
/// </summary>
public class PlayerView : NetworkBehaviour
{
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
}

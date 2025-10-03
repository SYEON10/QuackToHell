using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MinigameController : MonoBehaviour
{
    [Header("Binding (Designer)")]
    [Tooltip("클라이언트(로컬) 화면에만 뜨는 UI 프리팹")]
    [SerializeField] private GameObject clientUiPrefab;

    [Tooltip("true: 메인 Canvas 아래로 붙임 / false: 지정한 Transform 또는 월드에 생성")]
    [SerializeField] private bool attachUiToMainCanvas = true;

    [Tooltip("UI 부모를 직접 지정하고 싶을 때 사용 (비우면 자동 탐색)")]
    [SerializeField] private Transform uiRootOverride;

    [Header("Interaction")]
    [Tooltip("상호작용 반경(미터). 1m = 1 Unity 유닛")]
    [Min(0.1f)] public float interactionRadius = 1f;

    [Tooltip("상호작용 키(기본: Space)")]
    public KeyCode interactionKey = KeyCode.Space;

    [Tooltip("로컬 플레이어 Transform(비우면 자동 탐색: Netcode Local Player → tag==\"Player\" → Camera.main)")]
    [SerializeField] private Transform overrideLocalPlayer;

    [Header("Highlight (Sprite Outline Clone)")]
    [Tooltip("테두리로 처리할 대상 Renderer들(비우면 자식에서 자동 수집)")]
    [SerializeField] private List<Renderer> highlightRenderers = new();

    [SerializeField, Min(1.0f)] private float outlineScale = 1.08f;   // 복제본 확대 배율
    [SerializeField] private int outlineSortingOffset = 10;            // 정렬순서 오프셋(앞에 나오게 +값)
    [SerializeField] private float outlineAlpha = 1f;                  // 노란색 불투명도(0~1)

    [Header("UX")]
    [Tooltip("UI가 떠 있는 동안 ESC로 닫기 허용")]
    [SerializeField] private bool closeWithEscape = true;

    // state
    private bool _isLocalEligible;
    private GameObject _spawnedLocalUi;
    private readonly List<GameObject> _outlineClones = new();

    // Unity
    private void Awake()
    {
        // 하이라이트 대상 자동 수집(없을 때만)
        if (highlightRenderers == null || highlightRenderers.Count == 0)
        {
            Renderer[] found = GetComponentsInChildren<Renderer>(includeInactive: true);
            highlightRenderers = new List<Renderer>(found);
        }
        DisableHighlight();
    }

    private void Update()
    {
        UpdateLocalEligibilityAndHighlight();

        if (_isLocalEligible && Input.GetKeyDown(interactionKey))
            OpenUi();

        if (closeWithEscape && _spawnedLocalUi && _spawnedLocalUi.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseUi();
    }

    // 2D 클릭
    private void OnMouseUpAsButton()
    {
        if (_isLocalEligible && !IsPointerOverUI())
            OpenUi();
    }

    private void OnDestroy()
    {
        DisableHighlight();
        // 생성했던 복제본 정리
        for (int i = 0; i < _outlineClones.Count; i++)
        {
            if (_outlineClones[i]) Destroy(_outlineClones[i]);
        }
        _outlineClones.Clear();

        if (_spawnedLocalUi) Destroy(_spawnedLocalUi);
    }

    // === Eligibility / Highlight ===
    private void UpdateLocalEligibilityAndHighlight()
    {
        var player = ResolveLocalPlayer();
        if (player == null)
        {
            SetEligible(false);
            return;
        }

        float dist = Vector3.Distance(player.position, transform.position);
        SetEligible(dist <= interactionRadius);
    }

    private Transform ResolveLocalPlayer()
    {
        if (overrideLocalPlayer) return overrideLocalPlayer;

        // Netcode for GameObjects: 각 클라의 로컬 플레이어 우선
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return NetworkManager.Singleton.LocalClient.PlayerObject.transform;
        }

        var tagged = GameObject.FindWithTag("Player");
        if (tagged) return tagged.transform;

        return Camera.main ? Camera.main.transform : null;
    }

    private void SetEligible(bool eligible)
    {
        if (_isLocalEligible == eligible) return;
        _isLocalEligible = eligible;
        if (eligible) EnableHighlight(); else DisableHighlight();
    }

    private void EnableHighlight()
    {
        foreach (var r in highlightRenderers)
        {
            if (!r) continue;
            var sr = r as SpriteRenderer ?? r.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            var existing = sr.transform.Find("OutlineClone");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                continue;
            }

            var go = new GameObject("OutlineClone");
            go.transform.SetParent(sr.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * outlineScale;

            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = sr.sprite;
            sr2.flipX = sr.flipX;
            sr2.flipY = sr.flipY;

            sr2.sortingLayerID = sr.sortingLayerID;
            sr2.sortingOrder = sr.sortingOrder + outlineSortingOffset;

            // 머티리얼 코드로 생성
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.yellow; // 항상 진한 노란색
            sr2.material = mat;

            _outlineClones.Add(go);
        }
    }

    private void DisableHighlight()
    {
        // 복제본만 숨김
        for (int i = 0; i < _outlineClones.Count; i++)
        {
            if (_outlineClones[i]) _outlineClones[i].SetActive(false);
        }

        // 혹시 Emission 머티리얼을 함께 쓰는 경우가 있을 수 있어 원복도 시도(무시 가능)
        foreach (var r in highlightRenderers)
        {
            if (!r) continue;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m) continue;
                if (m.IsKeywordEnabled("_EMISSION"))
                    m.DisableKeyword("_EMISSION");
            }
        }
    }

    // === UI Open/Close ===
    public void OpenUi()
    {
        if (!clientUiPrefab) return;

        if (_spawnedLocalUi && _spawnedLocalUi.activeSelf) return;

        Transform parent = uiRootOverride;
        if (parent == null && attachUiToMainCanvas)
        {
            var canvas = FindMainCanvas();
            if (canvas) parent = canvas.transform;
        }

        _spawnedLocalUi = Instantiate(clientUiPrefab, parent);
        _spawnedLocalUi.SetActive(true);
    }

    public void CloseUi()
    {
        if (_spawnedLocalUi)
        {
            Destroy(_spawnedLocalUi);
            _spawnedLocalUi = null;
        }
    }

    // === Helpers ===
    private Canvas FindMainCanvas()
    {
        var tagged = GameObject.FindWithTag("MainCanvas");
        if (tagged && tagged.TryGetComponent(out Canvas c)) return c;
        return GameObject.FindAnyObjectByType<Canvas>();
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
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

    [Tooltip("로컬 플레이어 Transform(비우면 자동 탐색: tag==\"Player\" → Camera.main)")]
    [SerializeField] private Transform overrideLocalPlayer;

    [Header("Highlight")]
    [Tooltip("노란 테두리(Emission) 적용할 렌더러. 비우면 하위 Renderer 자동 수집")]
    [SerializeField] private List<Renderer> highlightRenderers = new();

    [SerializeField] private float emissionIntensity = 2f;

    [Header("UX")]
    [Tooltip("UI가 떠 있는 동안 ESC로 닫기 허용")]
    [SerializeField] private bool closeWithEscape = true;

    // state 
    private bool _isLocalEligible;
    private GameObject _spawnedLocalUi;

    // Unity
    private void Awake()
    {
        if (highlightRenderers == null || highlightRenderers.Count == 0)
        {
            var found = GetComponentsInChildren<Renderer>(includeInactive: true);
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

    private void OnMouseDown()
    {
        if (_isLocalEligible)
            OpenUi();
    }

    private void OnDestroy()
    {
        DisableHighlight();
        if (_spawnedLocalUi) Destroy(_spawnedLocalUi);
    }

    // Eligibility / Highlight
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
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m) continue;
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", Color.yellow * emissionIntensity);
                }
            }
        }
    }

    private void DisableHighlight()
    {
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

    // UI Open/Close
    public void OpenUi()
    {
        if (!clientUiPrefab) return;

        // 이미 열려 있으면 스킵
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

    // Helpers
    private Canvas FindMainCanvas()
    {
        var tagged = GameObject.FindWithTag("MainCanvas");
        if (tagged)
        {
            var c = tagged.GetComponent<Canvas>();
            if (c) return c;
        }
        return GameObject.FindObjectOfType<Canvas>();
    }
}

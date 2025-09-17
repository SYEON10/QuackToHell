using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public enum RenderRule
{
    AlwaysBehindPlayer,
    AlwaysInFrontOfPlayer,
    ByApproachDirection
}

public enum InteractionType
{
    None,
    Vent,
    Minigame,
    TrialSummon,
    Teleport,
    RareCardShop,
    Entrance
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(40)]
public sealed class ObjectController : MonoBehaviour
{
    [Header("Resource")]
    [SerializeField] private string resourcePathKey;

    [Header("Remote CSV")]
    [Tooltip("Google Sheet CSV export URL (ResourcePathKey, Path, Information)")]
    [SerializeField] private string resourceCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTD0Ax8qLCqNXtM4XtX0GPVHJw9H2YwTH_KYY8wdZfco3hQDh45_ZI0mvBEwxGhtu5GH8SnAKUV00Z2/pub?output=csv";
    [SerializeField] private bool loadCsvOnEnable = true;

    [Header("Collision")]
    [SerializeField] private bool passThrough = true;

    [Header("Rendering")]
    [SerializeField] private RenderRule renderRule = RenderRule.AlwaysBehindPlayer;
    [SerializeField, Range(-50, 50)] private int orderOffset = 0;

    [Header("Interaction")]
    [SerializeField] private bool isInteractable = false;
    [SerializeField] private InteractionType interactionType = InteractionType.None;

    [Header("Refs (자동)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D collider2D;
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private Transform player;

    // 내부 상태
    private int _lastAppliedOrder = int.MinValue;

    // Static CSV Registry
    static Dictionary<string, string> s_keyToPath = new(System.StringComparer.OrdinalIgnoreCase);
    static bool s_loaded;
    static bool s_loading;
    static event System.Action s_onTableUpdated;

    #region Unity Hooks
    void Reset()
    {
        TryCacheRefs();
    }

    void Awake()
    {
        TryCacheRefs();
        ApplyAll(); // 런타임에서도 즉시 적용
    }

    void OnValidate()
    {
        TryCacheRefs();
        ApplyAll(); // 인스펙터 변경 즉시 반영
    }

    void OnEnable()
    {
        s_onTableUpdated -= ApplySprite;
        s_onTableUpdated += ApplySprite;

        if (loadCsvOnEnable && !string.IsNullOrWhiteSpace(resourceCsvUrl))
            TryStartCsvLoad(resourceCsvUrl);

        ApplyAll();
    }

    void OnDisable()
    {
        s_onTableUpdated -= ApplySprite;
    }

    void LateUpdate()
    {
        // 접근 방향 규칙은 프레임마다 갱신
        if (renderRule == RenderRule.ByApproachDirection)
        {
            ApplySortingDynamic();
        }
    }
    #endregion

    #region Apply Pipeline
    private void ApplyAll()
    {
        ApplySprite();
        ApplyCollision();
        ApplySortingInitial();
        EnsureInteractionController();
    }

    private void ApplySprite()
    {
        if (spriteRenderer == null) return;
        if (string.IsNullOrWhiteSpace(resourcePathKey)) return;

        if (s_keyToPath.TryGetValue(resourcePathKey, out var path) && !string.IsNullOrEmpty(path))
        {
            var s = Resources.Load<Sprite>(path);
            if (s != null) spriteRenderer.sprite = s;
        }
    }

    private void ApplyCollision()
    {
        if (collider2D == null) return;
        collider2D.enabled = true;
        collider2D.isTrigger = passThrough; // 통과 O → Trigger
    }

    private void ApplySortingInitial()
    {
        if (renderRule == RenderRule.ByApproachDirection)
        {
            // 동적 규칙은 LateUpdate에서 처리
            ApplySortingDynamic();
            return;
        }

        int baseOrder = 0;
        switch (renderRule)
        {
            case RenderRule.AlwaysBehindPlayer: baseOrder = -100; break;
            case RenderRule.AlwaysInFrontOfPlayer: baseOrder = 100; break;
            default: baseOrder = 0; break;
        }
        SetSortingOrder(baseOrder + orderOffset);
    }

    private SortingGroup _playerSG;
    private SpriteRenderer _playerSR;

    private int GetPlayerSortingOrder()
    {
        if (player == null) return 0;

        if (_playerSG == null) _playerSG = player.GetComponent<SortingGroup>();
        if (_playerSR == null) _playerSR = player.GetComponentInChildren<SpriteRenderer>();

        if (_playerSG != null) return _playerSG.sortingOrder;
        if (_playerSR != null) return _playerSR.sortingOrder;

        return 0; // 플레이어에 정렬 정보 없으면 0 기준
    }

    private void ApplySortingDynamic()
    {
        if (player == null) return;

        int playerOrder = GetPlayerSortingOrder();
        int relative = (player.position.y > transform.position.y) ? +10 : -10;
        int desired = playerOrder + relative + orderOffset;
        SetSortingOrder(desired);
    }

    private void SetSortingOrder(int order)
    {
        if (_lastAppliedOrder == order) return;
        _lastAppliedOrder = order;

        if (sortingGroup != null) sortingGroup.sortingOrder = order;
        else if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
    }

    private void EnsureInteractionController()
    {
        if (!isInteractable || interactionType == InteractionType.None)
        {
#if UNITY_EDITOR
            RemoveIfExists<VentController>();
            RemoveIfExists<TrialSummonController>();
            RemoveIfExists<EntranceController>();
#endif
            return;
        }

        switch (interactionType)
        {
            case InteractionType.Vent:
                AddIfMissing<VentController>();
                break;
            case InteractionType.TrialSummon:
                AddIfMissing<TrialSummonController>();
                break;
            case InteractionType.Entrance:
                AddIfMissing<EntranceController>();
                break;
        }
    }
    #endregion

    #region Helpers
    private void TryCacheRefs()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);

        if (collider2D == null)
            collider2D = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>(true);

        if (sortingGroup == null)
            sortingGroup = GetComponent<SortingGroup>() ?? GetComponentInChildren<SortingGroup>(true);
    }

    private T AddIfMissing<T>() where T : Component
    {
        var t = GetComponent<T>();
        if (t == null) t = gameObject.AddComponent<T>();
        return t;
    }

#if UNITY_EDITOR
    [ContextMenu("Reload Resource CSV Now")]
    void ReloadCsvNow()
    {
        s_loaded = false; s_loading = false; s_keyToPath.Clear();
        TryStartCsvLoad(resourceCsvUrl);
    }

    private void RemoveIfExists<T>() where T : Component
    {
        var t = GetComponent<T>();
        if (t != null)
        {
            if (Application.isPlaying) Destroy(t);
            else DestroyImmediate(t);
        }
    }
#endif

    // 인스펙터에서 바꾸기 쉬운 공개 Setter
    public void SetResourceKey(string key) { resourcePathKey = key; ApplySprite(); }
    public void SetPassThrough(bool v) { passThrough = v; ApplyCollision(); }
    public void SetRenderRule(RenderRule r) { renderRule = r; ApplySortingInitial(); }
    public void SetInteractable(bool v) { isInteractable = v; EnsureInteractionController(); }
    public void SetInteractionType(InteractionType t) { interactionType = t; EnsureInteractionController(); }
    #endregion

    #region CSV Loader
    void TryStartCsvLoad(string url)
    {
        if (s_loaded || s_loading) return;
        StartCoroutine(LoadCsvRoutine(url));
    }

    IEnumerator LoadCsvRoutine(string url)
    {
        s_loading = true;
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogError($"[ObjectController] CSV 다운로드 실패: {req.error}");
            s_loading = false;
            yield break;
        }

        BuildResourceMapFromCsv(req.downloadHandler.text);
        s_loaded = true;
        s_loading = false;
        s_onTableUpdated?.Invoke();
    }

    static void BuildResourceMapFromCsv(string csv)
    {
        s_keyToPath.Clear();

        var lines = SplitLines(csv);
        if (lines.Count == 0) return;

        var header = SplitCsvLine(lines[0]);
        int iKey = HeaderIndex(header, "ResourcePathKey");
        int iPath = HeaderIndex(header, "Path");

        if (iKey < 0 || iPath < 0)
        {
            Debug.LogError("[ObjectController] CSV 헤더에 ResourcePathKey, Path가 필요합니다.");
            return;
        }

        for (int r = 1; r < lines.Count; r++)
        {
            var cols = SplitCsvLine(lines[r]);
            if (cols.Count == 0) continue;
            string key = Get(cols, iKey);
            string path = Get(cols, iPath);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(path)) continue;
            s_keyToPath[key.Trim()] = path.Trim();
        }
    }

    static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        using var sr = new System.IO.StringReader(text);
        string line;
        while ((line = sr.ReadLine()) != null) lines.Add(line);
        return lines;
    }

    static List<string> SplitCsvLine(string line)
    {
        var res = new List<string>();
        if (line == null) return res;
        bool inQ = false;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQ && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; }
                else inQ = !inQ;
            }
            else if (c == ',' && !inQ)
            {
                res.Add(sb.ToString()); sb.Length = 0;
            }
            else sb.Append(c);
        }
        res.Add(sb.ToString());

        for (int i = 0; i < res.Count; i++)
        {
            var s = res[i]?.Trim();
            if (!string.IsNullOrEmpty(s) && s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                s = s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
            res[i] = s;
        }
        return res;
    }

    static int HeaderIndex(List<string> cols, string name)
    {
        for (int i = 0; i < cols.Count; i++)
            if (string.Equals(cols[i]?.Trim(), name, System.StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    static string Get(List<string> cols, int idx)
        => (idx >= 0 && idx < cols.Count) ? (cols[idx] ?? "").Trim() : "";
    #endregion
}

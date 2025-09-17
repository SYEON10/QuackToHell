# QuackToHell 개발 가이드

---

## 1. MVP 패턴 작성법

### 구조
```
View ↔ Presenter ↔ Model
```

### 규칙
- **Presenter만 View와 Model을 가짐**
- **View**: UI만 담당, 이벤트 발생만
- **Model**: 데이터만 담당
- **Presenter**: 중개자 역할, 비즈니스 로직 처리

### 코드 예시

```csharp
// Presenter - View, Model 주입받음
public class PlayerPresenter : NetworkBehaviour
{
    [Header("Components")]
    private PlayerModel playerModel;
    private PlayerView playerView;
    private RoleManager roleManager;
    private PlayerInput playerInput;

    private void Awake()
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
}

// View - 이벤트만 발생
public class PlayerView : MonoBehaviour
{
    public Action OnKillInput;
    
    public void OnKillButtonClick() => OnKillInput?.Invoke();
}

// Model - 데이터만 관리
public class PlayerModel : NetworkBehaviour
{
    public NetworkVariable<PlayerStateData> PlayerStateData { get; private set; }
}
```

### 주의사항
- **View와 Model은 서로를 모르고, Presenter만이 둘을 연결**
- **다른 클래스는 Presenter를 통해서만 View, Model에 접근**
- **View에서 비즈니스 로직 처리 금지**
- **Model에서 UI 직접 조작 금지**

---

## 2. 전략 패턴 확장법

### 새 역할 추가
1. `IRoleStrategy` 인터페이스 구현
2. `RoleManager.CreateStrategyForRole()`에 케이스 추가
3. `PlayerJob` enum에 추가 (`PlayerData.cs`에 enum 있음)

```csharp
// 1. 새 전략 클래스
public class NewRoleStrategy : IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerInput _playerInput;
    private InputActionMap _newRoleActionMap;
    private InputActionMap _commonActionMap;
    
    public void Setup()
    {
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        _newRoleActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.NewRole);
        
        if (_commonActionMap != null) _commonActionMap.Enable();
        if (_newRoleActionMap != null) _newRoleActionMap.Enable();
    }
    
    public void TryKill() { /* 구현 */ }
    public bool CanKill() { return false; }
    // 나머지 메서드들...
}

// 2. RoleManager에 추가
case PlayerJob.NewRole:
    return new NewRoleStrategy(_playerPresenter, playerInput);
```

### 새 Ability 추가
1. `IRoleStrategy`에 새 메서드 추가
2. 모든 전략 클래스에 구현

```csharp
// IRoleStrategy에 추가
void TryNewAbility();
bool CanNewAbility();

// 각 전략 클래스에서 구현
public void TryNewAbility() { /* 구현 */ }
public bool CanNewAbility() { return true; } // 또는 false
```

---

## 3. Unity Input System 사용법

### Input System 활성화
1. **Edit → Project Settings → XR Plug-in Management → Input System Package**
2. **"Active Input Handling"을 "Input System Package (New)" 또는 "Both"로 설정**

### Input Action 설정
1. **Input Actions 에셋 생성**
2. **Action Maps 생성** (Player, Farmer, Animal, Ghost 등)
3. **Actions 생성** (Move, Kill, Interact, Report 등)

### 코드에서 사용

```csharp
public class PlayerView : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    
    private void Start()
    {
        SetupInputSystem();
    }
    
    private void SetupInputSystem()
    {
        InputAction moveAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Move}"];
        if (DebugUtils.AssertNotNull(moveAction, "MoveAction", this))
        {
            moveAction.performed += OnMoveInput;
            moveAction.canceled += OnMoveInput;
        }
    }
}
```

### 역할별 Input Map 관리

```csharp
public class FarmerStrategy : IRoleStrategy
{
    private InputActionMap _farmerActionMap;
    private InputActionMap _commonActionMap;
    
    public void Setup()
    {
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        _farmerActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Farmer);
        
        if (_commonActionMap != null) _commonActionMap.Enable();
        if (_farmerActionMap != null) _farmerActionMap.Enable();
    }
    
    public void Cleanup()
    {
        if (_farmerActionMap != null) _farmerActionMap.Disable();
        if (_commonActionMap != null) _commonActionMap.Disable();
    }
}
```

### Input System 규칙
- **UnityEngine.Input 사용 금지** - Input System으로 완전 전환
- **Action Map으로 역할별 입력 분리** - Farmer, Animal, Ghost 등
- **이벤트 기반 처리** - 직접적인 입력 체크보다는 이벤트 사용
- **Input Action 이름은 상수로 관리** - 하드코딩 방지

---

## 4. Helper 클래스 사용법

### PlayerHelperManager 사용

```csharp
// 플레이어 검색 및 조회
PlayerPresenter player = PlayerHelperManager.Instance.FindPlayerById(clientId);
PlayerPresenter[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers();

// 플레이어 데이터 접근
int playerGold = PlayerHelperManager.Instance.GetPlayerGold(clientId);
bool isPlayerAlive = PlayerHelperManager.Instance.IsPlayerAlive(clientId);

// 캐시 무효화 (플레이어 추가/제거 시)
PlayerHelperManager.Instance.InvalidateCache();
```

### Helper 클래스 작성 규칙

```csharp
// Helper 클래스는 MonoBehaviour 상속
public class MyHelperManager : MonoBehaviour
{
    public static MyHelperManager Instance => SingletonHelper<MyHelperManager>.Instance;
    
    private void Awake()
    {
        SingletonHelper<MyHelperManager>.InitializeSingleton(this);
    }
    
    // 읽기 전용 헬퍼 메서드만 제공
    public PlayerPresenter FindPlayerById(ulong clientId)
    {
        // 구현...
    }
    
    // 실제 데이터 수정은 하지 않음 (읽기 전용)
    public int GetPlayerGold(ulong clientId)
    {
        // 데이터 조회만, 수정은 하지 않음
    }
}
```

### Helper 클래스 주의사항
- **읽기 전용** - 데이터 조회만 하고 수정하지 않음
- **캐싱 활용** - FindObjectsByType 반복 호출 방지
- **싱글톤 패턴** - SingletonHelper 사용
- **MonoBehaviour 상속** - Unity 생명주기 활용

---

## 5. QSingleton 사용법

### MonoBehaviour용

```csharp
public class MyManager : MonoBehaviour
{
    public static MyManager Instance => SingletonHelper<MyManager>.Instance;
    
    private void Awake()
    {
        SingletonHelper<MyManager>.InitializeSingleton(this);
    }
}

// 사용
MyManager.Instance.DoSomething();
```

---

## 6. Utilities 사용법

### DebugUtils

```csharp
if (!DebugUtils.AssertNotNull(player, "Player", this)) return;
if (!DebugUtils.Ensure(health > 0, "Health must be positive", this)) return;
```

### 상수 사용

```csharp
// 하드코딩 금지
if (collider.CompareTag("Player")) // ❌

// 상수 사용
if (collider.CompareTag(GameTags.Player)) // ✅
```

### 새 상수 추가

```csharp
// Assets/Scripts/Utilities/GameInputs.cs
public static class GameInputs
{
    public static class ActionMaps
    {
        public const string Player = "Player";
        public const string Farmer = "Farmer";
        public const string Animal = "Animal";
        public const string Ghost = "Ghost";
    }
    
    public static class Actions
    {
        public const string Move = "Move";
        public const string Kill = "Kill";
        public const string Interact = "Interact";
        public const string Report = "Report";
        public const string Sabotage = "Sabotage";
    }
}
```

---

## 7. 필수 규칙

### 변수명

```csharp
// ❌ 금지
var r = 0;
var player = FindObjectOfType<Player>();

// ✅ 필수
int rowIndex = 0;
Player player = FindObjectOfType<Player>();
```

### 체인 메서드 제거

```csharp
// ❌ 금지
player?.GetComponent<PlayerModel>()?.PlayerStateData?.Value?.IsDead

// ✅ 필수
if (DebugUtils.AssertNotNull(player, "Player", this))
{
    PlayerModel model = player.GetComponent<PlayerModel>();
    if (DebugUtils.AssertNotNull(model, "PlayerModel", this))
    {
        bool isDead = model.PlayerStateData.Value.IsDead;
    }
}
```

### 서버 권위

```csharp
[ServerRpc]
public void TryKillServerRpc(ServerRpcParams rpcParams = default)
{
    // 서버에서 검증 필수
    ulong clientId = rpcParams.Receive.SenderClientId;
    if (!IsOwner || OwnerClientId != clientId) return;
    
    // 비즈니스 로직
    if (!CanKill()) return;
    DoKill();
}
```

### FindObject 최적화

```csharp
// 캐싱 사용
private Player[] _cachedPlayers;
private bool _isCacheValid = false;

private void UpdatePlayerCache()
{
    if (!_isCacheValid)
    {
        _cachedPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
        _isCacheValid = true;
    }
}
```

### TODO 주석 보존

```csharp
// TODO 주석은 절대 삭제하지 말 것
// TODO: 실제 인벤토리 조회로 교체
```

### 전략 패턴 사용법

```csharp
// 다형성으로 간단하게 사용
roleManager.CurrentStrategy?.TryKill();
roleManager.CurrentStrategy?.CanKill();

// 빈 구현이 있어도 문제없음 - 각 역할이 필요한 것만 구현
```

### MVP 패턴 외부 인터페이스

```csharp
// ❌ 직접 접근 금지
playerPresenter.PlayerModel.SomeData = value;

// ✅ 메시지 기반 접근
playerPresenter.RequestStatusChange(newStatus);
playerPresenter.RequestMovement(x, y);
playerPresenter.RequestKill();
```

### 컴포넌트 주입 방식

```csharp
// 통일된 패턴 - GetComponent만 사용
[Header("Components")]
private PlayerModel playerModel;
private PlayerView playerView;

private void Awake()
{
    playerModel = GetComponent<PlayerModel>();
    playerView = GetComponent<PlayerView>();
    
    DebugUtils.AssertComponent(playerModel, "PlayerModel", this);
    DebugUtils.AssertComponent(playerView, "PlayerView", this);
}
```

---

**이 가이드를 따라 개발하면 코드의 일관성과 유지보수성이 크게 향상됩니다! 🚀**

/// <summary>
/// 게임에서 사용하는 모든 상수를 중앙 관리하는 클래스
/// </summary>
public static class GameConstants
{
    // 플레이어 관련 상수
    public static class Player
    {
        public const float GhostSpeedMultiplier = 1.5f;
        public const float GhostTransparency = 0.5f;
        public const int DefaultGold = 100000;
        public const float DefaultMoveSpeed = 10f;
    }
    
    // UI 관련 상수
    public static class UI
    {
        public const float DelayedSceneLoadTime = 2f;
        public const float DetectionRadius = 1.0f;
    }
    
    // 카드 관련 상수
    public static class Card
    {
        public const float InventoryCardWidth = 200f;
        public const float InventoryCardHeight = 300f;
        public const float SaleCardWidth = 200f;
        public const float SaleCardHeight = 350f;
        public const ulong NOT_DISPLAYING_CLIENT_ID = 9999; //아무도 카드를 진열하고있지 않음을 나타내는 값
    }
    
    // 네트워크 관련 상수
    public static class Network
    {
        public const int MinPlayersToStart = 2;
    }
}

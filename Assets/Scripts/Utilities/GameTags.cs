/// <summary>
/// 게임에서 사용하는 모든 태그를 중앙 관리하는 클래스
/// </summary>
public static class GameTags
{
    // 플레이어 관련 태그
    public const string Player = "Player";
    public const string PlayerGhost = "PlayerGhost";
    public const string PlayerCorpse = "PlayerCorpse";
    
    // 카메라 관련 태그
    public const string MainCamera = "MainCamera";
    
    // UI 관련 태그
    public const string UI_ConvocationOfTrialCanvas = "UI_ConvocationOfTrialCanvas";
    
    // 카드 관련 태그
    public const string CardForSale = "CardForSale";
    public const string CardForSaleParent = "CardForSaleParent";
    public const string UI_CardInventory = "UI_CardInventory";
    public const string CardForInventory = "CardForInventory";
    public const string UI_CardShopRow = "UI_CardShopRow";
    public const string UI_CardShopCanvas = "UI_CardShopCanvas";
    public const string UI_CardShopPanel = "UI_CardShopPanel";
    
    
    // 상호작용 관련 태그
    public const string Interactable = "Interactable";
    public const string ConvocationOfTrial = "ConvocationOfTrial";
}

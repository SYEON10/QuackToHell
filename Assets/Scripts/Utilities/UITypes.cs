public static class UITypes{
    public enum UIType{
        WorldSpace,
        Background,
        HUD,
        MenusAndPanels,
        Popup,
        System,
    }
    public static int GetSortingOrder(UIType uiType){
        return uiType switch{
            UIType.WorldSpace => GameConstants.UI.SortingOrder.WorldSpace,
            UIType.Background => GameConstants.UI.SortingOrder.Background,
            UIType.HUD => GameConstants.UI.SortingOrder.HUD,
            UIType.MenusAndPanels => GameConstants.UI.SortingOrder.MenusAndPanels,
            UIType.Popup => GameConstants.UI.SortingOrder.Popup,
            UIType.System => GameConstants.UI.SortingOrder.System,
            _ => GameConstants.UI.SortingOrder.HUD
        };
    }
}
using UnityEngine;
/// <summary>
/// 책임: HUD(Heads-Up Display) 관련 UI를 관리하는 매니저
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField]
    private GameObject inventoryPrefab;
    #region 버튼 바인딩
    public void InventoryButton_OnClick()
    {
        // 비활성화된 오브젝트도 포함해서 찾기
        CardInventoryView[] existingInventories = FindObjectsByType<CardInventoryView>(FindObjectsSortMode.None);
    
        //인벤토리 오브젝트가 없을 때만 생성
        if (existingInventories.Length > 0)
        {
            existingInventories[0].gameObject.SetActive(true);
            return;
        }
        
        Instantiate(inventoryPrefab, Vector3.zero, Quaternion.identity);
        
    }
    #endregion
}

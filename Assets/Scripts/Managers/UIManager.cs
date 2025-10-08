using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager:MonoBehaviour
{
    #region 싱글톤
    public static UIManager Instance => SingletonHelper<UIManager>.Instance;

    private void Awake()
    {
        SingletonHelper<UIManager>.InitializeSingleton(this);
    }
    #endregion
    

    Stack<UIPopup> _popupStack = new Stack<UIPopup>();



    public GameObject HUDRoot
    {
        get
        {
			GameObject root = GameObject.Find("@UI_HUDRoot");
			if (root == null){
                root = new GameObject { name = "@UI_HUDRoot" };
                SetCanvas(root, UITypes.UIType.HUD);
            }
				
            return root;
		}
    }
    public GameObject PopupRoot
    {
        get
        {
			GameObject root = GameObject.Find("@UI_PopupRoot");
			if (root == null){
                root = new GameObject { name = "@UI_PopupRoot" };
                SetCanvas(root, UITypes.UIType.Popup);
            }
				
            return root;
		}
    }

    
    public void SetCanvas(GameObject go, UITypes.UIType uiType)
    {
        Canvas canvas = GameObjectUtils.GetOrAddComponent<Canvas>(go);
        GraphicRaycaster raycaster = GameObjectUtils.GetOrAddComponent<GraphicRaycaster>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = UITypes.GetSortingOrder(uiType);
    }


    /// <summary>
    /// Scene프리팹의 경로는 Resources/UI/Scene 이하여야 합니다.
    /// </summary>
	public T ShowHUDUI<T>(string name = null) where T : UIHUD
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

        GameObject prefab = Resources.Load<GameObject>($"UI/HUD/{name}");
		GameObject go = Object.Instantiate(prefab);
		T sceneUI = GameObjectUtils.GetOrAddComponent<T>(go);

		go.transform.SetParent(HUDRoot.transform,false);

		return sceneUI;
	}


    /// <summary>
    /// Popup프리팹의 경로는 Resources/UI/Popup 이하여야 합니다.
    /// </summary>
	public T ShowPopupUI<T>(string name = null) where T : UIPopup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;
        
        GameObject prefab = Resources.Load<GameObject>($"UI/Popup/{name}");
        GameObject go = Object.Instantiate<GameObject>(prefab);
        T popup = GameObjectUtils.GetOrAddComponent<T>(go);
        _popupStack.Push(popup);

        go.transform.SetParent(PopupRoot.transform,false);

        //새 팝업이 항상 맨 앞에(하이어라키에서 아래로)
        go.transform.SetAsLastSibling();

		return popup;
    }

    /// <summary>
    /// 닫으려는 popup이 존재하지 않으면 닫지 않는 안전ver 메서드
    /// </summary>
    public void ClosePopupUI(UIPopup popup)
    {
		if (_popupStack.Count == 0)
			return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }

        ClosePopupUI();
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UIPopup popup = _popupStack.Pop();
        Object.Destroy(popup.gameObject);
        popup = null;
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }
}

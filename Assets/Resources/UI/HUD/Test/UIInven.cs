using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInven : UIHUD
{
    enum GameObjects
    {
        GridPanel
    }

    void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();

        /*Bind<GameObject>(typeof(GameObjects));

        GameObject gridPanel = Get<GameObject>((int)GameObjects.GridPanel);
        foreach (Transform child in gridPanel.transform)
            Destroy(child.gameObject);

        // 실제 인벤토리 정보를 참고해서
        for (int i = 0; i < 8; i++)
        {
            GameObject item = MakeSubItem<UIInvenItem>(gridPanel.transform).gameObject;            
            UIInvenItem invenItem = item.GetOrAddComponent<UIInvenItem>();
            invenItem.SetInfo($"집행검{i}번");
        }*/
    }
    
    private T MakeSubItem<T>(Transform parent = null, string name = null) where T : UIBase
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject prefab= Resources.Load<GameObject>($"UI/SubItem/Test/{name}");
        GameObject go = Object.Instantiate(prefab);
        if (parent != null)
            go.transform.SetParent(parent);

        return GameObjectUtils.GetOrAddComponent<T>(go);
    }
}

public class UIInvenItem : UIBase
{
    enum GameObjects
    {
        ItemIcon,
        ItemNameText,
    }

    string _name;

    void Start()
    {
        Init();
    }

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        Get<GameObject>((int)GameObjects.ItemNameText).GetComponent<Text>().text = _name;

        Get<GameObject>((int)GameObjects.ItemIcon).BindEvent((PointerEventData) => { Debug.Log($"아이템 클릭! {_name}"); });
    }

    public void SetInfo(string name)
    {
        _name = name;
    }
}

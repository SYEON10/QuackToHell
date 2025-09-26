using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UIPopupTest : UIPopup
{
    enum Buttons
    {
        PointButton
    }

    enum Texts
    {
        Info,
    }

    enum GameObjects
    {
        TestObject,
    }

    enum Images
    {
        ItemIcon,
    }

    private void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));
        Bind<Image>(typeof(Images));

        //Get<Button>((int)Buttons.PointButton).gameObject.BindEvent(OnButtonClicked);

        GameObject go = Get<Image>((int)Images.ItemIcon).gameObject;
        BindEvent(go, (PointerEventData data) => { Debug.Log("Clicked"); }, GameEvents.UIEvent.Click);

        TextMeshProUGUI text =  Get<TextMeshProUGUI>((int)Texts.Info);
        text.text = "바꿔치기술";
        
        
    }

    int _score = 0;

    public void OnButtonClicked(PointerEventData data)
    {
        _score++;
        Get<Text>((int)Texts.Info).text = $"r상태 : {_score}";
    }

}
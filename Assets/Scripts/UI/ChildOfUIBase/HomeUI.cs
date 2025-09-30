using System;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
public class HomeUI : UIHUD
{
    private GameObject GameStartContents_gameObject;
    private GameObject SettingContents_gameObject;
    private GameObject CreateGameContents_gameObject;
    private GameObject EnterCodeContents_gameObject;
    private GameObject FindGame_gameObject;
    
    enum Buttons
    {
        Button_GameStart,
        Button_Option,
        Button_CreateGame,
        Button_FindGame,
        Button_EnterGame,
        Button_Back_CreateGameContents,
        Button_Back_EnterGameContents,
        Button_Back_FindGame
    }
    
    enum GameObjects
    {
        //버튼누르면 Img_Info에 활성화되는 애들
        GameStartContents,
        SettingContents,
        CreateGameContents,
        EnterCodeContents,
        //버튼누르면 활성화되는 애
        FindGame
    }
    
    private void Start()
    {
        base.Init();
        
        Bind<GameObject>(typeof(GameObjects));
        GameStartContents_gameObject = Get<GameObject>((int)GameObjects.GameStartContents).gameObject;
        SettingContents_gameObject = Get<GameObject>((int)GameObjects.SettingContents).gameObject;
        CreateGameContents_gameObject = Get<GameObject>((int)GameObjects.CreateGameContents).gameObject;
        EnterCodeContents_gameObject = Get<GameObject>((int)GameObjects.EnterCodeContents).gameObject;
        FindGame_gameObject = Get<GameObject>((int)GameObjects.FindGame).gameObject;
        
        Bind<Button>(typeof(Buttons));
        
        GameObject Button_GameStart_gameObject = Get<Button>((int)Buttons.Button_GameStart).gameObject;
        BindEvent(Button_GameStart_gameObject,OnClicked_GameStart, GameEvents.UIEvent.Click);
        GameObject Button_Option_gameObject = Get<Button>((int)Buttons.Button_Option).gameObject;
        BindEvent(Button_Option_gameObject,OnClicked_Option, GameEvents.UIEvent.Click);
        GameObject Button_CreateGame_gameObject = Get<Button>((int)Buttons.Button_CreateGame).gameObject;
        BindEvent(Button_CreateGame_gameObject,OnClicked_CreateGame, GameEvents.UIEvent.Click);
        GameObject Button_FindGame_gameObject = Get<Button>((int)Buttons.Button_FindGame).gameObject;
        BindEvent(Button_FindGame_gameObject,OnClicked_FindGame, GameEvents.UIEvent.Click);
        GameObject Button_EnterGame_gameObject = Get<Button>((int)Buttons.Button_EnterGame).gameObject;
        BindEvent(Button_EnterGame_gameObject,OnClicked_EnterCode, GameEvents.UIEvent.Click);
        GameObject Button_Back_CreateGameContents_gameObject = Get<Button>((int)Buttons.Button_Back_CreateGameContents).gameObject;
        BindEvent(Button_Back_CreateGameContents_gameObject,OnClicked_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_Back_EnterGameContents_gameObject = Get<Button>((int)Buttons.Button_Back_EnterGameContents).gameObject;
        BindEvent(Button_Back_EnterGameContents_gameObject,OnClicked_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_Back_FindGame_gameObject = Get<Button>((int)Buttons.Button_Back_FindGame).gameObject;
        BindEvent(Button_Back_FindGame_gameObject,OnClicked_Button_Back, GameEvents.UIEvent.Click);
        
    }

    private void OnClicked_GameStart(PointerEventData data)
    {
        GameStartContents_gameObject.SetActive(true);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_Option(PointerEventData data)
    {
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(true);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_CreateGame(PointerEventData data)
    {
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(true);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_EnterCode(PointerEventData data)
    {
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(true);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_FindGame(PointerEventData data)
    {
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(true);
    }

    private void OnClicked_Button_Back(PointerEventData data)
    {
        //TODO: Stack으로 관리해서 이전 화면이 나타나게 Pop 
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
}

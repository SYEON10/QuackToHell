using TMPro;
using Unity.Netcode;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using Unity.Netcode;
public class LobbyUI : UIHUD
{
    private TMP_Dropdown colorDropdown;
    
    enum Dropdowns
    {
        Dropdown_Color,
    }

    enum Texts
    {
        Text_Code,
        Text_Button_StartGame
    }

    enum Buttons
    {
        Button_Back,
        Button_StartGame
    }

    private void Start()
    {
        base.Init();
        
        Bind<TMP_Dropdown>(typeof(Dropdowns));
        colorDropdown = Get<TMP_Dropdown>((int)Dropdowns.Dropdown_Color);
        colorDropdown.onValueChanged.AddListener(OnColorDropdownButton);
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        Get<TextMeshProUGUI>((int)Texts.Text_Code).text = LobbyManager.Instance.HostLobbyCode;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame).text = "Game Start";
        }
        else
        {
            Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame).text = "Ready";
        }
        
        
        Bind<Button>(typeof(Buttons));
        GameObject Button_Back_gameObject =  Get<Button>((int)Buttons.Button_Back).gameObject;
        BindEvent(Button_Back_gameObject, OnClick_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_StartGame_gameObject = Get<Button>((int)Buttons.Button_StartGame).gameObject;
        BindEvent(Button_StartGame_gameObject, OnClick_Button_StartGame, GameEvents.UIEvent.Click);
        
        
    }
    private void OnClick_Button_StartGame(PointerEventData data)
    {
        LobbyManager.Instance.StartGame();
    }
    private void OnClick_Button_Back(PointerEventData data)
    {
        LobbyManager.Instance.CleanUpLobby();
    }
    private void OnColorDropdownButton(Int32 colorIndex)
    {
        PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).ChangeColorServerRpc(colorIndex, NetworkManager.Singleton.LocalClientId);
    }
    
}

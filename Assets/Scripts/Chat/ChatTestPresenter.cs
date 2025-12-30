using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ChatTestPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatTestView chatTestView;

    [Header("Scene Lifecycle Settings")]
    [SerializeField] private bool loadExistingMessagesOnStart = true;
    // note cba0898: const나 SO, json 등으로 별도 분리 추천
    [SerializeField] private int loadMessageCount = 50;

    // note cba0898: const나 SO, json 등으로 별도 분리 추천
    [Header("Display Settings")]
    [SerializeField] private bool showTimestamps = false;
    [SerializeField] private Color ownMessageNameColor = Color.red;
    [SerializeField] private Color otherMessageNameColor = Color.white;
    [SerializeField] private Color ownMessageTextColor = Color.white;
    [SerializeField] private Color otherMessageTextColor = Color.white;

    private void Start()
    {
        Debug.Assert(chatTestView != null, $"[ChatTestPresenter][{this.name}]] ChatTestView is not assigned");

        BindEvents();

        if (loadExistingMessagesOnStart)
        {
            LoadExistingMessages();
        }
    }

    private void BindEvents()
    {
        ChatModel.Instance.OnMessageReceived += OnMessageReceived;

        chatTestView.OnSendButtonClicked += OnSendButtonClicked;
        chatTestView.OnInputFieldSubmit += OnSendButtonClicked;
    }

    private void UnbindEvents()
    {
        if (ChatModel.Instance != null)
        {
            ChatModel.Instance.OnMessageReceived -= OnMessageReceived;
        }
        
        if (chatTestView != null)
        {
            chatTestView.OnSendButtonClicked -= OnSendButtonClicked;
            chatTestView.OnInputFieldSubmit -= OnSendButtonClicked;
        }
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void LoadExistingMessages()
    {
        List<ChatMessageData> existingMessages = ChatModel.Instance.GetRecentMessages(loadMessageCount);

        foreach (ChatMessageData messageData in existingMessages)
        {
            // 필터링 로직
            if (ShouldDisplayMessage(messageData))
            {
                ChatTestUIData chatTestUIData = GetChatTestUIData(messageData);
                chatTestView.AddMessage(chatTestUIData);
            }
        }
    }

    private void OnMessageReceived(ChatMessageData messageData)
    {
        Debug.Assert(chatTestView != null);

        if (ShouldDisplayMessage(messageData))
        {
            ChatTestUIData chatTestUIData = GetChatTestUIData(messageData);
            chatTestView.AddMessage(chatTestUIData);
        }
    }

    private ChatTestUIData GetChatTestUIData(ChatMessageData messageData)
    {
        bool isOwnMessage = (messageData.senderId == NetworkManager.Singleton.LocalClientId);
        
        Color nameColor = isOwnMessage ? ownMessageNameColor : otherMessageNameColor;
        Color messageColor = isOwnMessage ? ownMessageTextColor : otherMessageTextColor;
        
        string timestamp = showTimestamps ? ChatHelper.GetFormatTimestamp(messageData.timestamp) : "";
        
        ChatTestUIData chatTestUIData = new ChatTestUIData();
        chatTestUIData.playerName = messageData.senderName;
        chatTestUIData.message = messageData.message;
        chatTestUIData.playerIcon = null;
        chatTestUIData.nameColor = nameColor;
        chatTestUIData.messageColor = messageColor;
        chatTestUIData.showTimestamp = showTimestamps;
        chatTestUIData.timestamp = timestamp;
        chatTestUIData.isOwnMessage = isOwnMessage;

        return chatTestUIData;
    }

    private void OnSendButtonClicked()
    {
        string messageText = chatTestView.GetInputText();
        if (string.IsNullOrWhiteSpace(messageText))
            return;

        ChatModel.Instance.SendMessage(messageText);
        chatTestView.ClearInputText();
        chatTestView.FocusInputField();
    }


    /// <summary>
    /// 메시지 표시 여부 판단 (필터링 로직)
    /// </summary>
    protected bool ShouldDisplayMessage(ChatMessageData message)
    {
        // 재판장 씬이 아니면 모든 메시지 표시
        if (SceneManager.GetActiveScene().name != GameScenes.Court)
        {
            return true;
        }
        
        // 로컬 플레이어가 생존자인지 확인
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerModel localPlayerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
    
        if (localPlayerModel == null)
        {
            // 플레이어를 찾을 수 없으면 일단 표시 (안전장치)
            return true;
        }
    
        PlayerLivingState localPlayerState = localPlayerModel.GetPlayerAliveState();
    
        // 로컬 플레이어가 죽었으면: 모든 채팅 보임
        if (localPlayerState == PlayerLivingState.Dead)
        {
            return true;
        }
    
        // 로컬 플레이어가 살아있으면: 죽은 플레이어의 채팅은 숨김
        if (localPlayerState == PlayerLivingState.Alive)
        {
            // senderState가 Dead면 표시하지 않음
            return message.senderState != PlayerChatState.Dead;
        }
        
        return true;
    }
}

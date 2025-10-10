using UnityEngine;
using System;
using UnityEngine.UI;

public interface ICardShopView
{

    event Action OnClickLock;
    event Action OnClickReRoll;

    void ShowLoading(bool on);
    void ShowResult(bool success, string msg);

    void SetLockedVisual(bool locked);
    void SetRefreshInteractable(bool interactable);
}

public class CardShopView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button lockButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button xButton;
    [SerializeField] private GameObject cardShopPanel;
    [SerializeField] private Animator cardShopPanelAnimator;


    public event Action OnClickLock;
    public event Action OnClickReRoll;
    public event Action OnClickX;

    private void Awake()
    {
        DebugUtils.AssertNotNull(lockButton, "lockButton", this);
        DebugUtils.AssertNotNull(rerollButton, "rerollButton", this);
        DebugUtils.AssertNotNull(xButton, "xButton", this);

        lockButton.onClick.AddListener(() => OnClickLock?.Invoke());
        rerollButton.onClick.AddListener(() => OnClickReRoll?.Invoke());
        xButton.onClick.AddListener(() => OnClickX?.Invoke());
    }

    private void Start()
    {
        cardShopPanelAnimator = cardShopPanel.GetComponent<Animator>();
    }

    private void OnDestroy()
    {
        if (lockButton != null)
        {
            lockButton.onClick.RemoveListener(() => OnClickLock?.Invoke());
        }
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(() => OnClickReRoll?.Invoke());
        }
        if (xButton != null)
        {
            xButton.onClick.RemoveListener(() => OnClickX?.Invoke());
        }
    }
    
    public void SetRefreshInteractable(bool interactable)
    {
        if (rerollButton) rerollButton.interactable = interactable;
    }

    public void ToggleCardShopUI(bool isActive)
    {
        DebugUtils.AssertNotNull(cardShopPanel, "cardShopPanel", this);
        DebugUtils.AssertNotNull(cardShopPanelAnimator, "cardShopPanelAnimator", this);

        cardShopPanel.SetActive(isActive);
        cardShopPanelAnimator.SetBool("Active", isActive);
    }
    
/*
    #region x버튼 바인딩 함수

    /// <summary>
    /// X버튼 바인딩 함수
    /// </summary>
    public void XButton_OnClick()
    {
        OnClickX?.Invoke();
    }    

    #endregion
*/

}

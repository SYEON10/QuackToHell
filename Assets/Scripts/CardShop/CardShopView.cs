using UnityEngine;
using System;
using UnityEngine.UI;

public interface ICardShopView
{
    event Action<int, ulong, int> OnClickBuy;
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
    [SerializeField] private GameObject cardShopPanel;
    [SerializeField] private Animator cardShopPanelAnimator;

    public event Action<int, ulong, int> OnClickBuy;
    public event Action OnClickLock;
    public event Action OnClickReRoll;

    private void Awake()
    {

        if (lockButton) lockButton.onClick.AddListener(() => OnClickLock?.Invoke());
        if (rerollButton) rerollButton.onClick.AddListener(() => OnClickReRoll?.Invoke());
    }

    public void SetRefreshInteractable(bool interactable)
    {
        if (rerollButton) rerollButton.interactable = interactable;
    }


    #region x버튼 바인딩 함수

    private void Start()
    {
        cardShopPanelAnimator = cardShopPanel.GetComponent<Animator>();
    }

    /// <summary>
    /// X버튼 바인딩 함수
    /// </summary>
    public void XButton_OnClick()
    {
        cardShopPanel.GetComponent<Animator>().SetBool("Active", false);
    }    

    #endregion


}

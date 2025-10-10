using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // UI 클릭을 위해 Image 권장
public class ClickSpotUI : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("순서 모드일 때 이 인덱스 순서로 클릭해야 함")]
    [SerializeField] private int orderIndex = 0;
    [Tooltip("클릭 시 바뀔 비주얼(선택). 예: 체크마크, 틴트 변경 등")]
    [SerializeField] private GameObject onClickedVisual;
    [Tooltip("잘못된 순서 클릭 시 잠깐 깜빡이는 연출(선택)")]
    [SerializeField] private Animation wrongOrderAnim;

    private ClickOncePointsGame _manager;
    private bool _clicked;

    public int OrderIndex => orderIndex;
    public bool WasClicked => _clicked;

    // 매니저가 호출
    public void Init(ClickOncePointsGame manager)
    {
        _manager = manager;
        ResetState();
    }

    public void ResetState()
    {
        _clicked = false;
        if (onClickedVisual) onClickedVisual.SetActive(false);

        // 기본 색/연출 복원
        var img = GetComponent<Image>();
        if (img) img.color = Color.white;
    }

    public void MarkClicked()
    {
        _clicked = true;
        if (onClickedVisual) onClickedVisual.SetActive(true);

        var img = GetComponent<Image>();
        if (img) img.color = new Color(0.8f, 1f, 0.8f, 1f); // 연한 초록 틴트(임시)
    }

    public void PlayWrongOrderFeedback()
    {
        if (wrongOrderAnim) wrongOrderAnim.Play();
        else
        {
            // 간단한 깜빡임
            var img = GetComponent<Image>();
            if (img) img.color = new Color(1f, 0.8f, 0.8f, 1f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_clicked) return;
        _manager?.NotifySpotClicked(this);
    }

#if UNITY_EDITOR
    // 에디터에서 우클릭 메뉴로 순서 찍기 편하게
    [ContextMenu("Order++")]
    private void CtxOrderPlus() => orderIndex++;
    [ContextMenu("Order--")]
    private void CtxOrderMinus() => orderIndex--;
#endif
}

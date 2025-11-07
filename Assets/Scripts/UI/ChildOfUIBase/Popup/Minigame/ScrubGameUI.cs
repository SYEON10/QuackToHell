using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class ScrubGameUI : UIPopup
{
    private float totalScrubDistance = 0f;
    [Header("Write the goal distance(pixels) to complete the game")]
    [SerializeField] 
    private float distanceToComplete = 1000f;
    
    private Vector3 startPosition;
    private GameObject targetGameObject;
    enum Images
    {
        Target,
    }

    
    private void Start()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        targetGameObject = Get<Image>((int)Images.Target).gameObject;
        BindEvent(targetGameObject,OnRubbing, GameEvents.UIEvent.Drag);
        BindEvent(targetGameObject,OnBeginDrag, GameEvents.UIEvent.BeginDrag);
        BindEvent(targetGameObject,OnEndDrag, GameEvents.UIEvent.EndDrag);
    }
    
    private void OnEndDrag(PointerEventData data)
    {

        targetGameObject.transform.position = startPosition;
    }

    private void OnBeginDrag(PointerEventData data)
    {
        
        startPosition = targetGameObject.transform.position;
    }
    
    private void OnRubbing(PointerEventData data)
    {
        
        //위치 움직이기
        targetGameObject.transform.position = data.position;
        //움직인거리
        totalScrubDistance += data.delta.magnitude;
        Debug.Log($"OnRubbing: {totalScrubDistance}");
        if (totalScrubDistance >= distanceToComplete)
        {
            OnGameComplete();
        }
    }

    private void OnGameComplete()
    {
        Debug.Log("문지르기 성공!");
        gameObject.SetActive(false);
    }
    
}

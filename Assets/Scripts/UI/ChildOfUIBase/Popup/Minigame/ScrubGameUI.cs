using System.Threading;
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
    private GameObject eraserGameObject;
    private GameObject targetGameObject;
    private RectTransform eraserRectTransform;
    private RectTransform targetRectTransform;
    private Image targetImage;
    private bool isComplete = false;
    private float progress = 1f;
    enum Images
    {
        Eraser,
        Target,
    }

    
    private void Start()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        eraserGameObject = Get<Image>((int)Images.Eraser).gameObject;
        eraserRectTransform = Get<Image>((int)Images.Eraser).GetComponent<RectTransform>();
        BindEvent(eraserGameObject,OnRubbing, GameEvents.UIEvent.Drag);
        BindEvent(eraserGameObject,OnBeginDrag, GameEvents.UIEvent.BeginDrag);
        BindEvent(eraserGameObject,OnEndDrag, GameEvents.UIEvent.EndDrag);
        targetGameObject = Get<Image>((int)Images.Target).gameObject;
        targetRectTransform = Get<Image>((int)Images.Target).GetComponent<RectTransform>();
        targetImage = Get<Image>((int)Images.Target).GetComponent<Image>();
    }
    
    /*eraser rect랑 target rect 겹치는지 확인 : (xmin,xmax,ymin,ymax)사이에 포인터가 있는지 체크*/
    private bool IsRectTransformOverlapping(RectTransform rectA, RectTransform rectB)
    {
        //각 렉트트랜스폼의 월드좌표기준 4개 코너 가져오기
        Vector3[] cornersA = new Vector3[4];
        rectA.GetWorldCorners(cornersA);
        //xmin, xmax, ymin, ymax로 사각형 만들기
        Rect rect1 = new Rect(cornersA[0].x, cornersA[0].y, cornersA[2].x - cornersA[0].x, cornersA[2].y - cornersA[0].y);
                
        Vector3[] cornersB = new Vector3[4];
        rectB.GetWorldCorners(cornersB);
        Rect rect2 = new Rect(cornersB[0].x, cornersB[0].y, cornersB[2].x - cornersB[0].x, cornersB[2].y - cornersB[0].y);
        
        return rect1.Overlaps(rect2);
    }
    
    private void OnEndDrag(PointerEventData data)
    {

        eraserGameObject.transform.position = startPosition;
    }

    private void OnBeginDrag(PointerEventData data)
    {
        
        startPosition = eraserGameObject.transform.position;
    }
    
    private void OnRubbing(PointerEventData data)
    {
        if(isComplete) return;
        
        //위치 움직이기
        eraserGameObject.transform.position = data.position;
        
        //안겹치면 로직x
        if (!IsRectTransformOverlapping(eraserRectTransform, targetRectTransform))
        {
            return;
        }
        
        //움직인거리
        totalScrubDistance += data.delta.magnitude;
        Debug.Log($"totalScrubDistance: {totalScrubDistance}");
        progress = 1f - Mathf.Clamp01(totalScrubDistance /distanceToComplete) ;
        Debug.Log($"progress: {progress}");
        
        //클리어 조건
        if (totalScrubDistance >= distanceToComplete)
        {
            progress = 0f;
            OnGameComplete();
        }
       
        //투명해지기 (움직이는 거리에 비례해서 a값 조정)
        Color color = targetImage.color;
        color.a = progress;
        targetImage.color = color;
    }
    


    private void OnGameComplete()
    {
        Debug.Log("문지르기 성공!");
        isComplete = true;
    }
    
}

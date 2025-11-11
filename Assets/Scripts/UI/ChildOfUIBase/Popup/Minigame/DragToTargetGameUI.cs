using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

//TODO: 올릴 골드 세팅하고(인스펙터용 열기) 클리어 시 골드증가(server rpc)
public class DragToTargetGameUI : UIPopup
{
    [Tooltip("Drop Zone Positions: you have to fit index 0 to 4 like gameObject name: DragItem0, DragItem1, DragItem2, DragItem3, DragItem4")]
    [SerializeField] private RectTransform[] onDroppedFitPositions;
    
    private bool isComplete = false;
    private Vector3[] _onDroppedFitPositions = new Vector3[5];
    private GameObject[] dragItemGameObject = new GameObject[5];

    private int matchCount = 0;
    private const int TOTAL_MATCH_COUNT = 5;
    enum Images
    {
        DropZone0,    // 아이템을 놓을 영역
        DropZone1,
        DropZone2,
        DropZone3,
        DropZone4,
        DragItem0,     // 드래그할 아이템
        DragItem1,
        DragItem2,
        DragItem3,
        DragItem4,
    }

    
    private void Start()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        for (int i = 0; i < 5; i++)
        {
            dragItemGameObject[i] = Get<Image>((int)Images.DragItem0 + i).gameObject;
            BindEvent(dragItemGameObject[i],OnBeginDrag, GameEvents.UIEvent.BeginDrag);
            BindEvent(dragItemGameObject[i],OnEndDrag, GameEvents.UIEvent.EndDrag);
            BindEvent(dragItemGameObject[i],OnDragging, GameEvents.UIEvent.Drag);
            
            _onDroppedFitPositions[i] = dragItemGameObject[i].transform.position;
        
            GameObject dropZoneGameObject = Get<Image>((int)Images.DropZone0 + i).gameObject;
            BindEvent(dropZoneGameObject, OnDropped, GameEvents.UIEvent.Drop);
        }
        
    }

    int ReturnDragItemGameObjectIndex(string objectName)
    {
        Match match = Regex.Match(objectName, @"\d+");
        if (match.Success)
        {
            //out int index는 if문 안에서만 사용할 수 있는 index라는 새로운 정수 변수를 선언
            if (int.TryParse(match.Value, out int index))
            {
                return index;
            }
        }
        return -1;
    }
    private void OnEndDrag(PointerEventData data)
    {
        string objectName = data.pointerDrag.gameObject.name;
        int index = ReturnDragItemGameObjectIndex(objectName);
        
        if (index!=-1)
        {
            dragItemGameObject[index].transform.position = _onDroppedFitPositions[index];
        }
    }

    private void OnDropped(PointerEventData data)
    {
        int myIndex= ReturnDragItemGameObjectIndex(data.pointerEnter.name);
        int otherIndex = ReturnDragItemGameObjectIndex(data.pointerDrag.name);

        if (myIndex != -1 && otherIndex != -1)
        {
            if (myIndex == otherIndex)
            {
                matchCount++;
                
                //투명컬러
                Color color = new Color(1f, 1f, 1f, 0.5f);
                data.pointerDrag.gameObject.GetComponent<Image>().color = color;
                data.pointerEnter.gameObject.GetComponent<Image>().color = color;
                SetDragItemPosition(index: otherIndex);
                if (matchCount == TOTAL_MATCH_COUNT)
                {
                    isComplete = true;
                    OnGameComplete();
                }
            }
            
        }
    }

    void SetDragItemPosition(int index)
    {
        //해당 인덱스로, 매칭위치 덮어씌우기
        _onDroppedFitPositions[index] = onDroppedFitPositions[index].position;
    }

    private void OnBeginDrag(PointerEventData data)
    {
        string objectName = data.pointerDrag.gameObject.name;
        int index = ReturnDragItemGameObjectIndex(objectName);
        if (index!=-1)
        {
            _onDroppedFitPositions[index] = dragItemGameObject[index].transform.position;
        }
    }
    
    private void OnDragging(PointerEventData data)
    {
        string objectName = data.pointerDrag.gameObject.name;
        int index = ReturnDragItemGameObjectIndex(objectName);
        if (index!=-1)
        {
            //위치 움직이기
            dragItemGameObject[index].transform.position = data.position;
        }
    }
    
    private void OnGameComplete()
    {
        Debug.Log("타겟 위치로 드래그하기 성공!");
        gameObject.SetActive(false);
    }

}

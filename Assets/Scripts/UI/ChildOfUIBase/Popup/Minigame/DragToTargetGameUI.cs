using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragToTargetGameUI : UIPopup
{
    private bool isComplete = false;
    private Vector3 startPosition;
    private GameObject dragItemGameObject;
    enum Images
    {
        DropZone,    // 아이템을 놓을 영역
        DragItem     // 드래그할 아이템
    }

    
    private void Start()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        dragItemGameObject = Get<Image>((int)Images.DragItem).gameObject;
        BindEvent(dragItemGameObject,OnBeginDrag, GameEvents.UIEvent.BeginDrag);
        BindEvent(dragItemGameObject,OnEndDrag, GameEvents.UIEvent.EndDrag);
        BindEvent(dragItemGameObject,OnDragging, GameEvents.UIEvent.Drag);
        
        GameObject dropZoneGameObject = Get<Image>((int)Images.DropZone).gameObject;
        BindEvent(dropZoneGameObject, OnDrapped, GameEvents.UIEvent.Drop);
    }
    
    private void OnEndDrag(PointerEventData data)
    {
        if (isComplete)
        {
            
        }
        else
        {
            dragItemGameObject.transform.position = startPosition;
        }
      
    }

    private void OnDrapped(PointerEventData data)
    {
        Debug.Log(data.pointerDrag.name + " 아이템이 드롭되었습니다!");
        isComplete = true;
        OnGameComplete();
    }

    private void OnBeginDrag(PointerEventData data)
    {
        startPosition = dragItemGameObject.transform.position;
    }
    
    private void OnDragging(PointerEventData data)
    {
        //위치 움직이기
        dragItemGameObject.transform.position = data.position;
    }
    
    private void OnGameComplete()
    {
        Debug.Log("타겟 위치로 드래그하기 성공!");
        gameObject.SetActive(false);
    }

}

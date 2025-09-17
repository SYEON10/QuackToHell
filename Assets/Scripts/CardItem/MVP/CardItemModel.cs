using Unity.Netcode;
using UnityEngine;

namespace CardItem.MVP
{
    public class CardItemModel : MonoBehaviour
    {
        /*   private void Start()
       {
           OnCardDefDataChanged += (newValue) =>
           {
               CardDefData = newValue;
           };

           OnCardItemStatusDataChanged += (newValue) =>
           {
               SetStateByCardItemStateEnum(newValue.State);
               ApplyStateChange();
           };
           SetStateByCardItemStateEnum(CardItemStatusData.State);
           ApplyStateChange();


       }
       private void Update()
       {
           if (curState != null)
           {
               curState.OnStateUpdate();
           }
       }*/

    


        #region 데이터
        private readonly NetworkVariable<CardItemData> _cardItemData = new NetworkVariable<CardItemData>();
        public NetworkVariable<CardItemData> CardItemData => _cardItemData;

        [ServerRpc]
        public void UpdateCardDataServerRpc(CardItemData newData)
        {
            _cardItemData.Value = newData;
        }

        public void UpdateCardStateFromServer(CardItemData newData)
        {
            // 이전 값을 저장
            CardItemData previousValue = _cardItemData.Value;
            
            // 서버에서 직접 수정하거나, 클라이언트에서 서버 동기화 받기
            _cardItemData.Value = newData;
            
            // OnValueChanged 이벤트가 발생하지 않으므로 수동으로 상태 업데이트
            OnCardItemDataChanged(previousValue, newData);
        }

        #endregion

        #region 카드 상태

        // 상태 컴포넌트들 (미리 생성)
        private CardItemNoneState noneStateComponent;
        private CardItemSoldState soldStateComponent;
        private CardItemSoldingState soldingStateComponent;

        private StateBase preState;
        private StateBase tempState;
        private StateBase curState;

        private void Start()
        {
            // 미리 부착된 컴포넌트들 참조
            noneStateComponent = GetComponent<CardItemNoneState>();
            soldStateComponent = GetComponent<CardItemSoldState>();
            soldingStateComponent = GetComponent<CardItemSoldingState>();

            // NetworkVariable 값 변경 이벤트 바인딩
            _cardItemData.OnValueChanged += OnCardItemDataChanged;
            
            // 초기 상태 설정
            if (_cardItemData.Value.cardItemStatusData.State != CardItemState.None)
            {
                SetStateByCardItemStateEnum(_cardItemData.Value.cardItemStatusData.State);
                ApplyStateChange();
            }
        }

        private void OnCardItemDataChanged(CardItemData previousValue, CardItemData newValue)
        {
            // 상태가 변경되었을 때만 상태 업데이트
            if (previousValue.cardItemStatusData.State != newValue.cardItemStatusData.State)
            {
                SetStateByCardItemStateEnum(newValue.cardItemStatusData.State);
            }
            
        }

        private void SetStateByCardItemStateEnum(CardItemState inputCardItemState = CardItemState.None)
        {
            switch (inputCardItemState)
            {
                case CardItemState.None:
                    SetState(noneStateComponent);
                    break;
                case CardItemState.Sold:
                    SetState(soldStateComponent);
                    break;
                case CardItemState.Solding:
                    SetState(soldingStateComponent);
                    break;
                default:
                    break;
            }
        }

        private void SetState(StateBase state)
        {
            tempState = curState;
            curState = state;
            preState = tempState;

            // 이전 상태 비활성화
            if (preState != null)
            {
                preState.enabled = false;
            }

            // 현재 상태 활성화
            if (curState != null)
            {
                curState.enabled = true;
            }

            // 상태 변경 적용 (OnStateEnter/OnStateExit 호출)
            ApplyStateChange();
        }

        private void ApplyStateChange()
        {
            string preStateName = preState?.GetType().Name ?? "null";
            string curStateName = curState?.GetType().Name ?? "null";
            
            if (preState != null)
            {
                preState.OnStateExit();
            }
            
            if (curState != null)
            {
                curState.OnStateEnter();
            }
        }

        private void Update()
        {
            if (curState != null)
            {
                curState.OnStateUpdate();
            }
        }

        #endregion
    }
}

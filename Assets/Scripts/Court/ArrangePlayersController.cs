using System;
using Unity.Netcode;
using UnityEngine;

//재판장-03 플레이어 배치
//[서버 사이드 스크립트]
//플레이어 배치 시각화, 플레이어 Sprite의 Layer Order(Z축) 지정을 담당

//TODO. 재판을 연 플레이어 표시는? => 어디 데이터에서 넘어오는지 판단 후 결정 해당 스크립트에서 작업하지 않아도 됨
namespace Court
{
    public class ArrangePlayersController : NetworkBehaviour
    {
        [SerializeField] private Vector2 baseCenterPosition = Vector2.zero; // 중앙 기준점
        [SerializeField] private float horizontalSpacing = 1.0f;  // 좌우 간격
        [SerializeField] private float verticalFloorHeight = 1.5f; //층 높이 간격 (중앙 기준점 기준)
        [SerializeField] private float reporterIndicatorYOffset = 0.5f; //재판 호출자 벨 스폰 Y축 Offset
        [SerializeField] private GameObject reporterIndicatorPrefab;

        private void Start()
        {
            //모든 플레이어의 Animation을 Idle로 변경
            //trialManager에 TrialResultClientRpc 함수가 있는데 필요에 따라 거기다 넣어도 됨
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            PlayerModel model = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
            model.SetAnimationStateServerRpc(PlayerAnimationState.Idle);

            //플레이어를 자리에 배치
            if (IsHost)
            {
                PlayerModel[] allPlayers = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>();
                Array.Sort(allPlayers, (a, b) => a.ClientId.CompareTo(b.ClientId));

                int floor = (allPlayers.Length / 8) + 1;
                for (int i = 0; i < allPlayers.Length; i++)
                {
                    PlayerModel player = allPlayers[i];
                    if (player == null) continue;

                    //배치할 위치(x/y)와 sorting order(z)를 결정

                    Vector3 targetPosition = CalculatePosition(i, floor);
                    targetPosition.z = CalculateSortingOrder(i);

                    //위치에 따라 Flip
                    player.gameObject.transform.Find("Body").GetComponent<SpriteRenderer>().flipX = !(i % 2 == 0);

                    if (player.ClientId == TrialManager.Instance.ReporterClientId)
                    {
                        SpawnReporterIndicator(targetPosition);
                    }

                    player.transform.position = targetPosition;
                }
            }
        }

        private void SpawnReporterIndicator(Vector3 playerPosition)
        {
            Vector3 spawnPos = playerPosition + new Vector3(0, -reporterIndicatorYOffset, 0);

            GameObject indicator = Instantiate(reporterIndicatorPrefab, spawnPos, Quaternion.identity);

            if (indicator.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Spawn(true);
            }
        }

        /// <summary>
        /// 재판장 배치 위치 계산
        /// </summary>
        /// <param name="floorCount">전체 층 수(1 or 2)</param>
        private Vector3 CalculatePosition(int index, int floorCount)
        {
            // 법정석 좌우 결정
            // index % 2 == 0: 왼쪽(-), index % 2 == 1: 오른쪽(+)
            float direction = (index % 2 == 0) ? -1.0f : 1.0f;

            if (direction == 1.0f)
            {
                index -= 1;
            }

            // 층 결정
            float yOffset = 0f;
            if (floorCount == 2)
            {
                bool isSecondFloor = (index / floorCount) % 2 == 1;
                yOffset = isSecondFloor ? verticalFloorHeight : -verticalFloorHeight;
            }

            // 규칙 3: 중앙으로부터의 거리 (Offset)
            // index / 4 값에 따라 멀어짐 (0,1,2,3 -> 0 / 4,5,6,7 -> 1 ...)
            float distanceMultiplier = (index / (floorCount * 2)) + 1;

            // 최종 좌표 계산
            float finalX = baseCenterPosition.x + (direction * distanceMultiplier * horizontalSpacing);
            float finalY = baseCenterPosition.y + yOffset;

            // Z축은 보통 0이나 층에 따른 우선순위용으로 사용 가능
            return new Vector3(finalX, finalY, 0);
        }

        /// <summary>
        /// 인덱스에 따라 Player의 Z축 Order를 반환
        /// </summary>
        /// 특이사항 : Sprite의 sort order 대신 Z축을 수정하는 이유는 캐릭터의 의상 때문
        /// (여기서 이미 order를 사용하고 있어서 캐릭터 간의 앞뒤는 Z축으로 결정하기로 함)
        private float CalculateSortingOrder(int index)
        {
            return (float)index / 10f;
        }

    }
}

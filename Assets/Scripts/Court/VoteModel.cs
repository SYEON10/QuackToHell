using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

//** 도메인 로직을 담당한다는 점에서 Model이나, MVP 구조를 따르지 않음

//재판장 Vote 관리 시스템
//[서버 사이드 스크립트]
//전체 재판장의 투표수, 순위, 등의 데이터를 관리 => NetworkVariable로 관리

namespace Court
{
    public class VoteModel : NetworkBehaviour
    {
        #region 싱글톤
        public static VoteModel Instance => SingletonHelper<VoteModel>.Instance;

        #endregion

        private NetworkList<VoteData> _voteDataList;

        private void Awake()
        {
            SingletonHelper<VoteModel>.InitializeSingleton(this, false);

            _voteDataList = new NetworkList<VoteData>(
                readPerm: NetworkVariableReadPermission.Everyone,
                writePerm: NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeVoteList();
            }
        }

        /// <summary>
        /// 모든 플레이어의 투표 데이터를 초기화
        /// </summary>
        private void InitializeVoteList()
        {
            _voteDataList.Clear();

            int playerSize = PlayerHelperManager.Instance.GetAllPlayers<PlayerModel>().Length;

            for (int i = 0; i < playerSize; i++)
            {
                _voteDataList.Add(new VoteData { count = 1 });
            }
        }

        //아마 투표수 변동이 있을 때 RPC를 사용해서 View 업데이트를 호출할 것 같음.
        /// <summary>
        /// 특정 플레이어 인덱스를 넣으면 현재 투표 순위를 반환 (1위부터 시작)
        /// 본인보다 더 많은 표를 받은 사람의 수만큼 순위가 밀림 = 동점자에게 동일한 순위가 부여됨
        /// </summary>
        public int GetPlayerRank(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _voteDataList.Count)
            {
                return -1;
            }

            int targetCount = _voteDataList[playerIndex].count;
            int rank = 1;

            foreach (var data in _voteDataList)
            {
                if (data.count > targetCount)
                {
                    rank++;
                }
            }

            return rank;
        }

        public int GetPlayerVoteCount(int index)
        {
            return _voteDataList[index].count;
        }

        /// <summary>
        /// 현재 투표수가 가장 높은 처형 타겟 플레이어들의 인덱스 배열을 반환(공동 1위)
        /// </summary>
        public int[] GetTopRankerIndices()
        {
            if (_voteDataList.Count == 0)
            {
                return Array.Empty<int>();
            }

            int maxCount = int.MinValue;
            List<int> topRankers = new List<int>();

            for (int i = 0; i < _voteDataList.Count; i++)
            {
                int currentCount = _voteDataList[i].count;

                if (currentCount > maxCount)
                {
                    maxCount = currentCount;
                    topRankers.Clear();
                    topRankers.Add(i);
                }
                else if (currentCount == maxCount)
                {
                    topRankers.Add(i);
                }
            }

            return topRankers.ToArray();
        }
    }
}

[System.Serializable]
public struct VoteData : INetworkSerializable, IEquatable<VoteData>
{
    public int count; //투표수

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref count);
    }

    public bool Equals(VoteData other)
    {
        return count == other.count;
    }
}
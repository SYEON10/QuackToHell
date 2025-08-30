using Unity.Netcode;
using UnityEngine.Serialization;

/// <summary>  
/// 플레이어 데이터 구조  
/// </summary>  
public enum PlayerLivingState
{
    Alive,
    Dead
}

public enum PlayerAnimationState
{
    Idle,
    Walk
}

public enum PlayerJob
{
    None,
}

[System.Serializable]
public struct PlayerStatusData : INetworkSerializable
{
    public const int MaxCredibility = 100;
    public const int MaxSpellpower = 100;

    [FormerlySerializedAs("Nickname")] public string nickname;
    [FormerlySerializedAs("Job")] public PlayerJob job;
    [FormerlySerializedAs("Credibility")] public int credibility;
    [FormerlySerializedAs("Spellpower")] public int spellpower;
    [FormerlySerializedAs("Gold")] public int gold;
    [FormerlySerializedAs("MoveSpeed")] public float moveSpeed;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (nickname == null)
            nickname = "";
        serializer.SerializeValue(ref nickname);
        serializer.SerializeValue(ref job);
        serializer.SerializeValue(ref credibility);
        serializer.SerializeValue(ref spellpower);
        serializer.SerializeValue(ref gold);
        serializer.SerializeValue(ref moveSpeed);
    }
}

[System.Serializable]
public struct PlayerStateData : INetworkSerializable
{
    [FormerlySerializedAs("AliveState")] public PlayerLivingState aliveState;
    [FormerlySerializedAs("AnimationState")] public PlayerAnimationState animationState;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref aliveState);
        serializer.SerializeValue(ref animationState);
    }
}
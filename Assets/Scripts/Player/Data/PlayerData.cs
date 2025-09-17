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
    Farmer,     // 농장주
    Animal,     // 동물
    Ghost       // 유령 (사망한 플레이어)
}

public enum EPlayerColorIndex
{
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple
}

[System.Serializable]
public struct PlayerAppearanceData : INetworkSerializable
{
    public int ColorIndex;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ColorIndex);
    }
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
    
    // Properties for compatibility with existing code
    public string Nickname 
    { 
        get => nickname; 
        set => nickname = value; 
    }
    public float MoveSpeed 
    { 
        get => moveSpeed; 
        set => moveSpeed = value; 
    }
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
    
    // Properties for compatibility with existing code
    public PlayerLivingState AliveState 
    { 
        get => aliveState; 
        set => aliveState = value; 
    }
    public PlayerAnimationState AnimationState 
    { 
        get => animationState; 
        set => animationState = value; 
    }
    
    public bool IsDead => aliveState == PlayerLivingState.Dead;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref aliveState);
        serializer.SerializeValue(ref animationState);
    }
}
using UnityEngine;

public class PlayerDeadState : NetworkStateBase
{
    public AudioSource deathSFX;
    [SerializeField]
    private SpriteRenderer[] spriteRenderers;
    [SerializeField]
    private PlayerModel playerModel;
    const float MOVE_SPEED_MULTIPLIER = 1.5f;

    public override void OnStateEnter()
    {
        //죽으면 이펙트 
        //Assets/Resources/Prefabs/FX_PF_Electricity_AreaExplosion_Blue.prefab
        GameObject effect = Resources.Load<GameObject>("Prefabs/FX_PF_Electricity_AreaExplosion_Blue");
        if (IsOwner)
        {
            Instantiate(effect,transform.position,Quaternion.identity);    
            SoundManager.Instance.SFXPlay(deathSFX.name, deathSFX.clip);
        }
    }

    public override void OnStateExit()
    {
    }

    public override void OnStateUpdate()
    {
    }
}

using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerDeadState : NetworkStateBase
{
    [SerializeField] private GameObject head;
    [SerializeField] private Animator animator;
    public AudioSource deathSFX;

    [SerializeField]
    private PlayerModel playerModel;
    
   
    public override void OnStateEnter()
    {
        head.SetActive(false);
        //애니메이션 on
        TriggerWalkAnimation();
        //죽으면 이펙트 
        //Assets/Resources/Prefabs/FX_PF_Electricity_AreaExplosion_Blue.prefab
        GameObject effect = Resources.Load<GameObject>("Prefabs/FX_PF_Electricity_AreaExplosion_Blue");
        if (IsOwner)
        {
            Instantiate(effect,transform.position,Quaternion.identity);   
            SoundManager.Instance.SFXPlay(deathSFX.name, deathSFX.clip);
        }
    }
    // 트리거 방식으로 애니메이션 제어
    public void TriggerWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }
        else
        {
            Debug.LogError("PlayerWalkState: Animator not found! Please assign in Inspector.");
        }
    }

    public override void OnStateExit()
    {
    }

    public override void OnStateUpdate()
    {
    }

}

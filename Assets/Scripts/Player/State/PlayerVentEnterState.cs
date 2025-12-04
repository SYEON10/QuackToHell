using UnityEngine;

public class PlayerVentEnterState : NetworkStateBase
{
     [SerializeField] private Animator animator;

     private PlayerPresenter presenter;
     
    public override void OnStateEnter()
    {
        animator.SetBool("IsVent",true);
        Debug.Log("Trigger Vent");
        
    }

      public override void OnStateExit()
    {
        animator.SetBool("IsVent", false);
        
    }

    public override void OnStateUpdate()
    {

    }
}

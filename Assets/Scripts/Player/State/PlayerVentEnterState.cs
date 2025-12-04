using System;
using UnityEngine;

public class PlayerVentEnterState : NetworkStateBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip ventExitAnimationClip;
    
    private PlayerPresenter presenter;
    private PlayerView myView;

    private void Start()
    {
        myView = GetComponent<PlayerView>();
    }
    public override void OnStateEnter()
    {
        animator.SetBool("IsVent",true);
        StartCoroutine(myView.SetIgnorePlayerMoveInput(true));
        Debug.Log("Trigger Vent");
        
    }

    public override void OnStateExit()
    {
        animator.SetBool("IsVent", false);
        StartCoroutine(myView.SetIgnorePlayerMoveInput(false,ventExitAnimationClip.length+0.2f));
    }

    public override void OnStateUpdate()
    {

    }
}

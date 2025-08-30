using Unity.Netcode;
using UnityEngine;

public class PlayerWalkState : State
{
    private Animator animator;
    private SpriteRenderer head;
    
    
    private NetworkVariable<bool> headFlipX = new NetworkVariable<bool>();
    
    private void Start()
    {
        animator = gameObject.transform.Find("Body").gameObject.GetComponent<Animator>();
        head = gameObject.transform.Find("Head").gameObject.GetComponent<SpriteRenderer>();
        
        // NetworkVariable 값 변경 이벤트 구독
        headFlipX.OnValueChanged += OnHeadFlipChanged;
        // 초기 값 적용
        OnHeadFlipChanged(false, headFlipX.Value);
    }

    private void OnHeadFlipChanged(bool previousValue, bool newValue)
    {
        // 모든 클라이언트에서 머리 플립 적용
        if (head != null)
        {
            head.flipX = newValue;
        }
    }

    public override void OnStateEnter()
    {
        TriggerWalkAnimation();
    }

    // 트리거 방식으로 애니메이션 제어
    public void TriggerWalkAnimation()
    {
        if (animator == null)
        {
            // animator가 null이면 다시 찾기
            animator = gameObject.transform.Find("Body").gameObject.GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            Debug.LogError("PlayerWalkState: Animator not found!");
        }
    }

    public override void OnStateExit()
    {

    }

    public override void OnStateUpdate()
    {
        if(!IsOwner) return;
        
        //*머리 default: 오른쪽 바라봄
        //왼쪽 키 누르면 머리 플립
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            FlipHeadServerRpc(true);
        }
        //오른쪽 키 누르면 머리 플립x
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            FlipHeadServerRpc(false);
        }
    }

    [ServerRpc]
    private void FlipHeadServerRpc(bool flip)
    {
        // 서버에서 머리 플립 상태 변경
        headFlipX.Value = flip;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (headFlipX != null)
        {
            headFlipX.OnValueChanged -= OnHeadFlipChanged;
        }
    }
}

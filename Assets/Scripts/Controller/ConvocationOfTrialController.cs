using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 키보드 키로 재판 소집하는 controller
/// </summary>
public class ConvocationOfTrialController : MonoBehaviour
{
    [SerializeField]
    private CircleCollider2D circleCollider2D;
    [SerializeField]
    private float detectionRadius = 1.0f;
    [SerializeField]
    private KeyCode convocationKey = KeyCode.R;
    private bool activateKey = false;
    private GameObject reporter;
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 현재 씬이 이미 로드된 상태라면 수동으로 호출
        if (SceneManager.GetActiveScene().name.Equals("VillageScene"))
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 오브젝트가 파괴되었는지 확인
        if (this == null || gameObject == null)
        {
            // 이벤트 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        if (scene.name.Equals("VillageScene"))
        {
            //콜라이더 설정
            circleCollider2D = gameObject.GetComponent<CircleCollider2D>();
            if(circleCollider2D == null)
            {
                Debug.LogError("[convocationController] CircleCollider2D component not found on ConvocationOfTrialController GameObject.");
                return;
            }
            circleCollider2D.isTrigger = true;
            circleCollider2D.radius = detectionRadius;
            //키 활성화
            activateKey = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            reporter = collision.gameObject;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            reporter = null;
        }
    }

    private void Update()
    {
        if (reporter == null)
        {
            return;
        }
        if (!activateKey)
        {
            return;
        }
        if (Input.GetKeyDown(convocationKey))
        {
            Debug.Log("Key Pressed!");
            
            // 안전한 컴포넌트 접근
            var playerView = reporter.GetComponent<PlayerView>();
            if (playerView != null)
            {
                playerView.OnReportCorpseTriggered();
            }
            else
            {
                Debug.LogWarning("[ConvocationOfTrialController] PlayerView component not found on reporter.");
            }
        }
    }
}

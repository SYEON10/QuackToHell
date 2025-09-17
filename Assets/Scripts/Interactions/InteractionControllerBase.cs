using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractionControllerBase : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] protected GameObject playerReference;
    
    protected GameObject cachedPlayer;

    protected virtual void Awake()
    {
        if (playerReference != null)
        {
            cachedPlayer = playerReference;
        }
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true; // 상호작용은 트리거 기준
    }

    public abstract bool CanInteract(GameObject player);
    public abstract void Interact(GameObject player);
}

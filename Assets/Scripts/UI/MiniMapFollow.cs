using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position + offset;
        transform.position = pos;
    }
}

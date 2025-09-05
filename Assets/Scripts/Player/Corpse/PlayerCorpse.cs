using Unity.Netcode;
using UnityEngine;

public class PlayerCorpse : NetworkBehaviour
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

private NetworkVariable<PlayerAppearanceData> _appearanceData = new NetworkVariable<PlayerAppearanceData>();
    public NetworkVariable<PlayerAppearanceData> AppearanceData { get { return _appearanceData; } }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 초기값 적용
        ApplyAppearance();

        // 값 변경 감지 등록
        if (_appearanceData != null)
        {
            _appearanceData.OnValueChanged += (previousValue, newValue) =>
            {
                ApplyAppearance();
            };
        }
    }
    private void ApplyAppearance()
    {
        //색깔
        SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            if (sr.gameObject.name.Contains("Body"))
            {
                int colorIndex = AppearanceData.Value.ColorIndex;
                // 모든 클라이언트에서 색상 변경 적용
                switch (colorIndex)
                {
                    case 0:
                        sr.color = Color.red;
                        break;
                    case 1:
                        sr.color = Color.orange;
                        break;
                    case 2:
                        sr.color = Color.yellow;
                        break;
                    case 3:
                        sr.color = Color.green;
                        break;
                    case 4:
                        sr.color = Color.blue;
                        break;
                    case 5:
                        sr.color = Color.purple;
                        break;
                }

            }
        }
    }


}

using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class VillageUI : UIHUD
{
    enum Images
    {
        Role_Image
    }

    private void Start()
    {
        base.Init();
        
        Bind<Image>(typeof(Images));
        GameObject Image_Role_gameObject = Get<Image>((int)Images.Role_Image).gameObject;
        if (PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId)
                .GetPlayerJob() == PlayerJob.Animal)
        {
            Image_Role_gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Art/Duck");    
        }
        else
        {
            Image_Role_gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Art/Farmer");    
        }
        
    }

}

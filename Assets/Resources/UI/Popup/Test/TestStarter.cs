using System;
using UnityEngine;
using UnityEngine.UI;
public class TestStarter : MonoBehaviour
{
    private float timer = 0f;
    private bool blockTimer = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIManager.Instance.ShowPopupUI<UIPopupTest>("TestPopup");
        UIManager.Instance.ShowPopupUI<UIPopupTest>("TestPopup2");
        UIManager.Instance.ShowHUDUI<UIInven>("SceneUITest");
        UIManager.Instance.ShowHUDUI<UIInven>("SceneUITest2");
        
    }

    private void Update()
    {

        if (blockTimer)
        {
            return;
        }
        timer+=Time.deltaTime;
        Debug.Log(timer);
        if (timer > 5f)
        {
            blockTimer = true;
            UIManager.Instance.ClosePopupUI();
        }
    }
}

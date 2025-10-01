using UnityEngine;

/// <summary>
/// 컴포지션 패턴을 사용한 싱글톤 헬퍼 클래스
/// NetworkBehaviour를 상속하는 클래스에서도 싱글톤 기능을 사용할 수 있도록 함
/// </summary>
/// <typeparam name="T">싱글톤으로 만들고자 하는 타입</typeparam>
public static class SingletonHelper<T> where T : MonoBehaviour
{
    private static T _instance;
    
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }
    
    /// <summary>
    /// 싱글톤 초기화를 위한 Awake 메서드
    /// NetworkBehaviour를 상속하는 클래스의 Awake에서 호출
    /// </summary>
    /// <param name="self">싱글톤으로 만들 객체</param>
    public static void InitializeSingleton(T self, bool dontDestroyOnLoad = true)
    {
        if (_instance == null)
        {
            _instance = self;
            if (dontDestroyOnLoad)
            {
                Object.DontDestroyOnLoad(self.gameObject);
            }
        }
        else if (_instance != self)
        {
            Object.Destroy(self.gameObject);
        }
    }
    
}

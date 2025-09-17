using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 디버그 및 검증을 위한 유틸리티 클래스
/// </summary>
public static class DebugUtils
{
    /// <summary>
    /// 객체가 null이 아닌지 확인하고, null이면 에러 로그 출력 (Editor에서만)
    /// </summary>
    /// <param name="obj">확인할 객체</param>
    /// <param name="objectName">객체 이름 (디버그용)</param>
    /// <param name="context">호출 컨텍스트 (선택사항)</param>
    /// <returns>null이 아니면 true, null이면 false</returns>
    public static bool EnsureNotNull(object obj, string objectName, Object context = null)
    {
        if (obj != null) return true;
        
        #if UNITY_EDITOR
        string message = $"{objectName} is null!";
        if (context != null)
        {
            Debug.LogError(message, context);
        }
        else
        {
            Debug.LogError(message);
        }
        #endif
        return false;
    }
    
    /// <summary>
    /// 객체가 null이 아닌지 확인하고, null이면 에러 로그 출력 후 false 반환
    /// </summary>
    /// <param name="obj">확인할 객체</param>
    /// <param name="objectName">객체 이름 (디버그용)</param>
    /// <param name="context">호출 컨텍스트 (선택사항)</param>
    /// <returns>null이 아니면 true, null이면 false</returns>
    public static bool AssertNotNull(object obj, string objectName, Object context = null)
    {
        if (obj != null) return true;
        
        string message = $"{objectName} is null!";
        if (context != null)
        {
            Debug.LogError(message, context);
        }
        else
        {
            Debug.LogError(message);
        }
        return false;
    }
    
    /// <summary>
    /// 조건이 true인지 확인하고, false이면 에러 로그 출력 (Editor에서만)
    /// </summary>
    /// <param name="condition">확인할 조건</param>
    /// <param name="message">에러 메시지</param>
    /// <param name="context">호출 컨텍스트 (선택사항)</param>
    /// <returns>조건이 true면 true, false면 false</returns>
    public static bool Ensure(bool condition, string message, Object context = null)
    {
        if (condition) return true;
        
        #if UNITY_EDITOR
        if (context != null)
        {
            Debug.LogError(message, context);
        }
        else
        {
            Debug.LogError(message);
        }
        #endif
        return false;
    }
    
    /// <summary>
    /// 조건이 true인지 확인하고, false이면 에러 로그 출력 후 false 반환
    /// </summary>
    /// <param name="condition">확인할 조건</param>
    /// <param name="message">에러 메시지</param>
    /// <param name="context">호출 컨텍스트 (선택사항)</param>
    /// <returns>조건이 true면 true, false면 false</returns>
    public static bool Assert(bool condition, string message, Object context = null)
    {
        if (condition) return true;
        
        if (context != null)
        {
            Debug.LogError(message, context);
        }
        else
        {
            Debug.LogError(message);
        }
        return false;
    }
    
    /// <summary>
    /// 컴포넌트가 존재하는지 확인하고, 없으면 에러 로그 출력
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <param name="component">확인할 컴포넌트</param>
    /// <param name="objectName">객체 이름 (디버그용)</param>
    /// <param name="context">호출 컨텍스트 (선택사항)</param>
    /// <returns>컴포넌트가 존재하면 true, 없으면 false</returns>
    public static bool AssertComponent<T>(T component, string objectName, Object context = null) where T : Component
    {
        if (component != null) return true;
        
        string message = $"{objectName} does not have {typeof(T).Name} component!";
        if (context != null)
        {
            Debug.LogError(message, context);
        }
        else
        {
            Debug.LogError(message);
        }
        return false;
    }
    
    /// <summary>
    /// 배열이 비어있지 않은지 확인
    /// </summary>
    /// <param name="array">확인할 배열</param>
    /// <param name="arrayName">배열 이름 (디버그용)</param>
    /// <param name="context">호출 컨텍스트 (선택사항)</param>
    /// <returns>배열이 비어있지 않으면 true, 비어있으면 false</returns>
    public static bool AssertNotEmpty(System.Array array, string arrayName, Object context = null)
    {
        if (array != null && array.Length > 0) return true;
        
        string message = $"{arrayName} is null or empty!";
        if (context != null)
        {
            Debug.LogError(message, context);
        }
        else
        {
            Debug.LogError(message);
        }
        return false;
    }
}

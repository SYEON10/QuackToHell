using UnityEngine;

public static class MathUtils 
{
    // 배열 섞기 (Fisher-Yates 알고리즘)
    public static void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
        }
    }
}

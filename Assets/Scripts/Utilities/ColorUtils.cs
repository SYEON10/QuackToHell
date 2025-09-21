using UnityEngine;

public static class ColorUtils
{
    public static Color GetColorByIndex(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0:
                return Color.red;
            case 1:
                return Color.orange;
            case 2:
                return Color.yellow;
            case 3:
                return Color.green;
            case 4:
                return Color.blue;
            case 5:
                return Color.purple;
        }
        return Color.white;
    }
 
}

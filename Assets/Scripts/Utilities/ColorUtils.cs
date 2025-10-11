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
                return  new Color(1f, 0.647f, 0f);
            case 2:
                return Color.yellow;
            case 3:
                return Color.green;
            case 4:
                return Color.blue;
            case 5:
                return new Color(0.502f, 0f, 0.502f); 
        }
        return Color.white;
    }
 
}

using UnityEngine;

[CreateAssetMenu(fileName = "NewColorTheme", menuName = "Color Theme", order = 60)]
public class ColorTheme : ScriptableObject
{
    public string themeName;

    [Header("Background")]
    public Color cameraBackgroundColor;

    [Header("Panels")]
    public Color backgroundPanelColor;
    public Color menuPanelColor;
    public Color menuBackgroundPanelColor;

    [Header("Visuals")]
    public Color arrowColor;
}

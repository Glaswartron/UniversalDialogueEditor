using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ColorThemedImage : MonoBehaviour, IColorThemed
{
    public enum ImageType
    {
        BACKGROUND_PANEL, MENU_PANEL, MENU_BACKGROUND_PANEL
    }

    public ImageType imageType;
    private Image image;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
    }

    public void ChangeTheme(ColorTheme newTheme)
    {
        if (image == null)
            image = GetComponent<Image>();

        switch (imageType)
        {
            case ImageType.BACKGROUND_PANEL:
                image.color = newTheme.backgroundPanelColor;
                break;
            case ImageType.MENU_PANEL:
                image.color = newTheme.menuPanelColor;
                break;
            case ImageType.MENU_BACKGROUND_PANEL:
                image.color = newTheme.menuBackgroundPanelColor;
                break;
        }
    }
}

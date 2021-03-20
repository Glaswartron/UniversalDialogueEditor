using UnityEngine;
using UnityEngine.UI;

public enum Language
{
    EN, DE
}

[RequireComponent(typeof(Button))]
public class LanguageButton : MonoBehaviour
{
    public static Language currentLang;

    public Sprite firstImage;
    public Sprite secondImage;

    public Image image;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Click);
    }

    /// <summary>
    /// Called when the button is clicked
    /// </summary>
    private void Click()
    {
        if (image.sprite == firstImage) // EN
        {
            image.sprite = secondImage;
            currentLang = Language.DE;
        }
        else if (image.sprite == secondImage) // DE
        {
            image.sprite = firstImage;
            currentLang = Language.EN;
        }

        //EditorManager.instance.UpdateInputFields();
    }
}

using UnityEngine;
using UnityEngine.UI;

public class ExtendedColorToggle : Toggle
{
    private readonly Color colorTrue = new Color(0x4A, 0xFF, 0x3B);
    private readonly Color colorFalse = new Color(0xFF, 0x5F, 0x67);

    private Image toggleBackground;

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();

        toggleBackground = GetComponentInChildren<Image>(); // A bit trippy

        onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(bool val)
    {
        toggleBackground.color = val ? colorTrue : colorFalse;
    }
}

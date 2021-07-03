using UnityEngine;
using UnityEngine.UI;

public class ExtendedColorToggle : Toggle
{
    // Hardcoded
    private readonly Color colorTrue = new Color(0x4A, 0xFF, 0x3B);
    private readonly Color colorFalse = new Color(0xFF, 0x5F, 0x67);

    private Image toggleBackground;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        toggleBackground = GetComponentInChildren<Image>(); // A bit trippy

        onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(bool val)
    {
        // TODO: Doesn't work at the moment due to a Unity issue!
        toggleBackground.color = val ? colorTrue : colorFalse;
    }
}

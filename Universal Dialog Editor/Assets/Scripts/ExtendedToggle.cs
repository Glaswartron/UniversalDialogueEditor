using UnityEngine;

public class ExtendedToggle : UnityEngine.UI.Toggle
{
    public bool deselectOnUnrelatedClick;
    public RectTransform[] relatedUIElements;

    private RectTransform toggleGroup;

    private void Update()
    {
        if (isOn && deselectOnUnrelatedClick)
        {
            if (toggleGroup == null)
                toggleGroup = group.GetComponent<RectTransform>();

            if (Input.GetMouseButtonDown(0))
            {
                /* Check where the user has clicked using rect.Contains
                 * for the toggle group and all relatedUIElements. Note
                 * that the rect of a rectTransform is in local space,
                 * so the mousePosition has to be transformed into its
                 * local space as well */

                bool overToggleGroup = toggleGroup.rect.Contains
                    (toggleGroup.InverseTransformPoint(Input.mousePosition));
                
                if (!overToggleGroup)
                {
                    bool overRelatedUI = false;
                    foreach (RectTransform rt in relatedUIElements)
                    {
                        overRelatedUI 
                            |= rt.rect.Contains(rt.InverseTransformPoint(Input.mousePosition));
                    }

                    if (!overRelatedUI)
                    {
                        /* Deselect in case the user has clicked anywhere
                         * but on the toggles toggle group or one of the
                         * relatedUIElements */

                        isOn = false;
                    }
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility : MonoBehaviour
{
    /// <summary>
    /// Checks whether the mouse is over an UI RectTransform
    /// </summary>
    /// <param name="uiRect">The UI Element which the mouse might be over</param>
    /// <returns>Whether (true) or not (false) the mouse position lies within the
    /// given RectTransforms Rect</returns>
    public static bool IsMouseOverUI(RectTransform uiRect)
    {
        return GetWorldRect(uiRect).Contains(Input.mousePosition);
    }

    /// <summary>
    /// Gets the rect of a rectTransform in world space
    /// </summary>
    /// <param name="rectTransform">The rectTransform whose world space 
    /// rect shall be determined</param>
    /// <returns>The rect of the rectTransform in world space</returns>
    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        // Get the bottom left corner.
        Vector3 position = corners[0];

        Vector2 size = new Vector2(
            rectTransform.lossyScale.x * rectTransform.rect.size.x,
            rectTransform.lossyScale.y * rectTransform.rect.size.y);

        return new Rect(position, size);
    }
}

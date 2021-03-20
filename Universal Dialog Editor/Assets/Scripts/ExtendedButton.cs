using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ExtendedButton : UnityEngine.UI.Button
{
    public UnityEvent onDeselect;

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);

        onDeselect.Invoke();
    }
}

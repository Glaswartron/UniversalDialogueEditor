using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionCollider : MonoBehaviour, IContextMenu
{
    public void ShowContextMenu(ContextMenuManager menuManager)
    {
        menuManager.AddButton("Delete Connection",
            () =>
            {
                // Triggers Connection's OnDestroy, where cleanup is done
                Destroy(transform.parent.gameObject);
            });
    }
}

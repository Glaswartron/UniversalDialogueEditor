using System.Collections.Generic;
using UnityEngine;

public class Connection : MonoBehaviour, IContextMenu
{
    [Header("Connection")]
    public DialoguePartVisual fromDP;
    public AnswerVisual fromA;

    public DialoguePartVisual toDP;

    [Header("Visuals")]
    [Space(5)]
    public Transform arrowTip;

    [HideInInspector] public bool collSet;
    [HideInInspector] public bool dontUpdateConnectedVisual;

    private LineRenderer lineRenderer;
    private PolygonCollider2D polygonCollider;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();

        UpdateColor();
    }

    private void FixedUpdate()
    {
        if (!collSet)
        {
            var firstPos = lineRenderer.GetPosition(0);
            var secondPos = lineRenderer.GetPosition(1);

            // Calculate and set the points for the polygon collider
            polygonCollider.SetPath(0, CalculateColliderPoints(new Vector2[] { firstPos, secondPos }));

            // Place tip triangle
            Vector2 firstPosToSecondPos = secondPos - firstPos;
            float angle = Mathf.Atan2(firstPosToSecondPos.y, firstPosToSecondPos.x) * Mathf.Rad2Deg;
            Vector2 newPos = (Vector2)secondPos - firstPosToSecondPos.normalized * 0.61f;

            Quaternion rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

            arrowTip.transform.SetPositionAndRotation(newPos, rotation);

            collSet = true;
        }
    }

    /// <summary>
    /// https://www.youtube.com/watch?v=BfP0KyOxVWs
    /// </summary>
    private Vector2[] CalculateColliderPoints(Vector2[] positions)
    {
        // Get The Width of the Line
        float width = lineRenderer.startWidth;

        // m = (y2 - y1) / (x2 - x1)
        float m = (positions[1].y - positions[0].y) / (positions[1].x - positions[0].x);
        float deltaX = (width / 2f) * (m / Mathf.Pow(m * m + 1, 0.5f));
        float deltaY = (width / 2f) * (1 / Mathf.Pow(1 + m * m, 0.5f));
        
        // Calculate Vertex Offset from Line Point
        Vector2[] offsets = new Vector2[2];
        offsets[0] = new Vector2(-deltaX, deltaY);
        offsets[1] = new Vector2(deltaX, -deltaY);

        List<Vector2> colliderPoints = new List<Vector2> {
            positions[0] + offsets[0],
            positions[1] + offsets[0],
            positions[1] + offsets[1],
            positions[0] + offsets[1]
        };

        return colliderPoints.ToArray();
    }

    private void OnDestroy()
    {
        if (dontUpdateConnectedVisual)
        {
            dontUpdateConnectedVisual = false;
            return;
        }

        if (fromDP != null)
            fromDP.ConnectedDP = null;

        if (fromA != null)
            fromA.ConnectedDP = null;
    }

    public void ShowContextMenu(ContextMenuManager menuManager)
    {
        menuManager.AddButton("Delete Connection",
            () =>
            {
                // Triggers OnDestroy, where cleanup is done
                Destroy(this.gameObject);
            });
    }

    public void UpdateColor()
    {
        arrowTip.GetComponent<SpriteRenderer>().color = EditorManager.instance.ActiveColorTheme.arrowColor;
        lineRenderer.startColor = EditorManager.instance.ActiveColorTheme.arrowColor;
        lineRenderer.endColor = EditorManager.instance.ActiveColorTheme.arrowColor;
    }

    public void DontUpdateConnectedVisual()
        => dontUpdateConnectedVisual = true;

}

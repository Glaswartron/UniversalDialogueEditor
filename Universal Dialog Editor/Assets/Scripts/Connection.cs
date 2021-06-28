using UnityEngine;

public class Connection : MonoBehaviour
{
    [Header("Connection")]
    public DialogPartVisual fromDP;
    public AnswerVisual fromA;

    public DialogPartVisual toDP;

    [Header("Visuals")]
    [Space(5)]
    public Transform arrowTip;

    [HideInInspector] public bool collSet;
    [HideInInspector] public bool dontUpdateConnectedVisual;

    private LineRenderer lineRenderer;
    private CircleCollider2D coll;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        coll = transform.GetChild(0).GetComponent<CircleCollider2D>();
    }

    private void FixedUpdate()
    {
        if (!collSet)
        {
            var firstPos = lineRenderer.GetPosition(0);
            var secondPos = lineRenderer.GetPosition(1);

            coll.transform.position =
                0.5f * (firstPos + secondPos);

            coll.radius = Vector2.Distance(secondPos, firstPos) / 4;

            // Place tip triangle
            Vector2 firstPosToSecondPos = secondPos - firstPos;
            float angle = Mathf.Atan2(firstPosToSecondPos.y, firstPosToSecondPos.x) * Mathf.Rad2Deg;
            Vector2 newPos = (Vector2)secondPos - firstPosToSecondPos.normalized * 0.6f;
            arrowTip.transform.position = newPos;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            collSet = true;
        }
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

    public void DontUpdateConnectedVisual()
        => dontUpdateConnectedVisual = true;

}

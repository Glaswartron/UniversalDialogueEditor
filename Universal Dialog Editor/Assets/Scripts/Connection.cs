using UnityEngine;

public class Connection : MonoBehaviour
{
    public DialogPartVisual fromDP;
    public AnswerVisual fromA;

    public DialogPartVisual toDP;

    private LineRenderer lineRenderer;
    private GameObject coll;

    bool collSet;

    bool dontUpdateConnectedVisual;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        coll = transform.GetChild(0).gameObject;
    }

    private void Update()
    {
        if (!collSet)
        {
            var firstPos = lineRenderer.GetPosition(0);
            var secondPos = lineRenderer.GetPosition(1);

            coll.transform.position =
                0.5f * (firstPos + secondPos);

            coll.GetComponent<CircleCollider2D>().radius = 
                Vector2.Distance(secondPos, firstPos) / 4;

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

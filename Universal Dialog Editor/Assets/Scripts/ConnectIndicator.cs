using UnityEngine;
using UnityEngine.UI;

public class ConnectIndicator : MonoBehaviour
{
    Image image;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        image.color = EditorManager.instance.inConnectMode ? Color.green : Color.grey;
    }
}

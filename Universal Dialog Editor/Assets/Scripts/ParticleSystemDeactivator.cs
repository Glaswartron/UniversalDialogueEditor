using System.Collections;
using UnityEngine;

public class ParticleSystemDeactivator : MonoBehaviour
{
    public float duration = 2f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DeactivateCo());
    }

    private IEnumerator DeactivateCo()
    {
        yield return new WaitForSeconds(duration);
        Destroy(this.gameObject);
    }
}

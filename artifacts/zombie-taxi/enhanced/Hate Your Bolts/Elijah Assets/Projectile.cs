using UnityEngine;

public class Projectile : MonoBehaviour
{
    float lifeTimer = 10.0f;

    private void Update()
    {
        lifeTimer -= 1 * Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Destroy(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit Target");
        Destroy(this);
    }
}

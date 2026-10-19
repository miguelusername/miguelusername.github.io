using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public int damage;
    float timer = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Zombie>())
        {
            other.GetComponent<Zombie>().zombieHealth -= damage;
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
            Destroy(gameObject);
    }
}

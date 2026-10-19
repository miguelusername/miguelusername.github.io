using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float shakeTime = 0f;
    private float shakeAmount = 0f;

    private void Awake()
    {
        originalPos = transform.localPosition;
    }

    public void Shake(float duration, float intensity)
    {
        shakeTime = duration;
        shakeAmount = intensity;
    }

    private void Update()
    {

        if(shakeTime > 0)
        {
            Vector3 offset = new Vector3(Random.Range(-1f, 1f),
                Random.Range(-1f, 1f), 0f) * shakeAmount;

            transform.localPosition = originalPos + offset;

            shakeTime -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class PassengerPointerUI : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public RectTransform arrow;
    public Transform target;//passenger or dropoff
    public float screenEdgePadding = 60f;

    private void Awake()
    {
        if (!arrow)
            arrow = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!cam) 
            cam = Camera.main;

        if (!target || !cam)
        {
            if (arrow) arrow.gameObject.SetActive(false);
            return;
        }

        arrow.gameObject.SetActive(true);

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        //If behind camera, flip
        if (screenPos.z < 0)
        {
            screenPos *= -1;
        }

        //Clamp to screen edges
        float x = Mathf.Clamp(screenPos.x, screenEdgePadding, Screen.width - screenEdgePadding);
        float y = Mathf.Clamp(screenPos.y, screenEdgePadding, Screen.height - screenEdgePadding);
        Vector2 clamped = new Vector2(x, y);

        arrow.position = clamped;

        //Rotate arrow to point toward the target direction from screen center
        Vector2 fromCenter = clamped - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
        arrow.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}

using UnityEngine;
using UnityEngine.UI;

public class GasPointerUI : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public RectTransform arrow;
    public Transform target; //gas station
    public float screenEdgePadding = 60f;
    [SerializeField] private RawImage iconImage;
    [SerializeField] private S_TaxiPlayer_Test player;

    private void Awake()
    {
        if(!arrow)
            arrow = GetComponent<RectTransform>();
        if (!iconImage)
            iconImage = GetComponent<RawImage>();
        if (!player)
            player = FindFirstObjectByType<S_TaxiPlayer_Test>();
    }

    void Update()
    {
        if(!cam)
            cam = Camera.main;

        if(!target || !cam)
        {
            if (arrow) arrow.gameObject.SetActive(false);
            return;
        }

        arrow.gameObject.SetActive(true);

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        if (screenPos.z < 0)
        {
            screenPos *= -1;
        }

        float x = Mathf.Clamp(screenPos.x, screenEdgePadding, Screen.width - screenEdgePadding);
        float y = Mathf.Clamp(screenPos.y, screenEdgePadding, Screen.height - screenEdgePadding);
        Vector2 clamped = new Vector2(x, y);

        arrow.position = clamped;

        Vector2 fromCenter = clamped - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
        //arrow.rotation = Quaternion.Euler(0, 0, angle - 90f);

        UpdateOpacity();
    }

    private void UpdateOpacity()
    {
        if(iconImage == null || player == null) return;

        float fuelPercent = player.fuelTank / player.fuelTankMax;

        //Invert it: full = 0 alpha, empty = 1 alpha
        float threshold = 0.7f;
        float t = Mathf.InverseLerp(threshold, 0f, fuelPercent);
        float alpha = Mathf.Clamp01(t);
        alpha = Mathf.SmoothStep(0f, 1f, alpha);

        Color c = iconImage.color;
        c.a = alpha;
        iconImage.color = c;
    }
}

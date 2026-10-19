using UnityEngine;
using TMPro;

public class CompassUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private S_TaxiPlayer_Test taxi;
    [SerializeField] private TextMeshProUGUI compassUIText;
    [SerializeField] private RectTransform compassRect;

    [Header("Display")]
    [SerializeField] private int visibleChars = 13;

    [Tooltip("Width in UI units of one compass character. Tweak until smooth.")]
    [SerializeField] private float characterWidth = 18f;

    [Tooltip("Optional center marker text, like ^ or |")]
    [SerializeField] private TextMeshProUGUI centerMarker;

    //Keep the dashed style
    private string compassChars = "- - - N - - - E - - - S - - - W ";

    private Vector2 baseAnchoredPos;

    private void Awake()
    {
        if (!taxi)
            taxi = FindFirstObjectByType<S_TaxiPlayer_Test>();

        if (!compassUIText)
            compassUIText = GetComponent<TextMeshProUGUI>();

        if (!compassRect && compassUIText)
            compassRect = compassUIText.rectTransform;

        if (compassRect)
            baseAnchoredPos = compassRect.anchoredPosition;
    }

    private void Update()
    {
        if (!taxi || !compassUIText || !compassRect) return;

        float yaw = taxi.transform.eulerAngles.y;

        float normalized = yaw / 360f;
        int totalLength = compassChars.Length;

        float exactIndex = normalized * totalLength;
        int wholeIndex = Mathf.FloorToInt(exactIndex);
        float fractional = exactIndex - wholeIndex;

        wholeIndex = WrapIndex(wholeIndex, totalLength);

        //Grab a couple extra chars so sliding looks smooth
        int displayCount = visibleChars + 2;

        string wrapped = compassChars + compassChars + compassChars;
        int start = totalLength + wholeIndex - (displayCount / 2);

        compassUIText.text = wrapped.Substring(start, displayCount);

        //Shift text based on fractional part between characters
        Vector2 pos = baseAnchoredPos;
        pos.x = baseAnchoredPos.x - (fractional * characterWidth);
        compassRect.anchoredPosition = pos;

        if (centerMarker)
            centerMarker.text = "|";
    }

    private int WrapIndex(int index, int length)
    {
        return (index % length + length) % length;
    }
}

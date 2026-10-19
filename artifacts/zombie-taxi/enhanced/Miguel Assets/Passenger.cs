using UnityEngine;

public class Passenger : MonoBehaviour
{
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private GameObject targetMarker;

    private Transform player;
    private bool isTargetPassenger;

    [SerializeField] private GameObject targetMarkerPrefab;
    [SerializeField] private Vector3 markerOffset = new Vector3(0, 2.5f, 0);

    private GameObject _marker;
    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj)
            player = playerObj.transform;
        else
            Debug.LogError("Player with tag not found.");
    }
    void Update()
    {
        if (!isTargetPassenger) return;   // only target rotates

        if (!player) return;

        //Direction the camera is facing
        Vector3 lookDir = player.forward;
        lookDir.y = 0f; //keep them upright

        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(-lookDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );
    }

    public void SetAsTarget(bool isTarget)
    {
        if (isTarget)
        {
            if (_marker == null && targetMarkerPrefab != null)
                _marker = Instantiate(targetMarkerPrefab);

            if (_marker != null)
            {
                _marker.SetActive(true);
                _marker.transform.position = transform.position + markerOffset;
            }
        }
        else
        {
            if (_marker != null) _marker.SetActive(false);
        }
    }
    public void OnPickedUp()
    {
        if (_marker != null) _marker.SetActive(false);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_marker != null && _marker.activeSelf)
            _marker.transform.position = transform.position + markerOffset;
    }
}

using UnityEngine;

public class CameraRigFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform anchor;
    [SerializeField] private S_TaxiPlayer_Test taxi;

    [Header("Position (world)")]
    [SerializeField] private float positionLerp = 8f;

    [Header("Yaw follow")]
    [SerializeField] private float yawLerp = 10f;

    [Header("Roll (floaty feel)")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float rollTurnDeg = 3f;
    [SerializeField] private float rollDriftExtraDeg = 2f;
    [SerializeField] private float rollLerp = 8f;

    private float _roll;

    private void Awake()
    {
        if (!cameraPivot) cameraPivot = transform.Find("CameraPivot");

        if (!anchor)
        {
            var player = GameObject.FindWithTag("Player");
            if (player) anchor = player.transform.Find("CameraAnchor");
        }

        if (anchor && !taxi) taxi = anchor.GetComponentInParent<S_TaxiPlayer_Test>();
    }

    private void LateUpdate()
    {
        if (!anchor) return;

        float dt = Time.deltaTime;

        //Follow anchor position (world)
        transform.position = Vector3.Lerp(
            transform.position,
            anchor.position,
            1f - Mathf.Exp(-positionLerp * dt)
        );

        //Follow anchor yaw only (keeps camera stable)
        float targetYaw = anchor.eulerAngles.y;
        float newYaw = Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, 1f - Mathf.Exp(-yawLerp * dt));
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

        //Floaty roll on the pivot (so it won't mess up yaw)
        if (cameraPivot)
        {
            float steer = taxi ? Mathf.Clamp(taxi.Steer01, -1f, 1f) : 0f;
            bool drifting = taxi ? taxi.IsDrifting : false;

            float targetRoll = -steer * rollTurnDeg;
            if (drifting) targetRoll += -steer * rollDriftExtraDeg;

            _roll = Mathf.Lerp(_roll, targetRoll, 1f - Mathf.Exp(-rollLerp * dt));

            //Roll around local Z only. VERY important
            cameraPivot.localRotation = Quaternion.Euler(0f, 0f, _roll);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CarMovement : MonoBehaviour
{
    PlayerControls controls;
    Camera cam;

    Vector2 move;
    Vector2 cameraRotation;

    [Header("Speed")]
    public Vector3 velocity; // X value is speed
    public float turnSmooth = 0.1f;
    float turnSmoothVelocity;
    public float fuelTank;
    public float fuelTankMax;
    public float maxSpeed;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    public bool isGrounded;
    public LayerMask groundMask;
    public int gravityForce;
    UnityAction OnGrounded;

    [Header("UI")]
    [SerializeField] Slider fuelGauge;
    [SerializeField] Transform speedGauge;
    UnityAction FuelChange;
    UnityAction SpeedChange;

    [Header("Slope")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;

    private void Awake()
    {
        controls = new PlayerControls();

        cam = GetComponentInChildren<Camera>();

        controls.Gameplay.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => move = Vector3.zero;

        fuelTank = fuelTankMax;
        FuelChange += () =>
        {
            fuelGauge.value = fuelTank;
        };
        SpeedChange += () =>
        {
            if (velocity.x <= 0)
                speedGauge.rotation = Quaternion.Euler(0f, 0f, 90f);
        };
        OnGrounded += () =>
        {
            velocity.y = 0;
        };
    }

    private void Update()
    {
        // Gravity
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.5f, groundMask);
        if (!isGrounded && !OnSlope())
        {
            velocity.y -= gravityForce * Time.deltaTime;
        }
        else OnGrounded?.Invoke();

        float targetAngle = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmooth);

        transform.Translate(new Vector3(0, velocity.y, 0), Space.World); // move with y velocity
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f).normalized * Vector3.forward * ChangeVelocity() * Time.deltaTime; // move forward with camera
        Vector3 slopeAngle = Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized * ChangeVelocity() * Time.deltaTime;


        if (fuelTank > 0 && move.sqrMagnitude > 0)
        {
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            if (OnSlope())
            {
                transform.Translate(slopeAngle, Space.World);
            }
            else transform.Translate(moveDir, Space.World);

            fuelTank -= 1 * Time.deltaTime;
            FuelChange?.Invoke();
        }
    }

    float ChangeVelocity()
    {
        if (velocity.x < maxSpeed && fuelTank > 0)
        {
            if (move.sqrMagnitude > 0.1f)
                velocity.x += 1f * Time.deltaTime;
            if (move.sqrMagnitude > 0.5f)
                velocity.x += 3f * Time.deltaTime;
            if (move.sqrMagnitude > 1f)
                velocity.x += 5f * Time.deltaTime;
        }
        if (move.sqrMagnitude <= 0 && velocity.x > 0)
            velocity.x -= 5f * Time.deltaTime;
        if (velocity.x < 0)
            velocity.x = 0;
        SpeedChange?.Invoke();

        return velocity.x;
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, transform.position.x + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }


    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }
}

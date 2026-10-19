using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MechMovement : MonoBehaviour
{
    PlayerControls controls;
    Camera cam;
    public Animator animator;
    int VelocityHash;

    [Header("Gadgets")]
    public bool leftShoulderActive;
    public bool rightShoulderActive;
    public bool leftArmActive;
    public bool rightArmActive;
    public bool dashActive;
    //public bool jumpActive;

    [Header("Movement")]
    Vector2 move;
    public Vector3 velocity; // X value is speed
    public float turnSmooth = 0.1f;
    float turnSmoothVelocity;
    //public float jumpForce = 5;
    //bool isJumping;

    [Header("Dashing")]
    public bool isDashing;
    public float dashSpeed = 10;
    public float dashTimer = 0.3f;
    float tempTimer;

    [Header("Slope")]
    public float maxSlopeAngle = 40;
    private RaycastHit slopeHit;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    public bool isGrounded;
    public LayerMask groundMask;
    public int gravityForce = 3;
    UnityAction OnGrounded;

    //[Header("UI")]

    [Header("Cannons")]
    [SerializeField] GameObject cannonPrefab;
    [SerializeField] Transform leftShoulderPositiion;
    [SerializeField] Transform rightShoulderPositiion;
    [SerializeField] Transform leftArmPositiion;
    [SerializeField] Transform rightArmPositiion;

    [Header("Lock On")]
    public GameObject[] playerMechs;
    public bool lockedOn;
    public GameObject lockOnTarget = null;


    private void Awake()
    {
        VelocityHash = Animator.StringToHash("Velocity");
        controls = new PlayerControls();
        cam = GetComponentInChildren<Camera>();

        controls.HateYourBolts.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.HateYourBolts.Move.canceled += ctx => move = Vector3.zero;
        controls.HateYourBolts.LockOn.performed += ctx => LockOnToPlayer();
        controls.HateYourBolts.Dash.performed += ctx => isDashing = true;
        //controls.HateYourBolts.Jump.performed += ctx => isJumping = true;

        OnGrounded += () =>
        {
            velocity.y = 0;
        };

        playerMechs = GameObject.FindGameObjectsWithTag("MechPlayer");
        for (int idx = 0; idx < playerMechs.Length; idx++)
        {
            if (playerMechs[idx] == this)
                playerMechs[idx] = null;
        }

        if (leftShoulderActive)
            Instantiate(cannonPrefab, leftShoulderPositiion.position, leftShoulderPositiion.rotation, leftShoulderPositiion).AddComponent<LeftShoulder>();
        if (rightShoulderActive)
            Instantiate(cannonPrefab, rightShoulderPositiion.position, rightShoulderPositiion.rotation, rightShoulderPositiion).AddComponent<RightShoulder>();
        if (leftArmActive)
            Instantiate(cannonPrefab, leftArmPositiion.position, leftArmPositiion.rotation, leftArmPositiion).AddComponent<LeftArm>();
        if (rightArmActive)
            Instantiate(cannonPrefab, rightArmPositiion.position, rightArmPositiion.rotation, rightArmPositiion).AddComponent<RightArm>();

        tempTimer = dashTimer;
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

        /*if (jumpActive && isJumping && isGrounded)
        {
            Debug.Log("JUMP");
            velocity.y += jumpForce * Time.deltaTime;
            isJumping = false;
        }*/

        float targetAngle = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmooth);

        transform.Translate(new Vector3(0, velocity.y, 0), Space.World); // move with y velocity
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f).normalized * Vector3.forward * velocity.x * Time.deltaTime; // move forward with camera
        Vector3 slopeAngle = Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized * velocity.x * Time.deltaTime;


        if (lockedOn)
            transform.LookAt(lockOnTarget.transform.position);
        else transform.rotation = Quaternion.Euler(0f, angle, 0f);

        if (move.sqrMagnitude > 0)
        {
            animator.SetFloat(VelocityHash, velocity.x);

            if (OnSlope())
            {
                transform.Translate(slopeAngle, Space.World);
            }
            else transform.Translate(moveDir, Space.World);

            if (dashActive)
            {
                if (isDashing && dashTimer > 0)
                {
                    transform.Translate(moveDir * dashSpeed, Space.World);
                    dashTimer -= 1 * Time.deltaTime;
                }
                else
                {
                    isDashing = false;
                    dashTimer = tempTimer;
                }
            }
        }
        else animator.SetFloat(VelocityHash, 0);

    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, transform.position.x + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    void LockOnToPlayer()
    {
        for (int idx = 0; idx < playerMechs.Length; idx++)
        {
            if (playerMechs[idx] == this)
                continue;
            else lockOnTarget = playerMechs[idx];
        }
        if (lockOnTarget != null)
            lockedOn = true;
    }

    private void OnEnable()
    {
        controls.HateYourBolts.Enable();
    }

    private void OnDisable()
    {
        controls.HateYourBolts.Disable();
    }
}

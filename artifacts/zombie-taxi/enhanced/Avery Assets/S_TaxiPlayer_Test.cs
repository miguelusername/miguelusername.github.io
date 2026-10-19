using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class S_TaxiPlayer_Test : MonoBehaviour
{
    //bool gameOver = false;

    public bool IsDrifting => _wasDrifting;
    public float Steer01 => _steer;

    [Header("Input")]
    public InputActionReference move;

    [Header("Phyiscs")]
    public Rigidbody rb;
    public WheelCollider FrontLeftWheel, FrontRightWheel, RearLeftWheel, RearRightWheel;
    public Transform frontLeftWheelCast, frontRightWheelCast, rearLeftWheelCast, rearRightWheelCast;
    public bool isGrounded;

    [Header("Models")]
    public Transform FrontLeftWheel_M;
    public Transform FrontRightWheel_M, RearLeftWheel_M, RearRightWheel_M
        , Body_M;

    [Header("Driving")]
    public float motorTorque = 1500f;
    public float reverseTorque = 900f;
    public float brakeTorque = 2500f;
    public float maxSpeedKmh = 70f;
    public float speedKmh;
    public float steerSmoothing = 8f;
    public float throttleSmoothing = 6f;
    public float steerAt0Kmh = 38f;
    public float steerAtMaxKmh = 10f;
    private float currentSteer = 0f;

    [Header("Drifting")]
    public InputActionReference drift;
    public float rearGripNormal = 1.15f;
    public float rearGripDrift = 0.65f;
    public float frontGripNormal = 1.35f;
    public float frontGripDrift = 1.55f; //helps you keep steering control

    public float driftMinSpeedKmh = 15f;
    public float driftSteerBoost = 1.15f; //not really necessary, see if we need or not
    public float driftYawAssist = 1.2f; // try 0.5–2
    private bool _wasDrifting;
    private bool _drifting;

    [Header("Drift Audio")]
    [SerializeField] public AudioSource driftAudio;
    public float driftAudioFadeSpeed = 8f;   //bigger = faster fade
    public float driftVolMax = 0.8f;
    public float driftPitchMin = 0.75f;
    public float driftPitchMax = 0.95f;
    private float _driftAudioVol;

    [Header("Engine Audio")]
    [SerializeField] private AudioSource engineAudio;
    private float enginePitchMin = 0.1f;
    private float enginePitchMax = 1.0f;
    private float engineVolMin = 0.01f;
    private float engineVolMax = 0.06f;
    private float engineResponse = 5f;
    private float engineCycleSpeed = 1.2f;
    private float engineCycleAmount = 0.12f;
    private float engineCycleMinSpeedKmh = 20f;
    private float engineCycleThrottleLimit = 0.35f;

    private float engineCycleTime = 0f;

    [Header("Camera Drift Tilt")]
    Camera cam;
    public float maxDriftTilt = 8f;   //degrees
    public float cameraTiltSpeed = 6f;
    private float _cameraTilt;


    private Vector2 _moveInput;
    private float _steer;
    private float _throttle;

    public float downforce = 70f;
    public float lateralDamping = 2.5f;
    public bool taperTorqueNearMaxSpeed = true;

    [Header("Fuel")]
    public float fuelTankMax = 100f;
    public float fuelTank = 100f;

    public float fuelBurnRate = 1.0f;
    public float burnUpgrade = 1.0f;
    public float minSpeedToBurn = 1.0f;
    public float outOfFuelBrake = 300f;

    public float zombieDamageChance = 0f;

    [Header("Fuel Scaling Over Time")]
    [SerializeField] private float fuelBurnStartMultiplier = 0.35f;
    [SerializeField] private float fuelBurnRampDuration = 180f; //3 minutes to reach full burn
    private float runTime;

    [Header("UI")]
    [SerializeField] private GameObject GameoverUI;
    [SerializeField] private Slider fuelGauge;
    [SerializeField] private Transform speedGaugeNeedle;
    public float needleZAt0 = 90f;
    public float needleZAtMax = -90f;

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
    }

    private void OnEnable()
    {
        if (move != null) move.action.Enable();

        if (drift != null) drift.action.Enable();
    }

    private void OnDisable()
    {
        if (move != null) move.action.Disable();

        if (drift != null) drift.action.Disable();
    }

    private void Start()
    {
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        fuelTank = Mathf.Clamp(fuelTank, 0f, fuelTankMax);

        if (fuelGauge != null)
        {
            fuelGauge.minValue = 0f;
            fuelGauge.maxValue = fuelTankMax;
            fuelGauge.value = fuelTank;
        }

        if (engineAudio != null && !engineAudio.isPlaying)
        {
            engineAudio.loop = true;
            engineAudio.Play();
        }

        SetFrontGrip(frontGripNormal);
        SetRearGrip(rearGripNormal);
    }

    private void Update()
    {
        _moveInput = move.action.ReadValue<Vector2>();

        /*float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        bool driftHeld = drift != null && drift.action.IsPressed();

        _drifting = driftHeld
            && speedKmh >= driftMinSpeedKmh
            && Mathf.Abs(_steer) > 0.15f
            && Mathf.Abs(_throttle) > 0.05f;*/

        //_moveDirection = move.action.ReadValue<Vector2>();
        //steerAngle = maxSteerAngle * _moveDirection.x;
        //calculatedSpeed = carSpeed * _moveDirection.y;
        //Debug.Log(_moveDirection.y);

        RaycastHit hitLF, hitRF, hitLR, hitRR;
        bool lfGrounded = false, rfGrounded = false, lrGrounded = false, rrGrounded = false;
        if (Physics.Raycast(frontLeftWheelCast.position, Vector3.down, out hitLF, 3, LayerMask.GetMask("Ground")))
            lfGrounded = true;
        else lfGrounded = false;
        if (Physics.Raycast(frontRightWheelCast.position, Vector3.down, out hitRF, 3, LayerMask.GetMask("Ground")))
            rfGrounded = true;
        else rfGrounded = false;
        if (Physics.Raycast(rearLeftWheelCast.position, Vector3.down, out hitLR, 3, LayerMask.GetMask("Ground")))
            lrGrounded = true;
        else lrGrounded = false;
        if (Physics.Raycast(rearRightWheelCast.position, Vector3.down, out hitRR, 3, LayerMask.GetMask("Ground")))
            rrGrounded = true;
        else rrGrounded = false;

        isGrounded = lfGrounded && rfGrounded && lrGrounded && rrGrounded;

        UpdateUI();
        UpdateWheels();
    }
    private void FixedUpdate()
    {
        /*
        RearLeftWheel.motorTorque = calculatedSpeed;
        RearRightWheel.motorTorque = calculatedSpeed;
        // FrontLeftWheel.motorTorque = calculatedSpeed;
        // FrontRightWheel.motorTorque = calculatedSpeed;

        Debug.Log(calculatedSpeed);

        //steer
        FrontLeftWheel.steerAngle = steerAngle;
        FrontRightWheel.steerAngle = steerAngle;
         */

        //Smooth input for gamepad + keyboard.
        _steer = Mathf.Lerp(_steer, _moveInput.x, steerSmoothing * Time.fixedDeltaTime);
        _throttle = Mathf.Lerp(_throttle, _moveInput.y, throttleSmoothing * Time.fixedDeltaTime);

        //Brake
        bool brakeHeld = drift.action.IsPressed() && _moveInput.y <= 0;
        if (brakeHeld)
        {
            brakeTorque = 5000;
        }
        else
        {
            brakeTorque = 2500;
        }

        speedKmh = rb.linearVelocity.magnitude * 3.6f;
        UpdateEngineAudio(speedKmh);
        float t = Mathf.InverseLerp(0f, maxSpeedKmh, speedKmh);

        //Drift
        bool driftHeld = drift != null && drift.action.IsPressed();

        bool drifting = driftHeld
            && speedKmh >= driftMinSpeedKmh
            && Mathf.Abs(_steer) > 0.15f
            && Mathf.Abs(_throttle) > 0.05f;
        _drifting = drifting;

        //--- Arcade stability assists ---
        // Downforce increases with speed (using m/s magnitude).
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude, ForceMode.Force);

        //Dampen sideways slip (local X) to reduce fishtailing/spin-outs.
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= (1f - Mathf.Clamp01(lateralDamping * Time.fixedDeltaTime));
        rb.linearVelocity = transform.TransformDirection(localVel);

        //--- Steering (more aggressive reduction at high speed) ---
        float maxSteer = Mathf.Lerp(steerAt0Kmh, steerAtMaxKmh, t);
        float steerScale = Mathf.Lerp(1f, 0.5f, t); // additional stability scaling
        float driftBoost = drifting ? driftSteerBoost : 1f;
        float steerAngle = maxSteer * _steer * steerScale * driftBoost;

        //Drifting
        if (drifting != _wasDrifting)
        {
            if (drifting)
            {
                SetRearGrip(rearGripDrift);
                SetFrontGrip(frontGripDrift);
            }
            else
            {
                SetRearGrip(rearGripNormal);
                SetFrontGrip(frontGripNormal);
            }
            _wasDrifting = drifting;
        }

        if (drifting)
        {
            rb.AddTorque(transform.up * _steer * driftYawAssist, ForceMode.Acceleration);
        }

        // Drift sound
        UpdateDriftAudio(drifting, speedKmh);

        FrontLeftWheel.steerAngle = steerAngle;
        FrontRightWheel.steerAngle = steerAngle;


        //--- Motor/Brake logic ---
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        float appliedMotor = 0f;
        float appliedBrake = 0f;

        if (Mathf.Abs(_throttle) < 0.05f && !brakeHeld)
        {
            //Coasting
            appliedMotor = 0f;
            appliedBrake = 0f;
        }
        else if (_throttle > 0f && !brakeHeld)
        {
            //Forward throttle
            float torqueLimiter = 1f;
            if (taperTorqueNearMaxSpeed)
            {
                //Taper torque smoothly near top speed instead of a hard cutoff.
                //Starts tapering at maxSpeed, reaches 0 at 1.2x maxSpeed.
                float s = Mathf.InverseLerp(maxSpeedKmh, maxSpeedKmh * 1.2f, speedKmh);
                torqueLimiter = 1f - Mathf.Clamp01(s);
            }

            appliedMotor = motorTorque * _throttle * torqueLimiter;

            //If we're moving backwards but pressing forward -> brake first
            if (forwardSpeed < -1f) appliedBrake = brakeTorque;
        }
        else //_throttle < 0
        {
            //Reverse / brake
            //If moving forward and pressing back -> brake
            if (forwardSpeed > 1f)
            {
                appliedMotor = 0f;
                appliedBrake = brakeTorque;
            }
            else
            {
                //Actually reverse
                appliedBrake = 0f;
                appliedMotor = reverseTorque * _throttle; // negative torque
            }
        }

        bool outOfFuel = fuelTank <= 0f;
        if (outOfFuel || (transform.position.y <= -10f))
        {
            PlayerGameOver(steerAngle);
            return;
        }

        //Apply motor to rear wheels
        RearLeftWheel.motorTorque = appliedMotor;
        RearRightWheel.motorTorque = appliedMotor;

        //Apply brakes to all wheels
        FrontLeftWheel.brakeTorque = appliedBrake;
        FrontRightWheel.brakeTorque = appliedBrake;
        RearLeftWheel.brakeTorque = appliedBrake;
        RearRightWheel.brakeTorque = appliedBrake;

        //Fuel burn
        runTime += Time.fixedDeltaTime;
        BurnFuel(speedKmh);
    }

    private void UpdateEngineAudio(float speedKmh)
    {
        if (engineAudio == null) return;

        float speed01 = Mathf.InverseLerp(0f, maxSpeedKmh, speedKmh);
        float throttle01 = Mathf.Clamp01(Mathf.Abs(_throttle));

        //Normal base engine sound
        float rev01 = Mathf.Clamp01(speed01 * 0.75f + throttle01 * 0.45f);

        //Add a looping rev cycle when cruising at a fairly steady speed
        float cycleOffset = 0f;

        bool cruising =
            speedKmh >= engineCycleMinSpeedKmh &&
            throttle01 > 0.05f &&
            throttle01 <= engineCycleThrottleLimit;

        if (cruising)
        {
            engineCycleTime += Time.fixedDeltaTime * engineCycleSpeed;
            float cycle01 = Mathf.PingPong(engineCycleTime, 1f);
            cycleOffset = cycle01 * engineCycleAmount;
        }
        else
        {
            engineCycleTime = 0f;
        }

        float targetPitch = Mathf.Lerp(enginePitchMin, enginePitchMax, rev01) + cycleOffset;
        float targetVolume = Mathf.Lerp(
            engineVolMin,
            engineVolMax,
            Mathf.Max(speed01, throttle01 * 0.6f)
        );

        engineAudio.pitch = Mathf.Lerp(
            engineAudio.pitch,
            targetPitch,
            engineResponse * Time.fixedDeltaTime
        );

        engineAudio.volume = Mathf.Lerp(
            engineAudio.volume,
            targetVolume,
            engineResponse * Time.fixedDeltaTime
        );
    }

    private void BurnFuel(float speedKmh)
    {
        if (fuelTank <= 0f) return;

        bool throttlePressed = Mathf.Abs(_throttle) > 0.05f;

        //Ramp from start multiplier up to 1.0 over time
        float t = Mathf.Clamp01(runTime / fuelBurnRampDuration);
        t = t * t; // ease-in
        float burnScale = Mathf.Lerp(fuelBurnStartMultiplier, 1f, t);

        float baseBurn = fuelBurnRate * burnUpgrade * burnScale;

        //Lose standard amount of fuel when moving. Lose half as much when idle
        if (throttlePressed)
            fuelTank -= baseBurn * Time.fixedDeltaTime;
        else
            fuelTank -= (baseBurn * 0.5f) * Time.fixedDeltaTime;

        if (fuelTank < 0f)
            fuelTank = 0f;
    }

    public void LoseFuel(float fuelLost)
    {
        if (fuelTank <= 0f) return;

        if (UnityEngine.Random.Range(0, zombieDamageChance) == zombieDamageChance)
            fuelTank -= fuelLost;

        if (fuelTank < 0f) fuelTank = 0f;
    }

    private void UpdateUI()
    {
        if (fuelGauge != null)
        {
            fuelGauge.value = fuelTank;
        }

        if (speedGaugeNeedle != null)
        {
            float speedKmh = rb.linearVelocity.magnitude * 3.6f;
            float t = Mathf.InverseLerp(0f, maxSpeedKmh, speedKmh);
            float z = Mathf.Lerp(needleZAt0, needleZAtMax, t);
            speedGaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, z);
        }
    }
    void SetRearGrip(float stiffness)
    {
        var s = RearLeftWheel.sidewaysFriction; s.stiffness = stiffness; RearLeftWheel.sidewaysFriction = s;
        s = RearRightWheel.sidewaysFriction; s.stiffness = stiffness; RearRightWheel.sidewaysFriction = s;
    }

    void SetFrontGrip(float stiffness)
    {
        var s = FrontLeftWheel.sidewaysFriction; s.stiffness = stiffness; FrontLeftWheel.sidewaysFriction = s;
        s = FrontRightWheel.sidewaysFriction; s.stiffness = stiffness; FrontRightWheel.sidewaysFriction = s;
    }


    void UpdateDriftAudio(bool drifting, float speedKmh)
    {
        if (driftAudio == null) return;

        //Ensure it's playing so we can fade volume smoothly
        if (!driftAudio.isPlaying)
            driftAudio.Play();

        //Optional: make volume respond to "how drifty" we are.
        //We can use sideways local velocity as a proxy for slip.
        float lateralSlip = Mathf.Abs(transform.InverseTransformDirection(rb.linearVelocity).x);
        float slip01 = Mathf.InverseLerp(0.5f, 6f, lateralSlip); // tune these numbers

        float speed01 = Mathf.InverseLerp(driftMinSpeedKmh, maxSpeedKmh, speedKmh);

        float targetVol = drifting ? driftVolMax * Mathf.Max(slip01, speed01 * 0.5f) : 0f;

        _driftAudioVol = Mathf.MoveTowards(
            _driftAudioVol,
            targetVol,
            driftAudioFadeSpeed * Time.fixedDeltaTime
        );

        driftAudio.volume = _driftAudioVol;

        //Pitch rises a bit with speed/slip
        float pitchDrive = Mathf.Clamp01(slip01 * 0.7f + speed01 * 0.3f); // less aggressive
        float targetPitch = Mathf.Lerp(driftPitchMin, driftPitchMax, pitchDrive);
        driftAudio.pitch = Mathf.Clamp(targetPitch, 0.6f, 1.1f);

        //If fully silent, optionally stop to save CPU (no abrupt cut because vol is 0)
        if (_driftAudioVol <= 0.001f && !drifting)
            driftAudio.Stop();
    }

    void PlayerGameOver(float steerAngle)
    {
        //Debug.Log("Entered if outOfFuel true");

        RearLeftWheel.motorTorque = 0f;
        RearRightWheel.motorTorque = 0f;

        FrontLeftWheel.brakeTorque = outOfFuelBrake;
        FrontRightWheel.brakeTorque = outOfFuelBrake;
        RearLeftWheel.brakeTorque = outOfFuelBrake;
        RearRightWheel.brakeTorque = outOfFuelBrake;

        //Let the player steer still even though they're out of gas
        FrontLeftWheel.steerAngle = steerAngle;
        FrontRightWheel.steerAngle = steerAngle;

        GameoverUI.SetActive(true);
    }

    void UpdateWheels()
    {
        bool driftHeld = drift != null && drift.action.IsPressed();

        UpdateWheelPose(FrontLeftWheel, FrontLeftWheel_M, true, driftHeld);
        UpdateWheelPose(FrontRightWheel, FrontRightWheel_M, true, driftHeld);

        //Let them back wheels move too when drifting
        if (driftHeld)
        {
            UpdateWheelPose(RearLeftWheel, RearLeftWheel_M, false, driftHeld);
            UpdateWheelPose(RearRightWheel, RearRightWheel_M, false, driftHeld);
        }
    }

    void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelMesh, bool isFrontWheel, bool isDrifting)
    {
        if (!wheelCollider || !wheelMesh) return;

        //Snap mesh to collider (suspension + rotation)
        wheelCollider.GetWorldPose(out Vector3 worldPos, out Quaternion worldRot);

        wheelMesh.position = worldPos;
        wheelMesh.rotation = worldRot;
    }

}
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CannonBehavior : MonoBehaviour
{
    public PlayerControls controls;
    public MechMovement mech;

    public GameObject bulletPrefab;
    public Transform spawnPoint;
    float bulletSpeed = 1000;

    private void Awake()
    {
        controls = new PlayerControls();
        mech = FindFirstObjectByType<MechMovement>();
        bulletPrefab = (GameObject)Resources.Load("BulletPrefab");
        spawnPoint = transform.GetChild(0);
    }
    private void Update()
    {
        if (mech.leftArmActive)
        {
            if (GetComponent<LeftArm>() && controls.HateYourBolts.FireLeftArm.triggered)
            {
                ShootProjectile();
            }

        }
        if (mech.leftShoulderActive)
        {
            if (GetComponent<LeftShoulder>() && controls.HateYourBolts.FireLeftShoulder.triggered)
            {
                ShootProjectile();
            }
        }

        if (mech.rightArmActive)
        {
            if (GetComponent<RightArm>() && controls.HateYourBolts.FireRightArm.triggered)
            {
                ShootProjectile();
            }
        }
        if (mech.rightShoulderActive)
        {
            if (GetComponent<RightShoulder>() && controls.HateYourBolts.FireRightShoulder.triggered)
            {
                ShootProjectile();
            }
        }
    }

    void ShootProjectile()
    {
        var projectile = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);

        if (mech.lockedOn)
        {
            var direction = mech.lockOnTarget.transform.position - transform.position;
            projectile.GetComponent<Rigidbody>().AddRelativeForce(direction.normalized * bulletSpeed, ForceMode.Force);
        }
        else projectile.GetComponent<Rigidbody>().AddForce(mech.transform.forward * bulletSpeed, ForceMode.Force);

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

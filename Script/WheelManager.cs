using UnityEngine;

[System.Serializable]
public class WheelSettings
{
    public float mass = 20f;
    public float radius = 0.37f;
    public float wheelDampingRate = 0.25f;
    public float suspensionDistance = 0.3f;
    public float suspensionSpringStrength = 35000f;
    public float suspensionSpringDamper = 4500f;
    public float targetPosition = 0.5f;

    public float forwardFrictionExtremumSlip = 0.4f;
    public float forwardFrictionExtremumValue = 1f;
    public float forwardFrictionAsymptoteSlip = 0.8f;
    public float forwardFrictionAsymptoteValue = 0.5f;
    public float forwardFrictionStiffness = 1f;

    public float sidewaysFrictionExtremumSlip = 0.2f;
    public float sidewaysFrictionExtremumValue = 1f;
    public float sidewaysFrictionAsymptoteSlip = 0.5f;
    public float sidewaysFrictionAsymptoteValue = 0.75f;
    public float sidewaysFrictionStiffness = 1f;
}

public class WheelManager : MonoBehaviour
{
    public WheelCollider[] wheelColliders;
    public WheelSettings settings;

    void Start()
    {
        ApplySettingsToAllWheels();
    }

    public void ApplySettingsToAllWheels()
    {
        foreach (var wheel in wheelColliders)
        {
            wheel.mass = settings.mass;
            wheel.radius = settings.radius;
            wheel.wheelDampingRate = settings.wheelDampingRate;
            wheel.suspensionDistance = settings.suspensionDistance;

            JointSpring suspensionSpring = wheel.suspensionSpring;
            suspensionSpring.spring = settings.suspensionSpringStrength;
            suspensionSpring.damper = settings.suspensionSpringDamper;
            suspensionSpring.targetPosition = settings.targetPosition;
            wheel.suspensionSpring = suspensionSpring;

            WheelFrictionCurve forwardFriction = wheel.forwardFriction;
            forwardFriction.extremumSlip = settings.forwardFrictionExtremumSlip;
            forwardFriction.extremumValue = settings.forwardFrictionExtremumValue;
            forwardFriction.asymptoteSlip = settings.forwardFrictionAsymptoteSlip;
            forwardFriction.asymptoteValue = settings.forwardFrictionAsymptoteValue;
            forwardFriction.stiffness = settings.forwardFrictionStiffness;
            wheel.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.extremumSlip = settings.sidewaysFrictionExtremumSlip;
            sidewaysFriction.extremumValue = settings.sidewaysFrictionExtremumValue;
            sidewaysFriction.asymptoteSlip = settings.sidewaysFrictionAsymptoteSlip;
            sidewaysFriction.asymptoteValue = settings.sidewaysFrictionAsymptoteValue;
            sidewaysFriction.stiffness = settings.sidewaysFrictionStiffness;
            wheel.sidewaysFriction = sidewaysFriction;
        }
    }
}

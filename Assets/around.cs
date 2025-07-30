using UnityEngine;

public class PhysicsHammer : MonoBehaviour
{
    [SerializeField] private float motorSpeed = 360f;
    [SerializeField] private float maxMotorForce = 1000f;

    private HingeJoint hinge;
    private JointMotor motor;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        motor = hinge.motor;
        motor.targetVelocity = motorSpeed;
        motor.force = maxMotorForce;
        hinge.motor = motor;
        hinge.useMotor = true;
    }

    // Дополнительно: метод для включения/выключения мотора
    public void SetMotorState(bool state)
    {
        hinge.useMotor = state;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

// [System.Serializable]
// public class AxleInfo {
//     public WheelCollider leftWheel;
//     public WheelCollider rightWheel;
//     public bool motor;
//     public bool steering;
//     public bool brake;
// }

public class SimpleCarController : MonoBehaviour {
    public List<AxleInfo> axleInfos; 
    public float maxMotorTorque = 400f;
    public float maxSteeringAngle = 30f;
    public float maxBrakeTorque = 3000f;

    private float motorInput;
    private float steeringInput;
    private bool isBraking;

    // AI 제어 모드를 위한 플래그
    public bool useAIControl = true;

    // AI 또는 외부에서 입력 값을 설정하는 메서드
    public void SetInputs(float motorInput, float steeringInput, bool isBraking)
    {
        if (useAIControl)
        {
            this.motorInput = Mathf.Clamp(motorInput, -1f, 1f); // AI 입력값 제한
            this.steeringInput = Mathf.Clamp(steeringInput, -1f, 1f); // AI 입력값 제한
            this.isBraking = isBraking; // AI가 브레이크를 제어
        }
    }

    public void Update()
    {
        if (!useAIControl)
        {
            // 수동 조작을 위해 키보드 입력을 받아 설정
            motorInput = Input.GetAxis("Vertical"); // W, S 키로 가속/감속
            steeringInput = Input.GetAxis("Horizontal"); // A, D 키로 좌/우 회전

            // 스페이스바로 브레이크 설정
            isBraking = Input.GetKey(KeyCode.Space);
        }
    }

    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0) {
            return;
        }

        Transform visualWheel = collider.transform.GetChild(0);

        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;

        // 바퀴의 회전 각도 적용
        visualWheel.transform.Rotate(Vector3.right, collider.rpm * 6 * Time.deltaTime, Space.Self);
    }

    public void FixedUpdate()
    {
        //Debug.Log($"Motor Input: {motorInput}, Steering Input: {steeringInput}, Is Braking: {isBraking}");

        // 기본 모터 토크 및 조향각 계산
        float motor = maxMotorTorque * motorInput; // 기본 모터 토크
        float steering = maxSteeringAngle * steeringInput; // 조향 각도
        float brake = isBraking ? maxBrakeTorque : 0f; // 브레이크 토크 적용 여부 결정

        if (motorInput < 0) // 후진
        {
            if (IsMovingForward()) // 차량이 전진 중이면 브레이크
            {
                brake = maxBrakeTorque * -motorInput; // 브레이크 토크 적용
                motor = 0f; // 모터 토크 없음
            }
            else // 차량이 후진 중이면 후진 토크 적용
            {
                motor = maxMotorTorque * motorInput; // 후진 토크 적용 (음수)
            }
        }

        // 각 축에 대해 조향, 모터, 브레이크 설정 적용
        foreach (AxleInfo axleInfo in axleInfos) {
            if (axleInfo.steering) {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
            if (axleInfo.motor) {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }
            if (axleInfo.brake) {
                axleInfo.leftWheel.brakeTorque = brake;
                axleInfo.rightWheel.brakeTorque = brake;
            }
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }
    }

    private bool IsMovingForward()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        return Vector3.Dot(transform.forward, rb.velocity) > 0;
    }
}

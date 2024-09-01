using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[System.Serializable]
public class AxleInfo {
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor;
    public bool steering;
    public bool brake;
}

public class LaneKeepingAgent : Agent {
    
    public List<AxleInfo> axleInfos;
    public float maxMotorTorque = 400f;
    public float maxSteeringAngle = 30f;
    public float maxBrakeTorque = 3000f;
    public float speedPenaltyThreshold = 10f; // 시속 10 km/h 이하일 때 감점할 속도 임계값
    public float distanceRewardFactor = 0.01f; // 이동 거리에 따른 보상 비율

    private float motorInput;
    private float steeringInput;
    private bool isBraking;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 lastPosition;
    private float totalDistanceTraveled;

    // 카메라를 위한 필드 추가
    public Camera frontCamera;
    private RenderTexture renderTexture;
    private Texture2D capturedImage;

    private CarSpeedometer speedometer;

    public void Start() {
        Time.timeScale  = 4.0f; // 시뮬레이션 속도를 2배로 설정
    }

    public override void Initialize() {
        // 초기 위치와 회전값을 저장
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        lastPosition = initialPosition;
        totalDistanceTraveled = 0f;

        // 카메라 센서 추가
        CameraSensorComponent cameraSensor = gameObject.AddComponent<CameraSensorComponent>();
        cameraSensor.Camera = frontCamera;
        cameraSensor.SensorName = "FrontCameraSensor_01";
        cameraSensor.Width = 84;
        cameraSensor.Height = 84;
        cameraSensor.Grayscale = true;

        // RenderTexture와 Texture2D 초기화
        renderTexture = new RenderTexture(84, 84, 24);
        frontCamera.targetTexture = renderTexture;
        capturedImage = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // CarSpeedometer 컴포넌트 초기화
        speedometer = GetComponent<CarSpeedometer>();
    }

    public override void CollectObservations(VectorSensor sensor) {
        // 차량의 속도 및 방향을 관찰값으로 추가
        Rigidbody rb = GetComponent<Rigidbody>();
        sensor.AddObservation(rb.velocity.magnitude); // 차량의 속도
        sensor.AddObservation(transform.forward); // 차량의 방향

        // 바퀴의 회전 속도, 각도, 토크를 관찰값으로 추가
        foreach (AxleInfo axleInfo in axleInfos) {
            sensor.AddObservation(axleInfo.leftWheel.rpm / 1000.0f); // 회전 속도
            sensor.AddObservation(axleInfo.leftWheel.steerAngle / maxSteeringAngle); // 각도
            sensor.AddObservation(axleInfo.leftWheel.motorTorque / maxMotorTorque); // 토크
        }
    }

private int lastLoggedDistance = 0;

public override void OnActionReceived(ActionBuffers actions) {
    // 행동을 받아와서 차량 컨트롤러에 전달
    float steering = actions.ContinuousActions[0];
    float acceleration = actions.ContinuousActions[1];
    isBraking = actions.DiscreteActions[0] > 0; // 예: 0은 브레이크 없음, 1은 브레이크 적용

    motorInput = Mathf.Clamp(acceleration, 0f, 1f); // AI 입력값 제한
    steeringInput = Mathf.Clamp(steering, -1f, 1f); // AI 입력값 제한

    ApplyControls();
    
    // 차량 속도가 시속 10km 이하일 때 감점
    int currentSpeed = speedometer.GetCurrentSpeed(); // CarSpeedometer에서 현재 속도를 가져옴
    AddReward(0.1f * currentSpeed); // 속도에 따른 보상

    // 이동 거리에 따라 보상 추가 및 디버그 로그 출력
    float distanceTraveled = Vector3.Distance(transform.localPosition, lastPosition);
    totalDistanceTraveled += distanceTraveled;
    AddReward(distanceTraveled * distanceRewardFactor);

    // totalDistanceTraveled가 정수값에 도달했을 때 디버그 로그 출력
    int currentDistanceInt = Mathf.FloorToInt(totalDistanceTraveled);
    if (currentDistanceInt > lastLoggedDistance) 
    {
        Debug.Log($"Total Distance Traveled reached {currentDistanceInt} meters.");
        lastLoggedDistance = currentDistanceInt;
    }

    lastPosition = transform.localPosition;
}


    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Wall")) {
            AddReward(-100f); // 벽과 충돌했을 때 페널티
            Debug.Log("Collision with wall - ending episode");
            EndEpisode(); // 에피소드 종료
        }

        if (collision.gameObject.CompareTag("Goal")) {
            AddReward(1000.0f); // 목표 지점에 도달했을 때 보상
            Debug.Log("Reached the finish line - ending episode");
            EndEpisode(); // 에피소드 종료
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        actionsOut.ContinuousActions.Array[0] = Input.GetAxis("Horizontal");
        actionsOut.ContinuousActions.Array[1] = Input.GetAxis("Vertical");
        actionsOut.DiscreteActions.Array[0] = Input.GetKey(KeyCode.Space) ? 1 : 0; // 스페이스바로 브레이크 제어
    }

    public override void OnEpisodeBegin() {
        // 초기 위치와 회전으로 리셋
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        lastPosition = initialPosition;
        totalDistanceTraveled = 0f;

        // 초기 속도 및 회전 초기화
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("Episode Started - Position and Rotation Reset");
        Debug.Log($"Initial Position: {transform.localPosition}, Initial Rotation: {transform.localRotation}");
    }

    public void FixedUpdate() {
        ApplyControls();
        RequestDecision();
    }

    private void ApplyControls() {
        // 기본 모터 토크 및 조향각 계산
        float motor = maxMotorTorque * motorInput; // 기본 모터 토크
        float steering = maxSteeringAngle * steeringInput; // 조향 각도
        float brake = isBraking ? maxBrakeTorque : 0f; // 브레이크 토크 적용 여부 결정
        
        if (motorInput < 0) { // 후진
            if (IsMovingForward()) { // 차량이 전진 중이면 브레이크
                brake = maxBrakeTorque * -motorInput; // 브레이크 토크 적용
                motor = 0f; // 모터 토크 없음
            } else { // 차량이 후진 중이면 후진 토크 적용
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

    private bool IsMovingForward() {
        Rigidbody rb = GetComponent<Rigidbody>();
        return Vector3.Dot(transform.forward, rb.velocity) > 0;
    }

    public void ApplyLocalPositionToVisuals(WheelCollider collider) {
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
}

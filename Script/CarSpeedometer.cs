using UnityEngine;

public class CarSpeedometer : MonoBehaviour
{
    private Rigidbody rb;
    private float logInterval = 1.0f;
    private float timeSinceLastLog = 0.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Rigidbody 초기화
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing from the GameObject.");
        }
        timeSinceLastLog = 0;
    }

    void Update()
    {
        if (rb != null)
        {
            // 현재 속도를 m/s 단위로 계산
            float speedInMetersPerSecond = rb.velocity.magnitude;

            // m/s 단위를 km/h로 변환
            int speedInKilometersPerHour = (int)(speedInMetersPerSecond * 3.6f);

            timeSinceLastLog += Time.fixedDeltaTime;
            if (timeSinceLastLog >= logInterval && speedInKilometersPerHour > 0)
            {
                Debug.Log("Current Speed: " + speedInKilometersPerHour + " km/h");
                timeSinceLastLog = 0f;  // 타이머 초기화
            }
        }
    }

    public int GetCurrentSpeed()
    {
        if (rb != null)
        {
            // 현재 속도를 계산하여 km/h 단위로 반환
            float speed = rb.velocity.magnitude * 3.6f; // m/s를 km/h로 변환
            return Mathf.RoundToInt(speed);
        }
        else
        {
            Debug.LogError("Rigidbody is not assigned.");
            return 0; // 혹은 예외를 던지거나 기본값을 반환하도록 설정할 수 있습니다.
        }
    }
}

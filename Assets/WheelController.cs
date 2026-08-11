using UnityEngine;

/// <summary>
/// Визуальное управление колёсами для трафиковых машин (без WheelCollider).
/// Колёса получают:
///   - вращение (прокрутка по локальной оси X) пропорционально линейной скорости и радиусу;
///   - руление (поворот по локальной оси Y) только для передних колёс.
///
/// Применяются через кватернионы раздельно, чтобы исключить gimbal lock при совмещении
/// поворота и прокрутки в localEulerAngles.
/// </summary>
public class WheelController : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public Transform visualModel;
        public bool isSteering;

        // Накопленный угол прокрутки колеса в градусах (не сбрасывается, только растёт)
        [HideInInspector] public float rollAngle;
        // Текущий угол руления (плавно интерполируется к целевому)
        [HideInInspector] public float steerAngle;
    }

    [Header("Колёса")]
    public Wheel[] wheels;

    [Header("Руление")]
    [Tooltip("Максимальный угол поворота передних колёс в градусах.")]
    public float maxSteeringAngle = 35f;
    [Range(1f, 20f)]
    [Tooltip("Скорость сглаживания угла руления. Больше = резче реагирует.")]
    public float steeringSmoothing = 10f;

    [Header("Вращение")]
    [Tooltip("Радиус колеса в метрах. Используется для перевода линейной скорости в угловую (°/с).")]
    public float wheelRadius = 0.33f;

    // -----------------------------------------------------------------------
    // Публичный API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Вызывается каждый FixedUpdate из TrafficCarWaypoints.
    /// </summary>
    /// <param name="linearSpeed">Линейная скорость машины в м/с (rb.velocity.magnitude или currentSpeed).</param>
    /// <param name="rawSteeringAngle">
    ///   Целевой угол руления в градусах, уже в диапазоне [-maxSteeringAngle, +maxSteeringAngle].
    ///   Положительный = поворот вправо.
    /// </param>
    public void UpdateWheels(float linearSpeed, float rawSteeringAngle)
    {
        float dt = Time.fixedDeltaTime;

        // Угловая скорость колеса: v = ω·r  =>  ω(°/s) = v / r * (180/π)
        float degreesPerSecond = (wheelRadius > 0f)
            ? (linearSpeed / wheelRadius) * Mathf.Rad2Deg
            : 0f;

        float targetSteer = Mathf.Clamp(rawSteeringAngle, -maxSteeringAngle, maxSteeringAngle);

        foreach (var wheel in wheels)
        {
            if (wheel.visualModel == null) continue;

            // --- Прокрутка (ось X в локальном пространстве колеса) ---
            wheel.rollAngle += degreesPerSecond * dt;

            // --- Руление (ось Y, только передние колёса) ---
            if (wheel.isSteering)
            {
                wheel.steerAngle = Mathf.LerpAngle(
                    wheel.steerAngle,
                    targetSteer,
                    steeringSmoothing * dt
                );
            }
            else
            {
                // Задние колёса плавно возвращаются к нулю (на случай сброса isSteering в рантайме)
                wheel.steerAngle = Mathf.LerpAngle(wheel.steerAngle, 0f, steeringSmoothing * dt);
            }

            // --- Применяем оба вращения раздельно, без gimbal lock ---
            // Сначала поворот вокруг Y (руление), потом прокрутка вокруг X
            Quaternion steerRot = Quaternion.AngleAxis(wheel.steerAngle, Vector3.up);
            Quaternion rollRot  = Quaternion.AngleAxis(wheel.rollAngle,  Vector3.right);
            wheel.visualModel.localRotation = steerRot * rollRot;
        }
    }
}

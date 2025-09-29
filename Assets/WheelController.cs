using UnityEngine;

public class WheelController : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public Transform visualModel;
        public bool isSteering;
        [HideInInspector] public float currentRotation;
    }

    [Header("Settings")]
    public Wheel[] wheels;
    public float maxSteeringAngle = 35f;
    public float rotationMultiplier = 50f;

    public void UpdateWheels(float speed, float rawSteeringAngle)
    {
        float clampedAngle = Mathf.Clamp(rawSteeringAngle, -maxSteeringAngle, maxSteeringAngle);
        
        foreach (var wheel in wheels)
        {
            if (wheel.visualModel == null) continue;

            // Вращение колеса
            wheel.currentRotation += speed * rotationMultiplier * Time.deltaTime;
            
            // Поворот только для рулевых колес
            if (wheel.isSteering)
            {
                wheel.visualModel.localEulerAngles = new Vector3(
                    wheel.currentRotation % 360,
                    clampedAngle,
                    wheel.visualModel.localEulerAngles.z
                );
            }
            else
            {
                wheel.visualModel.localEulerAngles = new Vector3(
                    wheel.currentRotation % 360,
                    wheel.visualModel.localEulerAngles.y,
                    wheel.visualModel.localEulerAngles.z
                );
            }
        }
    }
}
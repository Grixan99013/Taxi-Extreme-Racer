using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Сontroler : MonoBehaviour
{
    public WheelCollider front_drCol, front_passCol;
    public WheelCollider rear_drCol, rear_passCol;

    public Transform frontdr, frontpas;
    public Transform reardr, rearpas;

    public float _steerAngle;
    public float _motorForce; 

    public float steerAngl;
    float h, v;
    float wheelRPM;

    public GameObject centerOfMass;
    public Rigidbody rigBody;
    public TextMeshProUGUI _printGear;
    public TextMeshProUGUI _printSpeed;

    public int numberOfGears;
    public float maxRPM;
    public float minRPM;
    public AnimationCurve torqueCurve;
    public int _currentGear;
    public float _engineRPM;
    public float _finalGear;

    public enum Drivetrain
    {
        FrontWheelDrive,
        RearWheelDrive,
        AllWheelDrive
    }

    public Drivetrain drivetrain;

    void Start()
    {
        rigBody = GetComponent<Rigidbody>();
        rigBody.centerOfMass = centerOfMass.transform.localPosition;

        torqueCurve = new AnimationCurve(new Keyframe(minRPM, _motorForce), 
                                         new Keyframe(maxRPM / 2, _motorForce * 1.5f), 
                                         new Keyframe(maxRPM, _motorForce));
        _currentGear = 1;
    }

    void FixedUpdate()
    {
        Inputs();
        CalculateEngineRPM();
        Drive();
        UpdateWheelPos(front_drCol, frontdr);
        UpdateWheelPos(front_passCol, frontpas);
        UpdateWheelPos(rear_drCol, reardr);
        UpdateWheelPos(rear_passCol, rearpas);
        tires();
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        DisplayGear();
        DisplaySpeed();
        
    }

    void Inputs()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");
    }

    void DisplaySpeed()
    {
        float speed = rigBody.velocity.magnitude * 4.5f;
        _printSpeed.text = speed.ToString("0");
    }

    void DisplayGear()
    {
        _printGear.fontSize = 34f;
        if(v == 1){
            _printGear.text = "R";
        }
        else{
            _printGear.text = _currentGear.ToString();
        }
    }
    void tires()
    {
        front_drCol.steerAngle = _steerAngle * h;
        front_passCol.steerAngle = _steerAngle * h;
    }


    void CalculateEngineRPM()
    {
        switch (drivetrain)
        {
            case Drivetrain.FrontWheelDrive:
                wheelRPM = front_drCol.rpm;
                break;
            case Drivetrain.RearWheelDrive:
                wheelRPM = rear_drCol.rpm;
                break;
            case Drivetrain.AllWheelDrive:
                wheelRPM = front_drCol.rpm + rear_drCol.rpm;
                break;
            default:
                wheelRPM = front_drCol.rpm;
                break;
        }
        
        float gearRatio =  numberOfGears / ((float)_currentGear * _finalGear);;
        _engineRPM = wheelRPM * -gearRatio;
        if (_engineRPM < 0)
        {
            _engineRPM = -_engineRPM;
        }
        float shiftUpRPM = maxRPM / (gearRatio * _finalGear);
        float shiftDownRPM = minRPM / (gearRatio * _finalGear);

        if (_engineRPM > shiftUpRPM && _currentGear < numberOfGears)
        {
            _currentGear++;
        }
        else if (_engineRPM < shiftDownRPM && _currentGear > 1)
        {
            _currentGear--;
        } 
    }

    void Drive()
    {
        float torque = torqueCurve.Evaluate(_engineRPM) * v;
        float torque25 = torque / 4f;
        float torque75 = torque / 1.33f;
        switch (drivetrain)
        {
            case Drivetrain.FrontWheelDrive:
                front_drCol.motorTorque = torque75;
                front_passCol.motorTorque = torque75;
                rear_drCol.motorTorque = torque25;
                rear_passCol.motorTorque = torque25;
                break;
            case Drivetrain.RearWheelDrive:
                front_drCol.motorTorque = torque25;
                front_passCol.motorTorque = torque25;
                rear_drCol.motorTorque = torque75;
                rear_passCol.motorTorque = torque75;
                break;
            case Drivetrain.AllWheelDrive:
                front_drCol.motorTorque = torque;
                front_passCol.motorTorque = torque;
                rear_drCol.motorTorque = torque;
                rear_passCol.motorTorque = torque;
                break;
            default:
                front_drCol.motorTorque = torque;
                front_passCol.motorTorque = torque;
                rear_drCol.motorTorque = 0;
                rear_passCol.motorTorque = 0;
                break;
        }
    }

    void UpdateWheelPos(WheelCollider col, Transform t)
    {
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        t.position = pos;
        t.rotation = rot;
    }
}
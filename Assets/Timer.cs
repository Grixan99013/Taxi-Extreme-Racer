using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float time = 0;
    public float _goalTime;
    public bool start;
    public GameObject triggerPanel;
    public TextMeshProUGUI value;
    public TextMeshProUGUI winvalue;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        start = true;
    }
    void FixedUpdate()
    {
        if (start == true)
        {
            _goalTime -= Time.deltaTime;
            time += Time.deltaTime;

            int minutes = (int)(_goalTime / 60);
            int seconds = (int)(_goalTime % 60);
            value.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            minutes = (int)(time / 60);
            seconds = (int)(time % 60);
            winvalue.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        Finish();
    }
    void Finish(){
        if(_goalTime <= 0){
            triggerPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}

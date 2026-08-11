using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float time = 0;
    public float _goalTime;
    public bool start;

    [Tooltip("Компонент панели окончания смены (ShiftEndPanel)")]
    public ShiftEndPanel shiftEndPanel;

    public TextMeshProUGUI value;
    public TextMeshProUGUI winvalue;

    private bool shiftEndReported = false;
    private bool finished = false;   // ← защита от повторного вызова

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        if (start) return;

        start = true;
        ShiftStartTracker.MarkShiftStart();
        QuestManager.Instance?.ReportShiftStarted();
    }

    void FixedUpdate()
    {
        if (!start || finished) return;   // ← не тикаем и не проверяем финиш, пока смена не началась

        _goalTime -= Time.deltaTime;
        time += Time.deltaTime;

        // Не даём таймеру уходить в минус на дисплее
        float displayTime = Mathf.Max(0f, _goalTime);

        int minutes = (int)(displayTime / 60);
        int seconds = (int)(displayTime % 60);
        if (value != null)
            value.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        minutes = (int)(time / 60);
        seconds = (int)(time % 60);
        if (winvalue != null)
            winvalue.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (_goalTime <= 0)
        {
            Finish();
        }
    }

    void Finish()
    {
        if (finished) return;
        finished = true;

        // Останавливаем время до Show(), чтобы минигра или анимации не мешали
        Time.timeScale = 0f;

        if (!shiftEndReported)
        {
            shiftEndReported = true;
            QuestManager.Instance?.ReportShiftEnded();
        }

        if (shiftEndPanel != null)
        {
            shiftEndPanel.Show();
        }
        else
        {
            Debug.LogError("[Timer] shiftEndPanel не назначен в инспекторе! Панель окончания смены не появится.");
        }
    }
}

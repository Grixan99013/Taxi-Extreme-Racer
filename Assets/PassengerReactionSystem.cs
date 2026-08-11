using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Отслеживает скорость и столкновения и показывает текстовые реплики пассажира.
/// Назначь на любой GameObject в GameScene (например, на Canvas или HUD).
///
/// Подключение:
///   1. Добавь компонент на объект в сцене.
///   2. Назначь phraseData[i] для каждого типа пассажира (индексы совпадают с passengerType в CheckpointCounter).
///   3. Назначь UI-элементы: speechBubble (панель), speechText (TMP), speechAvatar (Image).
///   4. В CheckpointCounter вызываются OnPassengerPickedUp / OnPassengerDelivered / OnPassengerFailed
///      — просто добавь вызовы в нужные места (см. комментарии ниже).
/// </summary>
public class PassengerReactionSystem : MonoBehaviour
{
    // ─── Данные реплик ──────────────────────────────────────────────
    [Header("Данные реплик (индекс = passengerType)")]
    [Tooltip("Ассеты PassengerPhraseData для каждого типа пассажира. Индекс совпадает с passengerType.")]
    public PassengerPhraseData[] phraseData;

    // ─── UI «речевой пузырь» ────────────────────────────────────────
    [Header("UI речевого пузыря")]
    [Tooltip("Корневой объект пузыря (Panel / Image). Скрывается когда нет пассажира.")]
    public GameObject speechBubble;

    [Tooltip("Текст реплики.")]
    public TextMeshProUGUI speechText;

    [Tooltip("Аватар пассажира рядом с пузырём (необязательно).")]
    public Image speechAvatar;

    [Tooltip("Сколько секунд показывать реплику перед скрытием.")]
    public float phraseDuration = 3f;

    // ─── Пороги скорости ────────────────────────────────────────────
    [Header("Пороги скорости (км/ч)")]
    public float highSpeedThreshold    = 100f;
    public float veryHighSpeedThreshold = 140f;

    [Tooltip("Минимальный интервал между репликами на скорость (сек). Чтобы не спамило.")]
    public float speedPhraseInterval = 8f;

    // ─── Внутренние ссылки ──────────────────────────────────────────
    private Сontroler    carController;
    private CarDamageController damageController;

    private int     currentPassengerType = -1;
    private bool    hasPassenger         = false;
    private Sprite  currentPassengerSprite;

    private Coroutine hideCoroutine;
    private float     lastSpeedPhraseTime = -999f;

    // Флаги состояния скорости — чтобы реагировать на переход порога, а не каждый кадр
    private bool wasAboveHigh     = false;
    private bool wasAboveVeryHigh = false;

    // ────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ────────────────────────────────────────────────────────────────

    private void Start()
    {
        carController    = FindObjectOfType<Сontroler>();
        damageController = FindObjectOfType<CarDamageController>();

        if (damageController != null)
            damageController.OnCarCollision += OnCollision;

        if (speechBubble != null)
            speechBubble.SetActive(false);
    }

    private void OnDestroy()
    {
        if (damageController != null)
            damageController.OnCarCollision -= OnCollision;
    }

    private void Update()
    {
        if (!hasPassenger || carController == null) return;
        CheckSpeedReactions();
    }

    // ────────────────────────────────────────────────────────────────
    // Публичное API — вызывается из CheckpointCounter
    // ────────────────────────────────────────────────────────────────

    /// <summary>Вызвать когда пассажир сел в машину.</summary>
    public void OnPassengerPickedUp(int passengerType, Sprite avatar)
    {
        currentPassengerType   = passengerType;
        currentPassengerSprite = avatar;
        hasPassenger           = true;

        wasAboveHigh     = false;
        wasAboveVeryHigh = false;
        lastSpeedPhraseTime = -999f;

        // Приветственная реплика
        string phrase = GetPhraseData(passengerType)?.GetGreetingPhrase();
        if (!string.IsNullOrEmpty(phrase))
            ShowPhrase(phrase, avatar);
    }

    /// <summary>Вызвать когда пассажир успешно доставлен.</summary>
    public void OnPassengerDelivered(int passengerType, Sprite avatar)
    {
        string phrase = GetPhraseData(passengerType)?.GetArrivalPhrase();
        if (!string.IsNullOrEmpty(phrase))
            ShowPhrase(phrase, avatar);

        ClearPassenger();
    }

    /// <summary>Вызвать когда поездка провалилась (meter = 0).</summary>
    public void OnPassengerFailed(int passengerType, Sprite avatar)
    {
        HidePhrase();
        ClearPassenger();
    }

    // ────────────────────────────────────────────────────────────────
    // Реакции
    // ────────────────────────────────────────────────────────────────

    private void OnCollision()
    {
        if (!hasPassenger) return;

        string phrase = GetPhraseData(currentPassengerType)?.GetCollisionPhrase();
        if (!string.IsNullOrEmpty(phrase))
            ShowPhrase(phrase, currentPassengerSprite);
    }

    private void CheckSpeedReactions()
    {
        if (carController == null) return;

        // Скорость уже считается в controler.cs как rigBody.velocity.magnitude * 4.5f
        float speed = carController.rigBody != null
            ? carController.rigBody.velocity.magnitude * 4.5f
            : 0f;

        bool aboveVeryHigh = speed >= veryHighSpeedThreshold;
        bool aboveHigh     = speed >= highSpeedThreshold;

        // Переход через порог 140
        if (aboveVeryHigh && !wasAboveVeryHigh)
        {
            TryShowSpeedPhrase(very: true);
        }
        // Переход через порог 100 (но не 140, чтобы не дублировать)
        else if (aboveHigh && !wasAboveHigh)
        {
            TryShowSpeedPhrase(very: false);
        }

        wasAboveVeryHigh = aboveVeryHigh;
        wasAboveHigh     = aboveHigh;
    }

    private void TryShowSpeedPhrase(bool very)
    {
        // Соблюдаем интервал между спидовыми репликами
        if (Time.time - lastSpeedPhraseTime < speedPhraseInterval) return;

        var data = GetPhraseData(currentPassengerType);
        if (data == null) return;

        string phrase = very ? data.GetVeryHighSpeedPhrase() : data.GetHighSpeedPhrase();
        if (string.IsNullOrEmpty(phrase)) return;

        ShowPhrase(phrase, currentPassengerSprite);
        lastSpeedPhraseTime = Time.time;
    }

    // ────────────────────────────────────────────────────────────────
    // UI
    // ────────────────────────────────────────────────────────────────

    private void ShowPhrase(string phrase, Sprite avatar)
    {
        if (speechBubble == null || speechText == null) return;

        // Останавливаем предыдущий таймер скрытия
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        speechText.text = phrase;

        if (speechAvatar != null)
        {
            speechAvatar.sprite  = avatar;
            speechAvatar.enabled = avatar != null;
        }

        speechBubble.SetActive(true);

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private void HidePhrase()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        if (speechBubble != null)
            speechBubble.SetActive(false);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(phraseDuration);

        CanvasGroup cg = speechBubble.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float elapsed = 0f;
            float fadeTime = 0.4f;
            while (elapsed < fadeTime)
            {
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            cg.alpha = 1f; // Сбрасываем для следующего показа
        }

        if (speechBubble != null)
            speechBubble.SetActive(false);

        hideCoroutine = null;
    }

    // ────────────────────────────────────────────────────────────────
    // Вспомогательные
    // ────────────────────────────────────────────────────────────────

    private void ClearPassenger()
    {
        hasPassenger           = false;
        currentPassengerType   = -1;
        currentPassengerSprite = null;
        wasAboveHigh           = false;
        wasAboveVeryHigh       = false;
    }

    private PassengerPhraseData GetPhraseData(int type)
    {
        if (phraseData == null || type < 0 || type >= phraseData.Length) return null;
        return phraseData[type];
    }
}

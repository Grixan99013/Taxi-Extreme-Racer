using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Мини-игра "чаевые": индикатор бежит слева направо по полоске один проход,
/// игрок должен нажать клавишу (по умолчанию Space), когда индикатор находится
/// в зелёной зоне. Один проход, одна попытка:
///   - нажал в зелёной зоне -> успех
///   - нажал не в зелёной зоне -> мгновенный провал
///   - не нажал и индикатор дошёл до конца полоски -> провал по таймауту
///
/// НАСТРОЙКА В UNITY:
///   panelRoot   — корневой GameObject мини-игры (показывается/скрывается целиком)
///   barRect     — RectTransform самой полоски (серый фон), используется для расчёта
///                 ширины при позиционировании зелёной зоны и индикатора
///   greenZoneRect — RectTransform зелёного сегмента (дочерний объект barRect),
///                   его Anchor/Pivot должны быть настроены так, чтобых менять
///                   anchoredPosition.x и width напрямую (Left/Middle pivot, stretch по Y)
///   indicatorRect — RectTransform движущегося значка (тоже дочерний объект barRect)
///
/// Все позиции считаются в локальных координатах barRect: 0 = левый край, barRect.rect.width = правый край.
/// </summary>
public class PickupMinigame : MonoBehaviour
{
    [Header("UI элементы")]
    public GameObject panelRoot;
    public RectTransform barRect;
    public RectTransform greenZoneRect;
    public RectTransform indicatorRect;

    [Header("Ввод")]
    public KeyCode triggerKey = KeyCode.Space;

    [Header("Визуал результата (необязательно)")]
    [Tooltip("Цвет индикатора в обычном состоянии.")]
    public Color indicatorIdleColor = Color.white;
    [Tooltip("Цвет индикатора при успехе (на краткий момент перед скрытием).")]
    public Color indicatorSuccessColor = Color.green;
    [Tooltip("Цвет индикатора при провале.")]
    public Color indicatorFailColor = Color.red;
    public Image indicatorImage;

    [Tooltip("Image зелёной зоны — цвет меняется в зависимости от оценки (необязательно).")]
    public Image greenZoneImage;

    [Tooltip("TMP текст с текущей оценкой за заказ (необязательно).")]
    public TMPro.TextMeshProUGUI ratingLabel;

    [Tooltip("Цвет зелёной зоны при максимальной оценке (5).")]
    public Color zoneColorMax = new Color(0.2f, 0.85f, 0.3f, 1f);

    [Tooltip("Цвет зелёной зоны при минимальной оценке (1).")]
    public Color zoneColorMin = new Color(0.85f, 0.3f, 0.15f, 1f);

    [Tooltip("Сколько секунд показывать результат (зелёный/красный индикатор) перед скрытием панели.")]
    public float resultDisplayTime = 0.4f;

    public bool IsRunning { get; private set; }

    private Coroutine runningCoroutine;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Запускает мини-игру. travelTime — сколько секунд индикатор идёт от начала до конца полоски,
    /// zoneWidthFraction — ширина зелёной зоны как доля от 0..1 от всей полоски (например 0.18 = 18%).
    /// Зелёная зона всегда центрируется где-то в средней трети полоски (с небольшой случайностью),
    /// чтобы каждый заказ ощущался чуть иначе.
    /// onComplete вызывается ровно один раз с результатом (true = успех, false = провал).
    /// </summary>
    /// <summary>
    /// currentRating (1..5) — текущая оценка за заказ. Используется для
    /// окраски зелёной зоны и отображения текста (зона уже посчитана снаружи).
    /// </summary>
    public void StartMinigame(float travelTime, float zoneWidthFraction, int currentRating, Action<bool> onComplete)
    {
        if (IsRunning)
        {
            // На случай повторного вызова, пока предыдущий ещё не завершился — останавливаем старый
            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        }

        runningCoroutine = StartCoroutine(RunMinigame(travelTime, zoneWidthFraction, currentRating, onComplete));
    }

    private IEnumerator RunMinigame(float travelTime, float zoneWidthFraction, int currentRating, Action<bool> onComplete)
    {
        IsRunning = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (indicatorImage != null) indicatorImage.color = indicatorIdleColor;

        // Ставим игру на паузу на время мини-игры. Сама мини-игра работает в реальном
        // времени (unscaledDeltaTime/WaitForSecondsRealtime), поэтому продолжает идти,
        // несмотря на timeScale = 0.
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Ждём конец кадра, чтобы Unity успел пересчитать RectTransform только что
        // включённой панели — иначе barRect.rect.width на первом кадре может быть 0,
        // и зелёная зона/индикатор схлопнутся в левый край полоски.
        yield return new WaitForEndOfFrame();

        // Окрашиваем зелёную зону в зависимости от оценки: 5 -> зелёный, 1 -> красноватый
        float ratingT = Mathf.Clamp01((currentRating - 1) / 4f);
        Color zoneColor = Color.Lerp(zoneColorMin, zoneColorMax, ratingT);
        if (greenZoneImage != null)
            greenZoneImage.color = zoneColor;

        // Показываем текущую оценку над полоской
        if (ratingLabel != null)
            ratingLabel.text = $"Оценка: {currentRating}/5";

        float barWidth = barRect.rect.width;
        zoneWidthFraction = Mathf.Clamp(zoneWidthFraction, 0.05f, 0.6f);
        float zoneWidth = barWidth * zoneWidthFraction;

        // Зелёная зона где-то в средней трети полоски (40%–70% от ширины), со случайным смещением,
        // чтобы не быть каждый раз ровно по центру.
        float zoneCenterMin = barWidth * 0.40f;
        float zoneCenterMax = barWidth * 0.70f;
        float zoneCenterX = UnityEngine.Random.Range(zoneCenterMin, zoneCenterMax);
        float zoneStartX = zoneCenterX - zoneWidth * 0.5f;

        if (greenZoneRect != null)
        {
            greenZoneRect.anchoredPosition = new Vector2(zoneStartX, greenZoneRect.anchoredPosition.y);
            greenZoneRect.sizeDelta = new Vector2(zoneWidth, greenZoneRect.sizeDelta.y);
        }

        bool? result = null;
        float elapsed = 0f;
        travelTime = Mathf.Max(0.1f, travelTime);

        while (elapsed < travelTime)
        {
            float t = elapsed / travelTime;
            float indicatorX = Mathf.Lerp(0f, barWidth, t);

            if (indicatorRect != null)
                indicatorRect.anchoredPosition = new Vector2(indicatorX, indicatorRect.anchoredPosition.y);

            if (Input.GetKeyDown(triggerKey))
            {
                bool inZone = indicatorX >= zoneStartX && indicatorX <= zoneStartX + zoneWidth;
                result = inZone;
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Если цикл вышел по таймауту (индикатор дошёл до конца, клавиша не нажата) — провал
        if (result == null)
        {
            result = false;
            if (indicatorRect != null)
                indicatorRect.anchoredPosition = new Vector2(barWidth, indicatorRect.anchoredPosition.y);
        }

        bool success = result.Value;

        if (indicatorImage != null)
            indicatorImage.color = success ? indicatorSuccessColor : indicatorFailColor;

        if (resultDisplayTime > 0f)
            yield return new WaitForSecondsRealtime(resultDisplayTime);

        if (panelRoot != null) panelRoot.SetActive(false);

        Time.timeScale = previousTimeScale;

        IsRunning = false;
        runningCoroutine = null;

        onComplete?.Invoke(success);
    }
}

using UnityEngine;
using System;

/// <summary>
/// Статический хранитель Unix-времени начала текущей смены.
/// Позволяет ShiftEndPanel отфильтровать только заказы ТЕКУЩЕЙ смены
/// из общей истории OrderHistoryManager.
///
/// Вызов ShiftStartTracker.MarkShiftStart() делается из Timer.StartTimer().
/// </summary>
public static class ShiftStartTracker
{
    /// <summary>Unix-время (секунды UTC) старта текущей смены. 0 — смена ещё не начата.</summary>
    public static long ShiftStartUnix { get; private set; } = 0;

    /// <summary>
    /// Индекс первого заказа ТЕКУЩЕЙ смены в списке OrderHistoryManager.history.
    /// ShiftEndPanel использует его вместо временно́й метки, чтобы фильтрация не
    /// зависела от часового пояса устройства или быстрых тестов.
    /// -1 означает «смена ещё не начата».
    /// </summary>
    public static int ShiftOrderStartIndex { get; private set; } = -1;

    /// <summary>Записывает текущий момент и текущий размер истории как начало смены.</summary>
    public static void MarkShiftStart()
    {
        ShiftStartUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Запоминаем, сколько записей уже есть в истории — все новые с этого момента
        // принадлежат текущей смене.
        ShiftOrderStartIndex = OrderHistoryManager.Instance != null
            ? OrderHistoryManager.Instance.GetTotalCount()
            : 0;

        Debug.Log($"[ShiftStartTracker] Смена началась. Unix={ShiftStartUnix}, orderStartIndex={ShiftOrderStartIndex}");
    }

    /// <summary>Сбрасывает метку (вызывать при загрузке новой сцены, если нужно).</summary>
    public static void Reset()
    {
        ShiftStartUnix = 0;
        ShiftOrderStartIndex = -1;
    }
}

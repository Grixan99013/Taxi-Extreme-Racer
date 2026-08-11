using UnityEngine;

/// <summary>
/// При загрузке GameScene находит все объекты с тегом barrierTag и для каждого
/// независимо случайно решает — включить или выключить.
/// Вероятность включения настраивается через spawnChance (0..1).
///
/// КАК ИСПОЛЬЗОВАТЬ:
///   1. Выделите все объекты-заграждения на сцене и назначьте им тег (например "Barrier").
///      Если тег ещё не создан — Project Settings -> Tags and Layers -> Tags -> добавить.
///   2. Создайте любой пустой GameObject на сцене GameScene (например "BarrierManager"),
///      повесьте на него этот скрипт.
///   3. В инспекторе укажите тег в поле Barrier Tag (по умолчанию уже стоит "Barrier").
///   4. При каждой загрузке сцены все заграждения будут случайно включаться/выключаться.
///
/// ВАЖНО: объекты-заграждения должны быть изначально ВКЛЮЧЕНЫ в сцене (SetActive(true)),
///   иначе FindGameObjectsWithTag их не найдёт — Unity не ищет неактивные объекты по тегу.
///   Если нужно, чтобы они по умолчанию были скрыты до старта — оставьте их включёнными,
///   скрипт сам решит, какие выключить в Start().
/// </summary>
public class RoadBarrierRandomizer : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Тег, которым помечены все объекты-заграждения на сцене.")]
    public string barrierTag = "Barrier";

    [Range(0f, 1f)]
    [Tooltip("Вероятность того, что каждое конкретное заграждение будет ВКЛЮЧЕНО (0 = никогда, 1 = всегда, 0.5 = 50/50).")]
    public float spawnChance = 0.5f;

    [Tooltip("Если включено — в Console будет выведен список: какие заграждения включились, какие нет.")]
    public bool debugLog = false;

    private void Start()
    {
        GameObject[] barriers = GameObject.FindGameObjectsWithTag(barrierTag);

        if (barriers.Length == 0)
        {
            Debug.LogWarning($"[RoadBarrierRandomizer] Не найдено ни одного объекта с тегом \"{barrierTag}\". " +
                             "Убедитесь, что тег задан и объекты активны на сцене.");
            return;
        }

        int enabledCount = 0;

        foreach (GameObject barrier in barriers)
        {
            bool active = Random.value <= spawnChance;
            barrier.SetActive(active);

            if (active) enabledCount++;
        }

        if (debugLog)
        {
            Debug.Log($"[RoadBarrierRandomizer] Заграждений всего: {barriers.Length}, " +
                      $"включено: {enabledCount}, выключено: {barriers.Length - enabledCount}.");
        }
    }
}

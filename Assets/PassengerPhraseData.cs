using UnityEngine;

/// <summary>
/// ScriptableObject с репликами одного типа пассажира.
/// Создаётся через Assets > Create > Taxi Game > Passenger Phrase Data.
/// Каждому типу пассажира назначается свой ассет в инспекторе PassengerReactionSystem.
/// </summary>
[CreateAssetMenu(fileName = "PassengerPhraseData", menuName = "TaxiGame/Passenger Phrase Data")]
public class PassengerPhraseData : ScriptableObject
{
    [Header("Реакция на столкновение")]
    [Tooltip("Реплики при ударе машины. Выбирается случайная.")]
    public string[] collisionPhrases = {
        "Эй, осторожнее!",
        "Что это было?!",
        "Аккуратнее на дороге!"
    };

    [Header("Реакция на высокую скорость (100+ км/ч)")]
    [Tooltip("Если true — пассажир ЛЮБИТ высокую скорость. Иначе — боится.")]
    public bool enjoysHighSpeed = false;

    [Tooltip("Реплики при разгоне до 100+ км/ч. Выбирается случайная.")]
    public string[] highSpeedPhrases = {
        "Не так быстро, пожалуйста!",
        "Мы что, куда-то торопимся?!",
        "Сбавьте скорость!"
    };

    [Header("Реакция на очень высокую скорость (140+ км/ч)")]
    [Tooltip("Реплики при разгоне до 140+ км/ч. Если пусто — используются фразы из highSpeedPhrases.")]
    public string[] veryHighSpeedPhrases = {
        "Остановите машину!! СЕЙЧАС ЖЕ!",
        "Я выхожу на светофоре, клянусь!!",
        "Это безумие!!!"
    };

    [Header("Реакция при посадке")]
    [Tooltip("Реплика когда пассажир садится в машину. Если пусто — ничего не показывается.")]
    public string[] greetingPhrases = {
        "Доброго дня!",
        "Привет, едем?"
    };

    [Header("Реакция при доставке")]
    [Tooltip("Реплика при успешной доставке. Если пусто — ничего не показывается.")]
    public string[] arrivalPhrases = {
        "Спасибо, удачи!",
        "Отличная поездка!"
    };

    // ─── Хелперы ───────────────────────────────────────────

    public string GetCollisionPhrase()    => PickRandom(collisionPhrases);
    public string GetHighSpeedPhrase()    => PickRandom(highSpeedPhrases);
    public string GetVeryHighSpeedPhrase()
    {
        if (veryHighSpeedPhrases != null && veryHighSpeedPhrases.Length > 0)
            return PickRandom(veryHighSpeedPhrases);
        return GetHighSpeedPhrase();
    }
    public string GetGreetingPhrase()     => PickRandom(greetingPhrases);
    public string GetArrivalPhrase()      => PickRandom(arrivalPhrases);

    private string PickRandom(string[] arr)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Random.Range(0, arr.Length)];
    }
}

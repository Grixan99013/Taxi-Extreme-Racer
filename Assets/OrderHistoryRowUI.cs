using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Необязательный компонент для строки истории заказов — вешается на historyRowPrefab,
/// если нужно показывать рейтинг и деньги раздельно (например, разными цветами),
/// а не одной строкой текста.
///
/// Если на префабе нет этого компонента, OrderHistoryUI просто найдёт любой
/// TextMeshProUGUI на префабе и подставит туда полный текст через ToDisplayString().
/// </summary>
public class OrderHistoryRowUI : MonoBehaviour
{
    [Header("Вариант 1: один общий текст")]
    [Tooltip("Если задано — сюда подставляется готовая строка " +
             "\"Ты выполнил заказ, пассажир оценил на X, Y$\".")]
    public TextMeshProUGUI fullText;

    [Header("Вариант 2: раздельные поля")]
    public TextMeshProUGUI ratingText;
    public TextMeshProUGUI moneyText;
    public Image[] starIcons; // опционально: подсветка звёзд по рейтингу

    public void Setup(OrderHistoryEntry entry)
    {
        if (fullText != null)
        {
            fullText.text = entry.ToDisplayString();
        }

        if (ratingText != null)
        {
            ratingText.text = entry.rating.ToString();
        }

        if (moneyText != null)
        {
            moneyText.text = entry.money > 0 ? $"+{entry.money}$" : "0$";
            moneyText.color = entry.money > 0 ? Color.green : Color.red;
        }

        if (starIcons != null)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].color = i < entry.rating ? Color.yellow : Color.gray;
                }
            }
        }
    }
}

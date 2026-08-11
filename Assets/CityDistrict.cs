using UnityEngine;

/// <summary>
/// Невидимый прямоугольный район (зона) на карте.
/// Не использует коллайдеры — просто хранит центр и размер зоны
/// и умеет проверять, попадает ли точка в этот прямоугольник по осям X/Z.
/// 
/// Как использовать:
/// 1. Создать пустой GameObject (например, "District_Downtown") и повесить на него этот скрипт.
/// 2. Разместить объект в центре нужного района и задать size — ширину по X и глубину по Z.
/// 3. Все такие объекты должны быть собраны в массив на DistrictManager (или найдены автоматически).
/// 
/// Для удобства редактирования зона рисуется в виде проволочного прямоугольника в Scene View (Gizmos),
/// сама зона невидима в игре — рендера у объекта нет.
/// </summary>
public class CityDistrict : MonoBehaviour
{
    [Header("Настройки района")]
    public string districtName = "District";

    [Tooltip("Размер зоны по осям X (ширина) и Z (глубина). Высота (Y) не используется.")]
    public Vector2 size = new Vector2(100f, 100f);

    [Header("Отладка")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.25f);

    /// <summary>
    /// Возвращает true, если мировая точка (учитываются только X и Z) находится внутри района.
    /// Центром района считается позиция этого GameObject.
    /// </summary>
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 center = transform.position;
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;

        bool insideX = worldPosition.x >= center.x - halfX && worldPosition.x <= center.x + halfX;
        bool insideZ = worldPosition.z >= center.z - halfZ && worldPosition.z <= center.z + halfZ;

        return insideX && insideZ;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 center = transform.position;
        Vector3 size3D = new Vector3(size.x, 1f, size.y);
        Gizmos.DrawCube(center, size3D);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, size3D);
    }
}

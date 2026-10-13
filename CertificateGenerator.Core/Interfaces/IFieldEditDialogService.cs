using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Сервис показа диалога создания/редактирования поля.
/// </summary>
public interface IFieldEditDialogService
{
    /// <summary>
    /// Показать диалог создания нового поля.
    /// </summary>
    /// <param name="existingNames">Имена уже существующих полей.</param>
    /// <param name="columns">Колонки таблицы для выбора источника.</param>
    /// <returns>Созданное поле или <c>null</c>, если пользователь отменил.</returns>
    Field? ShowCreate(
        IReadOnlyList<string> existingNames,
        IReadOnlyList<ColumnInfo> columns);

    /// <summary>
    /// Показать диалог редактирования существующего поля.
    /// </summary>
    /// <param name="field">Поле для редактирования.</param>
    /// <param name="existingNames">Имена других полей.</param>
    /// <param name="columns">Колонки таблицы для выбора источника.</param>
    /// <returns>Обновлённое поле или <c>null</c>, если пользователь отменил.</returns>
    Field? ShowEdit(
        Field field,
        IReadOnlyList<string> existingNames,
        IReadOnlyList<ColumnInfo> columns);
}
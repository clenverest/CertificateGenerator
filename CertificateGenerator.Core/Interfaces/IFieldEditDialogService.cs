using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Сервис показа диалога создания/редактирования поля.
/// Живёт в Core, потому что ViewModel (Core не знает о WPF)
/// не должна зависеть от WPF-окна.
/// </summary>
public interface IFieldEditDialogService
{
    /// <summary>
    /// Показать диалог создания нового поля.
    /// </summary>
    /// <param name="existingNames">Имена уже существующих полей
    /// (для проверки дубликата).</param>
    /// <returns>Созданное поле или <c>null</c>, если пользователь отменил.</returns>
    Field? ShowCreate(IReadOnlyList<string> existingNames);

    /// <summary>
    /// Показать диалог редактирования существующего поля.
    /// </summary>
    /// <param name="field">Поле для редактирования.</param>
    /// <param name="existingNames">Имена других полей
    /// (без имени редактируемого поля).</param>
    /// <returns>Обновлённое поле или <c>null</c>, если пользователь отменил.</returns>
    Field? ShowEdit(Field field, IReadOnlyList<string> existingNames);
}
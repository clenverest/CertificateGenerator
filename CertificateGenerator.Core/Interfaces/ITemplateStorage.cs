using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Сохранение PNG-шаблона в папку проекта и чтение информации о нём.
/// </summary>
public interface ITemplateStorage
{
    /// <summary>
    /// Скопировать PNG из <paramref name="sourcePngPath"/> в папку
    /// <c>template/</c> проекта <paramref name="projectPath"/>.
    /// Вернуть информацию о шаблоне (имя файла, ширина, высота).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Бросается, если пути пусты.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Бросается, если исходный PNG не найден.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Бросается, если файл не является корректным PNG.
    /// </exception>
    TemplateInfo SaveTemplate(string projectPath, string sourcePngPath);

    /// <summary>
    /// Прочитать информацию о шаблоне из папки проекта.
    /// Вернуть <c>null</c>, если шаблона нет.
    /// </summary>
    TemplateInfo? GetTemplate(string projectPath);

    /// <summary>
    /// Полный путь к файлу шаблона в папке проекта.
    /// </summary>
    string GetTemplatePath(string projectPath);
}
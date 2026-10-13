using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Переключение между окнами приложения.
/// </summary>
public interface IWindowNavigator
{
    /// <summary>
    /// Открыть главное окно для указанного проекта.
    /// Текущее окно (стартовое) закрывается.
    /// </summary>
    /// <param name="project">Проект, с которым будем работать.</param>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    void NavigateToMain(Project project, string projectFolderPath);

    /// <summary>
    /// Вернуться на стартовый экран.
    /// Главное окно закрывается.
    /// </summary>
    void NavigateToStartup();
}
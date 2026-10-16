namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Показ модальных сообщений пользователю.
/// Абстракция над MessageBox, чтобы ViewModel не зависел от WPF
/// и чтобы сценарии с ошибками можно было покрыть тестами.
/// </summary>
public interface IMessageBoxService
{
    /// <summary>Информационное сообщение.</summary>
    void ShowInfo(string message, string title);

    /// <summary>Предупреждение (жёлтая иконка).</summary>
    void ShowWarning(string message, string title);

    /// <summary>Ошибка (красная иконка).</summary>
    void ShowError(string message, string title);
}
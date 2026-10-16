using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый сервис сообщений: не показывает окна,
/// только запоминает вызовы.
/// </summary>
public sealed class FakeMessageBoxService : IMessageBoxService
{
    /// <summary>Все информационные сообщения: (текст, заголовок).</summary>
    public List<(string Message, string Title)> InfoCalls { get; } = new();

    /// <summary>Все предупреждения: (текст, заголовок).</summary>
    public List<(string Message, string Title)> WarningCalls { get; } = new();

    /// <summary>Все ошибки: (текст, заголовок).</summary>
    public List<(string Message, string Title)> ErrorCalls { get; } = new();

    public void ShowInfo(string message, string title)
    {
        InfoCalls.Add((message, title));
    }

    public void ShowWarning(string message, string title)
    {
        WarningCalls.Add((message, title));
    }

    public void ShowError(string message, string title)
    {
        ErrorCalls.Add((message, title));
    }
}
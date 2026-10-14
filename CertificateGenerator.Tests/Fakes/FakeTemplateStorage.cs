using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковое хранилище шаблонов: позволяет задать,
/// что вернуть и что бросить.
/// </summary>
public sealed class FakeTemplateStorage : ITemplateStorage
{
    public TemplateInfo? TemplateToReturnOnSave { get; set; }
    public TemplateInfo? TemplateToReturnOnGet { get; set; }

    public Exception? ExceptionToThrowOnSave { get; set; }
    public Exception? ExceptionToThrowOnGet { get; set; }

    public string TemplatePathToReturn { get; set; } = @"C:\Project\template\template.png";

    public List<(string ProjectPath, string SourcePngPath)> SaveCalls { get; } = new();
    public List<string> GetCalls { get; } = new();
    public List<string> GetPathCalls { get; } = new();

    public TemplateInfo SaveTemplate(string projectPath, string sourcePngPath)
    {
        SaveCalls.Add((projectPath, sourcePngPath));

        if (ExceptionToThrowOnSave is not null)
        {
            throw ExceptionToThrowOnSave;
        }

        return TemplateToReturnOnSave
            ?? new TemplateInfo("template.png", 100, 100);
    }

    public TemplateInfo? GetTemplate(string projectPath)
    {
        GetCalls.Add(projectPath);

        if (ExceptionToThrowOnGet is not null)
        {
            throw ExceptionToThrowOnGet;
        }

        return TemplateToReturnOnGet;
    }

    public string GetTemplatePath(string projectPath)
    {
        GetPathCalls.Add(projectPath);
        return TemplatePathToReturn;
    }
}
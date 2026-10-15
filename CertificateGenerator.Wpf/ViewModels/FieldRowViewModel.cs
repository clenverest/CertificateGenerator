using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel строки списка полей на вкладке «Шаблон».
/// Связывает поле проекта с его размещением (если есть).
/// </summary>
public sealed partial class FieldRowViewModel : ObservableObject
{
    /// <summary>Поле проекта.</summary>
    public Field Field { get; }

    /// <summary>Имя поля для отображения.</summary>
    public string DisplayName => Field.Name;

    /// <summary>
    /// Метка типа поля: «Обычное», «ФИО» или «Статическое».
    /// </summary>
    public string KindLabel => Field.Kind switch
    {
        FieldKind.Simple => "Обычное",
        FieldKind.FullName => "ФИО",
        FieldKind.Static => "Статическое",
        _ => string.Empty
    };

    /// <summary>
    /// Размещение поля. null, если не размещено.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlacement))]
    private FieldPlacementViewModel? _placement;

    /// <summary>Есть ли размещение у поля.</summary>
    public bool HasPlacement => Placement is not null;

    public FieldRowViewModel(Field field)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
    }
}
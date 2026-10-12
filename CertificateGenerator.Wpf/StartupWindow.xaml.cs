using System.Windows;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Wpf;

public partial class StartupWindow : Window
{
    public StartupWindow(ProjectViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
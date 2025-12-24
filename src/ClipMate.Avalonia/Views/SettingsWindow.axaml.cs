using Avalonia.Controls;
using ClipMate.Avalonia.ViewModels;

namespace ClipMate.Avalonia.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}

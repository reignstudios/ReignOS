using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ReignOS.ControlCenter.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.singleton.Close();
    }
}

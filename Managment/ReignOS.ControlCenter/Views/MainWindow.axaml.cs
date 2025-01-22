using Avalonia.Controls;

namespace ReignOS.ControlCenter.Views;

public partial class MainWindow : Window
{
    public static MainWindow singleton { get; private set; }

    public MainWindow()
    {
        singleton = this;
        InitializeComponent();
    }
}

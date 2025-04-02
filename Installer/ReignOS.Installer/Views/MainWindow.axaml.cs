using Avalonia.Controls;
using ReignOS.Core;

namespace ReignOS.Installer.Views;

public partial class MainWindow : Window
{
    public static MainWindow singleton { get; private set; }

    public MainWindow()
    {
        singleton = this;
        InitializeComponent();
        Title = $"{Title} ({VersionInfo.version})";
    }
}

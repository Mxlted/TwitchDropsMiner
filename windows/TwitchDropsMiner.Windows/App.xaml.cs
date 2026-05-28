using System.Windows;
using TwitchDropsMiner.Windows.Core;

namespace TwitchDropsMiner.Windows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string repositoryRoot;
        try
        {
            repositoryRoot = RepositoryLocator.ForCurrentApp().Locate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Twitch Drops Miner",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown(1);
            return;
        }

        MainWindow = new MainWindow(repositoryRoot);
        MainWindow.Show();
    }
}

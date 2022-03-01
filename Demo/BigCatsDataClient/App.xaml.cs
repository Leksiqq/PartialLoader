using System;
using System.Windows;

namespace BigCatsDataClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnActivated(EventArgs e)
        {
            MainWindow.DataContext = MainWindow;
            base.OnActivated(e);
        }
    }
}

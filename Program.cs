using Parcial Lenguaje Zemog._Repositories;
using Parcial Lenguaje Zemog.Models;
using Parcial Lenguaje Zemog.Presenters;
using Parcial Lenguaje Zemog.Properties;
using Parcial Lenguaje Zemog.Views;
using Parcial Lenguaje Zemog.Views;

namespace Parcial Lenguaje Zemog
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            string sqlconnectionString = Settings.Default.SqlConnection;
            IMainView view = new MainView();
            new MainPresenter(view, sqlconnectionString);
            Application.Run((Form)view);
        }
    }
}
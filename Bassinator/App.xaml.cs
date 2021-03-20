using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Bassinator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public delegate void OnExitDelegate();
        public static OnExitDelegate OnExitEvent;

        private void AppExit(object sender, ExitEventArgs e)
        {
            OnExitEvent?.Invoke();
        }
    }
}

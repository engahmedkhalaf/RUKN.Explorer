using Autodesk.Navisworks.Api.Plugins;
using System;
using System.Windows.Forms;

namespace RUKN.Quant
{
    [Plugin("RUKNQuantPlugin", "EVRS", DisplayName = "RUKN Quant Plugin")]
    public class Plugin : CustomPlugin
    {
        private static MainWindow _window;

        public int Execute(params string[] parameters)
        {
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new MainWindow();
                    _window.Closed += (s, e) => { _window = null; };
                    _window.Show();
                }
                else
                {
                    _window.Activate();
                    if (_window.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _window.WindowState = System.Windows.WindowState.Normal;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open RUKN Quant: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 0;
        }
    }
}

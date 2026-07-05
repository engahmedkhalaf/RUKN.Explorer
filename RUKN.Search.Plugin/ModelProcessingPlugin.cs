using Autodesk.Navisworks.Api.Plugins;
using RUKN.Search.Common.Application;
using System.Windows.Interop;

namespace RUKN.Search.Plugin
{
    [Plugin("ModelProcessing", IdentityInformation.DeveloperID, ToolTip = "Model Processing & Viewpoints Generator", DisplayName = "RUKN Explorer")]
    public class ModelProcessingPlugin : CustomPlugin
    {
        private static ModelProcessingWindow _activeWindow;

        public int Execute(params string[] parameters)
        {
            if (_activeWindow != null && _activeWindow.IsLoaded)
            {
                _activeWindow.Focus();
            }
            else
            {
                _activeWindow = new ModelProcessingWindow();

                var hwnd = Autodesk.Navisworks.Api.Application.Gui.MainWindow.Handle;
                var helper = new WindowInteropHelper(_activeWindow);
                helper.Owner = hwnd;

                _activeWindow.Show();
            }
            return 0;
        }
    }
}

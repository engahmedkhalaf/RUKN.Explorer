using Autodesk.Navisworks.Api.Plugins;
using RUKN.Search.Common.Application;
using RUKN.Search.Plugin.Utils;


using System.Windows.Interop;

namespace RUKN.Search.Plugin
{
    [Plugin("SelectByRevitId", IdentityInformation.DeveloperID, ToolTip = "Select by revit element ID", DisplayName = "RUKN Explorer")]
    public class SelectByIdPlugin : CustomPlugin
    {
        private static SelectByIdWindow _activeWindow;

        public int Execute(params string[] parameters)
        {
            Tools.SelectedIds = "";

            if (!Tools.IsRevitModelLoaded())
            {
                MessageWindow.Show("Warning", "You don't have a Revit model loaded, please load one");
            }
            else
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Focus();
                }
                else
                {
                    _activeWindow = new SelectByIdWindow();
                    
                    var hwnd = Autodesk.Navisworks.Api.Application.Gui.MainWindow.Handle;
                    var helper = new WindowInteropHelper(_activeWindow);
                    helper.Owner = hwnd;
                    
                    _activeWindow.Show();
                }
            }

            return 0;
        }
    }
}
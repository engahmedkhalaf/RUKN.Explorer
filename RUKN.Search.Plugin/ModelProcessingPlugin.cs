using Autodesk.Navisworks.Api.Plugins;
using RUKN.Search.Common.Application;
using System.Windows.Interop;

namespace RUKN.Search.Plugin
{
    [Plugin("ModelProcessing", IdentityInformation.DeveloperID, ToolTip = "Model Processing & Viewpoints Generator", DisplayName = "RUKN Insight Pro")]
    public class ModelProcessingPlugin : CustomPlugin
    {
        private static ModelProcessingWindow _activeWindow;

        public int Execute(params string[] parameters)
        {
            if (_activeWindow != null && _activeWindow.IsLoaded)
            {
                _activeWindow.Focus();
                return 0;
            }

            // Verify License/Trial Validity
            if (!IsLicenseOrTrialValid())
            {
                var trialWindow = new RUKN.Search.Plugin.Windows.TrialWindow();
                var mainHwnd = Autodesk.Navisworks.Api.Application.Gui.MainWindow.Handle;
                var trialHelper = new WindowInteropHelper(trialWindow);
                trialHelper.Owner = mainHwnd;

                if (trialWindow.ShowDialog() != true)
                {
                    // User closed/cancelled welcome window without valid trial or license
                    return 0;
                }
            }

            // Launch Main App
            _activeWindow = new ModelProcessingWindow();
            var hwnd = Autodesk.Navisworks.Api.Application.Gui.MainWindow.Handle;
            var helper = new WindowInteropHelper(_activeWindow);
            helper.Owner = hwnd;

            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(_activeWindow);
            _activeWindow.Show();

            return 0;
        }

        private bool IsLicenseOrTrialValid()
        {
            try
            {
                // 1. Check for Paid Active License
                string licKey = SettingsConfig.GetValue("LicenseKey");
                if (!string.IsNullOrEmpty(licKey))
                {
                    return true;
                }

                // 2. Check for Active Trial License
                string trialStartStr = SettingsConfig.GetValue("TrialStartDate");
                if (!string.IsNullOrEmpty(trialStartStr) && System.DateTime.TryParse(trialStartStr, out System.DateTime trialStart))
                {
                    System.DateTime trialExpiry = trialStart.AddDays(14);
                    if (System.DateTime.Now <= trialExpiry)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}

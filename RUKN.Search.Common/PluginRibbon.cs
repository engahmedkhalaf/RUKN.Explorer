using Autodesk.Navisworks.Api.Plugins;
using RUKN.Search.Common.Application;
using RUKN.Search.Common.Utils;
using RUKN.Search.Plugin;
using RUKN.Search.Plugin.Windows;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Rukn.Navisworks.Plugin.Common
{
    [Plugin("SelectByRevitIdRibbon", IdentityInformation.DeveloperID, DisplayName = "RUKN Insight Pro")]
    [RibbonLayout("PluginRibbon.xaml")]
    [RibbonTab("RUKNBIM", DisplayName = "RUKN Insight Pro")]
    [Command("ModelProcessing", Icon = "ModelProcessing_16.png", LargeIcon = "ModelProcessing_32.png", ToolTip = "Model processing and viewpoint generation settings", DisplayName = "Model Processing")]
    [Command("LicenseAgreement", Icon = "License_16.png", LargeIcon = "License_32.png", ToolTip = "RUKNBIM Software License Agreement", DisplayName = "License")]
    public class PluginRibbon : CommonCommandHandlerPlugin
    {
        public override int ExecuteCommand(string name, params string[] parameters)
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pluginFileName = directoryName + $"\\{Assembly.GetExecutingAssembly().GetName().Name}.dll";
            Autodesk.Navisworks.Api.Application.Plugins.AddPluginAssembly(pluginFileName);

            switch (name)
            {
                case "ModelProcessing":
                    try
                    {
                        if (!Autodesk.Navisworks.Api.Application.IsAutomated)
                        {
                            PluginBuilder pluginBuilder = new PluginBuilder("ModelProcessing");
                            if (pluginBuilder.pluginRecord is CustomPluginRecord && pluginBuilder.pluginRecord.IsEnabled)
                            {
                                ModelProcessingPlugin modelProcessingPlugin = (ModelProcessingPlugin)(pluginBuilder.pluginRecord.LoadedPlugin ?? pluginBuilder.pluginRecord.LoadPlugin());
                                modelProcessingPlugin.Execute();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("ups, something went wrong" + Environment.NewLine + ex.Message);
                    }
                    break;

                case "LicenseAgreement":
                    try
                    {
                        if (!Autodesk.Navisworks.Api.Application.IsAutomated)
                        {
                            LicenseWindow licenseWindow = new LicenseWindow();
                            var hwnd = Autodesk.Navisworks.Api.Application.Gui.MainWindow.Handle;
                            var helper = new System.Windows.Interop.WindowInteropHelper(licenseWindow);
                            helper.Owner = hwnd;
                            licenseWindow.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to open License Agreement: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    break;
            }
            return 0;
        }

        public override bool TryShowCommandHelp(string name)
        {
            bool result = base.TryShowCommandHelp("https://www.ruknbim.com/");
            return result;
        }
    }
}
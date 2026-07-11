using Autodesk.Navisworks.Api.Plugins;
using RUKN.Search.Common.Application;
using RUKN.Search.Common.Utils;
using RUKN.Search.Plugin;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Rukn.Navisworks.Plugin.Common
{
    [Plugin("SelectByRevitIdRibbon", IdentityInformation.DeveloperID, DisplayName = "RUKN Explorer")]
    [RibbonLayout("PluginRibbon.xaml")]
    [RibbonTab("RUKNBIM", DisplayName = "RUKN Explorer")]
    [Command("ModelProcessing", Icon = "ElementID_16.ico", LargeIcon = "ElementID_32.png", ToolTip = "Model processing and viewpoint generation settings", DisplayName = "Model Processing")]
    public class PluginRibbon : CommonCommandHandlerPlugin
    {
        public override int ExecuteCommand(string name, params string[] parameters)
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pluginFileName = directoryName + $"\\{Assembly.GetExecutingAssembly().GetName().Name}.dll";
            Autodesk.Navisworks.Api.Application.Plugins.AddPluginAssembly(pluginFileName);

            // Dynamically load the RUKN.Quant assembly if it exists in the sibling directory
            string parentDir = Path.GetDirectoryName(directoryName);
            string quantPluginFileName = string.IsNullOrEmpty(parentDir) ? "" : Path.Combine(parentDir, "RUKN.Quant", "RUKN.Quant.dll");
            if (!string.IsNullOrEmpty(quantPluginFileName) && File.Exists(quantPluginFileName))
            {
                Autodesk.Navisworks.Api.Application.Plugins.AddPluginAssembly(quantPluginFileName);
            }

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
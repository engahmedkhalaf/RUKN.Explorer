using Autodesk.Navisworks.Api.Plugins;
using RUKN.Search.Common.Application;

namespace RUKN.Search.Common.Utils
{
    public class PluginBuilder
    {
        public string pluginId { get; set; }
        public string directoryName { get; set; }
        public string fileName { get; set; }
        public PluginRecord pluginRecord { get; set; }

        public PluginBuilder(string pluginName)
        {
            pluginId = string.Concat(pluginName, ".", IdentityInformation.DeveloperID);
            pluginRecord = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin(pluginId);
        }
    }
}

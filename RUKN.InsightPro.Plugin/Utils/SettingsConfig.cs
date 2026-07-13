using System.Collections.Generic;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml;
using Configuration = System.Configuration.Configuration;
using System.Windows.Input;

namespace RUKN.InsightPro.Plugin
{
    public static class SettingsConfig
    {
        public static readonly string currentVersion = "1.0.0";
        public static readonly string currentApiKey = "PlaceHolderApiKey";

        private static readonly string _configDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RUKNBIM");

        private static readonly string _configFile =
            Path.Combine(_configDir, "ruknbim.config");

        private static readonly object _locker = new object();

        static SettingsConfig()
        {
            // Ensure folder exists
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            // Ensure file exists with defaults
            if (!File.Exists(_configFile))
            {
                CreateDefaultConfig();
            }
            else
            {
                EnsureDefaultKeysExist();
            }
        }

        public static string GetValue(string key)
        {
            if (!File.Exists(_configFile))
                CreateDefaultConfig();

            lock (_locker)
            {
                try
                {
                    Configuration config = OpenConfig();
                    KeyValueConfigurationElement setting = config.AppSettings.Settings[key];
                    return setting != null ? setting.Value : null;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error retrieving value for key '{key}'.", ex);
                }
            }
        }

        public static void SetValue(string key, string value)
        {
            if (!File.Exists(_configFile))
                CreateDefaultConfig();

            lock (_locker)
            {
                try
                {
                    Configuration config = OpenConfig();
                    KeyValueConfigurationCollection settings = config.AppSettings.Settings;

                    if (settings[key] == null)
                        settings.Add(key, value);
                    else
                        settings[key].Value = value;

                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error setting value for key '{key}'.", ex);
                }
            }
        }

        private static Configuration OpenConfig()
        {
            var map = new ExeConfigurationFileMap { ExeConfigFilename = _configFile };
            return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        private static Dictionary<string, string> GetDefaultDictionary()
        {
            return new Dictionary<string, string>
            {
                { "runs",   "0"  },
                { "version",   currentVersion  },
                { "user",   "user01"  },
                { "release",   "0"  },
                { "apikey",   "0"  },
                { "SupabaseUrl", "https://auvtapbsdewwmzejchgq.supabase.co" },
                { "SupabaseAnonKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImF1dnRhcGJzZGV3d216ZWpjaGdxIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODE1MTQ2MjksImV4cCI6MjA5NzA5MDYyOX0.u5S9vIBU-RQ5XOIT_FAohOdyVyW7NjknkkVwCorLWe8" },
                { "LicenseType", "" },
                { "LicenseExpiry", "" },
                { "LicenseMachine", "" }
            };
        }

        private static void EnsureDefaultKeysExist()
        {
            try
            {
                var defaults = GetDefaultDictionary();
                bool modified = false;

                lock (_locker)
                {
                    Configuration config = OpenConfig();
                    KeyValueConfigurationCollection settings = config.AppSettings.Settings;

                    foreach (var kvp in defaults)
                    {
                        if (settings[kvp.Key] == null)
                        {
                            settings.Add(kvp.Key, kvp.Value);
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Creates ruknbim.config with the requested default keys/values.
        /// </summary>
        private static void CreateDefaultConfig()
        {
            var defaults = GetDefaultDictionary();

            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            XmlElement configuration = doc.CreateElement("configuration");
            doc.AppendChild(configuration);

            XmlElement appSettings = doc.CreateElement("appSettings");
            configuration.AppendChild(appSettings);

            foreach (var kvp in defaults)
            {
                XmlElement add = doc.CreateElement("add");
                add.SetAttribute("key", kvp.Key);
                add.SetAttribute("value", kvp.Value);
                appSettings.AppendChild(add);
            }

            doc.Save(_configFile);
        }
    }
}

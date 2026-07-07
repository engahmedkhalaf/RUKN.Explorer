using Autodesk.Navisworks.Api;
using System;

namespace RUKN.Quant
{
    public static class PropertyHelper
    {
        public static string GetRevitId(ModelItem item)
        {
            if (item == null) return string.Empty;
            try
            {
                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    foreach (DataProperty prop in category.Properties)
                    {
                        string name = prop.DisplayName ?? prop.Name;
                        if (name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("Element ID", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("LcRevitId", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = prop.Value;
                            if (val.IsInt32) return val.ToInt32().ToString();
                            if (val.IsDisplayString) return val.ToDisplayString();
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public static double? GetQuantityDouble(ModelItem item, string[] propertyNames)
        {
            if (item == null) return null;
            try
            {
                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    foreach (DataProperty prop in category.Properties)
                    {
                        string name = prop.DisplayName ?? prop.Name;
                        foreach (string target in propertyNames)
                        {
                            if (name.Equals(target, StringComparison.OrdinalIgnoreCase))
                            {
                                var val = prop.Value;
                                if (val.IsDouble) return val.ToDouble();
                                if (val.IsInt32) return val.ToInt32();
                                if (val.IsDisplayString)
                                {
                                    string s = val.ToDisplayString();
                                    // Remove unit suffixes (e.g. m, m², mm) and normalize formats
                                    s = System.Text.RegularExpressions.Regex.Replace(s, @"[^\d\.\-]", "").Trim();
                                    if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                                    {
                                        return d;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public static string GetPropertyString(ModelItem item, string categoryName, string propertyName)
        {
            if (item == null) return string.Empty;
            try
            {
                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    string catName = category.DisplayName ?? category.Name;
                    if (catName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (DataProperty prop in category.Properties)
                        {
                            string propName = prop.DisplayName ?? prop.Name;
                            if (propName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                            {
                                return prop.Value.ToDisplayString() ?? string.Empty;
                            }
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }
    }
}

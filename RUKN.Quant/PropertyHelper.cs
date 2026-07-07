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
                            name.Equals("Revit ID", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("LcRevitId", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = prop.Value;
                            string valStr = val.ToDisplayString();
                            if (!string.IsNullOrEmpty(valStr)) return valStr;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GetRevitIdDeep(ModelItem item)
        {
            if (item == null) return string.Empty;

            // 1. Check item itself
            string val = GetRevitId(item);
            if (!string.IsNullOrEmpty(val)) return val;

            // 2. Check ancestors (walk up)
            try
            {
                foreach (ModelItem ancestor in item.Ancestors)
                {
                    val = GetRevitId(ancestor);
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }

            // 3. Check descendants (walk down)
            try
            {
                foreach (ModelItem child in item.Descendants)
                {
                    val = GetRevitId(child);
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }

            return string.Empty;
        }

        public static double? GetQuantityDouble(ModelItem item, string categoryName, string[] targetKeywords)
        {
            if (item == null) return null;
            try
            {
                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    string catName = category.DisplayName ?? category.Name;
                    if (!catName.Equals(categoryName, StringComparison.OrdinalIgnoreCase)) continue;

                    foreach (DataProperty prop in category.Properties)
                    {
                        string name = prop.DisplayName ?? prop.Name;
                        if (string.IsNullOrEmpty(name)) continue;

                        foreach (string keyword in targetKeywords)
                        {
                            if (name.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                double? parsedVal = ParseDouble(prop.Value.ToDisplayString());
                                if (parsedVal.HasValue && parsedVal.Value > 0) return parsedVal.Value;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public static double? GetQuantityDouble(ModelItem item, string[] targetKeywords)
        {
            if (item == null) return null;
            try
            {
                // Pass 1: Try exact matching
                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    foreach (DataProperty prop in category.Properties)
                    {
                        string name = prop.DisplayName ?? prop.Name;
                        if (string.IsNullOrEmpty(name)) continue;

                        foreach (string keyword in targetKeywords)
                        {
                            if (name.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                double? parsedVal = ParseDouble(prop.Value.ToDisplayString());
                                if (parsedVal.HasValue && parsedVal.Value > 0) return parsedVal.Value;
                            }
                        }
                    }
                }

                // Pass 2: Try partial matching
                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    foreach (DataProperty prop in category.Properties)
                    {
                        string name = prop.DisplayName ?? prop.Name;
                        if (string.IsNullOrEmpty(name)) continue;

                        if (name.IndexOf("ID", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Code", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Type", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        foreach (string keyword in targetKeywords)
                        {
                            if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                double? parsedVal = ParseDouble(prop.Value.ToDisplayString());
                                if (parsedVal.HasValue && parsedVal.Value > 0) return parsedVal.Value;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public static double? GetQuantityDoubleDeep(ModelItem item, string categoryName, string[] targetKeywords)
        {
            if (item == null) return null;

            // 1. Check item itself (restricted to target categoryName first)
            double? val = GetQuantityDouble(item, categoryName, targetKeywords);
            if (val.HasValue) return val.Value;

            // 2. Check ancestors (walk up, restricted to categoryName)
            try
            {
                foreach (ModelItem ancestor in item.Ancestors)
                {
                    val = GetQuantityDouble(ancestor, categoryName, targetKeywords);
                    if (val.HasValue) return val.Value;
                }
            }
            catch { }

            // 3. Check descendants (walk down, restricted to categoryName)
            try
            {
                foreach (ModelItem child in item.Descendants)
                {
                    val = GetQuantityDouble(child, categoryName, targetKeywords);
                    if (val.HasValue) return val.Value;
                }
            }
            catch { }

            // FALLBACK: If not found in target categoryName, search all tabs
            val = GetQuantityDouble(item, targetKeywords);
            if (val.HasValue) return val.Value;

            try
            {
                foreach (ModelItem ancestor in item.Ancestors)
                {
                    val = GetQuantityDouble(ancestor, targetKeywords);
                    if (val.HasValue) return val.Value;
                }
            }
            catch { }

            try
            {
                foreach (ModelItem child in item.Descendants)
                {
                    val = GetQuantityDouble(child, targetKeywords);
                    if (val.HasValue) return val.Value;
                }
            }
            catch { }

            return null;
        }

        private static double? ParseDouble(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            try
            {
                // Keep only digits, dots, commas, and minus sign
                s = System.Text.RegularExpressions.Regex.Replace(s, @"[^\d\.\,\-]", "").Trim();
                
                // Replace comma with dot if comma is used as decimal separator
                if (s.Contains(",") && !s.Contains("."))
                {
                    s = s.Replace(",", ".");
                }
                else if (s.Contains(",") && s.Contains("."))
                {
                    if (s.IndexOf(',') < s.IndexOf('.'))
                    {
                        s = s.Replace(",", "");
                    }
                    else
                    {
                        s = s.Replace(".", "").Replace(",", ".");
                    }
                }

                if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    return d;
                }
            }
            catch { }
            return null;
        }

        public static string GetPropertyString(ModelItem item, string categoryName, string[] propertyNames)
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
                            foreach (string target in propertyNames)
                            {
                                if (propName.Equals(target, StringComparison.OrdinalIgnoreCase))
                                {
                                    return prop.Value.ToDisplayString() ?? string.Empty;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GetPropertyStringDeep(ModelItem item, string categoryName, string[] propertyNames)
        {
            if (item == null) return string.Empty;

            // 1. Check item itself
            string val = GetPropertyString(item, categoryName, propertyNames);
            if (!string.IsNullOrEmpty(val)) return val;

            // 2. Check ancestors (walk up)
            try
            {
                foreach (ModelItem ancestor in item.Ancestors)
                {
                    val = GetPropertyString(ancestor, categoryName, propertyNames);
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }

            // 3. Check descendants (walk down)
            try
            {
                foreach (ModelItem child in item.Descendants)
                {
                    val = GetPropertyString(child, categoryName, propertyNames);
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }

            return string.Empty;
        }
    }
}

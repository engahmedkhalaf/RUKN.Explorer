using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace RUKN.Search.Plugin
{
    public partial class ModelProcessingWindow : Window
    {
        private string _previousUnit = "m";

        public ModelProcessingWindow()
        {
            InitializeComponent();
            PopulateModels();
            LoadWindowSettings();
        }

        private void LoadWindowSettings()
        {
            try
            {
                string checkTop = SettingsConfig.GetValue("CheckOffsetTop");
                if (checkTop != null && bool.TryParse(checkTop, out bool ct)) 
                    CheckOffsetTop.IsChecked = ct;

                string textTop = SettingsConfig.GetValue("TextOffsetTop");
                if (textTop != null) TextOffsetTop.Text = textTop;

                string checkBottom = SettingsConfig.GetValue("CheckOffsetBottom");
                if (checkBottom != null && bool.TryParse(checkBottom, out bool cb)) 
                    CheckOffsetBottom.IsChecked = cb;

                string textBottom = SettingsConfig.GetValue("TextOffsetBottom");
                if (textBottom != null) TextOffsetBottom.Text = textBottom;

                string unit = SettingsConfig.GetValue("Unit");
                if (unit != null)
                {
                    _previousUnit = unit;
                    RadioMM.IsChecked = (unit == "mm");
                    RadioCM.IsChecked = (unit == "cm");
                    RadioM.IsChecked = (unit == "m");
                    RadioFT.IsChecked = (unit == "ft");
                }
            }
            catch (Exception) { }
        }

        private void SaveWindowSettings()
        {
            try
            {
                SettingsConfig.SetValue("CheckOffsetTop", CheckOffsetTop.IsChecked.ToString());
                SettingsConfig.SetValue("TextOffsetTop", TextOffsetTop.Text);

                SettingsConfig.SetValue("CheckOffsetBottom", CheckOffsetBottom.IsChecked.ToString());
                SettingsConfig.SetValue("TextOffsetBottom", TextOffsetBottom.Text);

                string unit = "m";
                if (RadioMM.IsChecked == true) unit = "mm";
                else if (RadioCM.IsChecked == true) unit = "cm";
                else if (RadioFT.IsChecked == true) unit = "ft";
                SettingsConfig.SetValue("Unit", unit);
            }
            catch (Exception) { }
        }

        private void Unit_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            string newUnit = "m";
            if (RadioMM.IsChecked == true) newUnit = "mm";
            else if (RadioCM.IsChecked == true) newUnit = "cm";
            else if (RadioFT.IsChecked == true) newUnit = "ft";

            if (newUnit == _previousUnit) return;

            // Convert Top Offset
            if (TryParseDouble(TextOffsetTop.Text, out double ot))
            {
                double converted = ConvertValue(ot, _previousUnit, newUnit);
                TextOffsetTop.Text = (converted % 1 == 0) ? converted.ToString("F0") : converted.ToString("0.##");
            }

            // Convert Bottom Offset
            if (TryParseDouble(TextOffsetBottom.Text, out double ob))
            {
                double converted = ConvertValue(ob, _previousUnit, newUnit);
                TextOffsetBottom.Text = (converted % 1 == 0) ? converted.ToString("F0") : converted.ToString("0.##");
            }

            _previousUnit = newUnit;
        }

        private double ConvertValue(double value, string fromUnit, string toUnit)
        {
            // baseline is meters
            double meters = value;
            if (fromUnit == "mm") meters = value / 1000.0;
            else if (fromUnit == "cm") meters = value / 100.0;
            else if (fromUnit == "ft") meters = value * 0.3048;

            if (toUnit == "mm") return meters * 1000.0;
            if (toUnit == "cm") return meters * 100.0;
            if (toUnit == "ft") return meters / 0.3048;
            return meters;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowSettings();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Close_Button(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Ruknbim_Link(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://ruknbim.com") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open link: " + ex.Message);
            }
        }

        private void ReadModels_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBlockStatus.Text = "Refreshing models and levels from source...";
                PopulateModels();
                TextBlockStatus.Text = "Model source successfully refreshed!";
            }
            catch (Exception ex)
            {
                TextBlockStatus.Text = "Error refreshing: " + ex.Message;
                MessageBox.Show("Failed to refresh models: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double ConvertToMeters(double value)
        {
            if (RadioMM.IsChecked == true)
                return value / 1000.0;
            if (RadioCM.IsChecked == true)
                return value / 100.0;
            if (RadioFT.IsChecked == true)
                return value * 0.3048;
            return value; // Meters
        }

        private string GetUnitText()
        {
            if (RadioMM.IsChecked == true) return "mm";
            if (RadioCM.IsChecked == true) return "cm";
            if (RadioFT.IsChecked == true) return "ft";
            return "m";
        }

        private bool TryParseDouble(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text)) return false;

            // Replace commas with dots and trim
            string normalized = text.Replace(',', '.').Trim();
            
            // Try parsing using InvariantCulture (which expects dot as decimal separator)
            if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            // Fallback to default parsing (which uses local culture settings)
            return double.TryParse(text.Trim(), out value);
        }

        private void GenerateViewpoints_Click(object sender, RoutedEventArgs e)
        {
            if (ComboModel.SelectedItem == null)
            {
                TextBlockStatus.Text = "Error: No model selected.";
                return;
            }

            string selectedModel = ComboModel.SelectedItem.ToString();
            if (selectedModel == "No models loaded")
            {
                TextBlockStatus.Text = "Error: No models loaded.";
                return;
            }

            // Find checked levels
            var checkedLevels = new System.Collections.Generic.List<string>();
            foreach (var child in PanelLevels.Children)
            {
                if (child is CheckBox cb && cb.IsChecked == true)
                {
                    checkedLevels.Add(cb.Content.ToString());
                }
            }

            if (checkedLevels.Count == 0)
            {
                TextBlockStatus.Text = "Error: No levels selected.";
                return;
            }

            TextBlockStatus.Text = $"Generating {checkedLevels.Count} viewpoint(s)...";

            int generatedCount = 0;
            Autodesk.Navisworks.Api.ModelItemCollection originalSelection = null;
            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                string unitText = GetUnitText();
                // Precache level elevations once with coordinate transformation to avoid slow repeated search queries in the loop
                PrecacheLevelElevations(selectedModel);

                var allElevations = GetAllLevelElevations(selectedModel);

                // Save current selection to restore at the end
                if (doc != null && doc.CurrentSelection != null && doc.CurrentSelection.SelectedItems != null)
                {
                    originalSelection = new Autodesk.Navisworks.Api.ModelItemCollection(doc.CurrentSelection.SelectedItems);
                }
                
                foreach (string levelName in checkedLevels)
                {
                    double? elevation = GetLevelElevation(selectedModel, levelName);
                    if (elevation.HasValue)
                    {
                        double? topZ = null;
                        double? bottomZ = null;

                        // 1. Calculate Bottom Z
                        if (CheckOffsetBottom.IsChecked == true && TryParseDouble(TextOffsetBottom.Text, out double offsetBottom))
                        {
                            bottomZ = elevation.Value + ConvertToMeters(offsetBottom);
                        }
                        else
                        {
                            // Default: Cut exactly at the floor level
                            bottomZ = elevation.Value;
                        }

                        // 2. Calculate Top Z
                        if (CheckOffsetTop.IsChecked == true && TryParseDouble(TextOffsetTop.Text, out double offsetTop))
                        {
                            topZ = elevation.Value + ConvertToMeters(offsetTop);
                        }
                        else
                        {
                            // Default: Find the next level's elevation to slice only this floor
                            double? nextElevation = null;
                            foreach (double el in allElevations)
                            {
                                if (el > elevation.Value + 0.01)
                                {
                                    nextElevation = el;
                                    break;
                                }
                            }

                            if (nextElevation.HasValue)
                            {
                                topZ = nextElevation.Value;
                            }
                            else
                            {
                                // Top floor fallback: current elevation + 4.0 meters
                                topZ = elevation.Value + 4.0;
                            }
                        }

                        // Apply the section cut using COM API
                        ApplySectionCut(topZ, bottomZ);

                        // Select the level's ModelItem before capturing the viewpoint
                        var levelItem = GetLevelModelItem(selectedModel, levelName);
                        if (levelItem != null && doc != null && doc.CurrentSelection != null)
                        {
                            var collection = new Autodesk.Navisworks.Api.ModelItemCollection();
                            collection.Add(levelItem);
                            doc.CurrentSelection.CopyFrom(collection);
                        }

                        // Capture current view state into a new viewpoint
                        Autodesk.Navisworks.Api.Viewpoint vp = doc.CurrentViewpoint.CreateCopy();
                        
                        // Create a SavedViewpoint
                        Autodesk.Navisworks.Api.SavedViewpoint savedVp = new Autodesk.Navisworks.Api.SavedViewpoint(vp);
                        
                        // Determine display name with top/bottom offset details
                        string displayName = $"{selectedModel} - {levelName}";
                        var details = new System.Collections.Generic.List<string>();
                        if (CheckOffsetTop.IsChecked == true && TryParseDouble(TextOffsetTop.Text, out double ot) && ot != 0)
                        {
                            details.Add($"Top Z: {(ot > 0 ? "+" : "")}{ot}{unitText}");
                        }
                        if (CheckOffsetBottom.IsChecked == true && TryParseDouble(TextOffsetBottom.Text, out double ob) && ob != 0)
                        {
                            details.Add($"Bottom Z: {(ob > 0 ? "+" : "")}{ob}{unitText}");
                        }
                        if (details.Count > 0)
                        {
                            displayName += $" ({string.Join(", ", details)})";
                        }
                        savedVp.DisplayName = displayName;

                        // Save to document
                        doc.SavedViewpoints.AddCopy(savedVp);
                        generatedCount++;
                    }
                }

                TextBlockStatus.Text = $"Successfully generated {generatedCount} viewpoint(s)!";
            }
            catch (Exception ex)
            {
                TextBlockStatus.Text = "Error: " + ex.Message;
                MessageBox.Show("Viewpoint generation failed: " + ex.Message);
            }
            finally
            {
                // Reset sectioning on the current live view to avoid leaving the screen cut
                ClearSectioning();

                // Restore original selection
                if (originalSelection != null)
                {
                    try
                    {
                        var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                        if (doc != null && doc.CurrentSelection != null)
                        {
                            doc.CurrentSelection.CopyFrom(originalSelection);
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private Autodesk.Navisworks.Api.ModelItem GetLevelModelItem(string selectedModelName, string levelNameName)
        {
            try
            {
                if (Autodesk.Navisworks.Api.Application.ActiveDocument != null)
                {
                    foreach (Autodesk.Navisworks.Api.Model model in Autodesk.Navisworks.Api.Application.ActiveDocument.Models)
                    {
                        string modelName = model.RootItem != null ? model.RootItem.DisplayName : System.IO.Path.GetFileNameWithoutExtension(model.SourceFileName);
                        if (modelName == selectedModelName)
                        {
                            if (model.RootItem != null)
                            {
                                foreach (Autodesk.Navisworks.Api.ModelItem child in model.RootItem.Children)
                                {
                                    if (child.DisplayName == levelNameName)
                                    {
                                        return child;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        private System.Collections.Generic.Dictionary<string, double> _cachedLevelElevations = new System.Collections.Generic.Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase);

        private void PrecacheLevelElevations(string selectedModelName)
        {
            _cachedLevelElevations.Clear();
            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                if (doc != null)
                {
                    // Find the selected model's transform
                    var modelTransform = Autodesk.Navisworks.Api.Transform3D.CreateTranslation(new Autodesk.Navisworks.Api.Vector3D(0, 0, 0));
                    foreach (Autodesk.Navisworks.Api.Model model in doc.Models)
                    {
                        string modelName = model.RootItem != null ? model.RootItem.DisplayName : System.IO.Path.GetFileNameWithoutExtension(model.SourceFileName);
                        if (modelName == selectedModelName)
                        {
                            modelTransform = model.Transform;
                            break;
                        }
                    }

                    // 1. Check active grid system levels first, transform to world coordinates, and cache
                    try
                    {
                        var activeSys = doc.Grids.ActiveSystem;
                        if (activeSys != null && activeSys.Levels != null)
                        {
                            foreach (var gridLevel in activeSys.Levels)
                            {
                                if (!string.IsNullOrEmpty(gridLevel.DisplayName))
                                {
                                    var comps = modelTransform.Factor();
                                    double worldZ = gridLevel.Elevation * comps.Scale.Z + comps.Translation.Z;
                                    _cachedLevelElevations[gridLevel.DisplayName] = worldZ;
                                }
                            }
                        }
                    }
                    catch { }

                    // 2. Search category "Levels" model items (already in world space)
                    try
                    {
                        var search = new Autodesk.Navisworks.Api.Search();
                        search.Selection.SelectAll();
                        search.SearchConditions.Add(
                            Autodesk.Navisworks.Api.SearchCondition.HasPropertyByDisplayName("Element", "Category")
                                .EqualValue(Autodesk.Navisworks.Api.VariantData.FromDisplayString("Levels")));
                        
                        var results = search.FindAll(doc, false);
                        foreach (var item in results)
                        {
                            if (item.DisplayName != null)
                            {
                                bool belongsToModel = false;
                                foreach (var ancestor in item.Ancestors)
                                {
                                    string ancestorName = ancestor.DisplayName ?? "";
                                    if (ancestorName.IndexOf(selectedModelName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        belongsToModel = true;
                                        break;
                                    }
                                }
                                if (belongsToModel)
                                {
                                    var bbox = item.BoundingBox();
                                    if (bbox != null)
                                    {
                                        if (!_cachedLevelElevations.ContainsKey(item.DisplayName))
                                        {
                                            _cachedLevelElevations[item.DisplayName] = bbox.Min.Z;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    // 3. Search category "Grids" model items (already in world space)
                    try
                    {
                        var search = new Autodesk.Navisworks.Api.Search();
                        search.Selection.SelectAll();
                        search.SearchConditions.Add(
                            Autodesk.Navisworks.Api.SearchCondition.HasPropertyByDisplayName("Element", "Category")
                                .EqualValue(Autodesk.Navisworks.Api.VariantData.FromDisplayString("Grids")));
                        
                        var results = search.FindAll(doc, false);
                        foreach (var item in results)
                        {
                            if (item.DisplayName != null)
                            {
                                bool belongsToModel = false;
                                foreach (var ancestor in item.Ancestors)
                                {
                                    string ancestorName = ancestor.DisplayName ?? "";
                                    if (ancestorName.IndexOf(selectedModelName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        belongsToModel = true;
                                        break;
                                    }
                                }
                                if (belongsToModel)
                                {
                                    var bbox = item.BoundingBox();
                                    if (bbox != null)
                                    {
                                        if (!_cachedLevelElevations.ContainsKey(item.DisplayName))
                                        {
                                            _cachedLevelElevations[item.DisplayName] = bbox.Min.Z;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private double? GetLevelElevationFromProperties(Autodesk.Navisworks.Api.ModelItem item)
        {
            if (item == null) return null;

            try
            {
                foreach (Autodesk.Navisworks.Api.PropertyCategory category in item.PropertyCategories)
                {
                    foreach (Autodesk.Navisworks.Api.DataProperty prop in category.Properties)
                    {
                        if (prop.DisplayName.Equals("Elevation", System.StringComparison.OrdinalIgnoreCase) ||
                            prop.Name.Equals("Elevation", System.StringComparison.OrdinalIgnoreCase) ||
                            prop.DisplayName.Equals("Elevation Height", System.StringComparison.OrdinalIgnoreCase))
                        {
                            var val = prop.Value;
                            if (val.IsDouble)
                            {
                                return val.ToDouble();
                            }
                            else if (val.IsDisplayString)
                            {
                                if (double.TryParse(val.ToDisplayString(), out double d))
                                {
                                    return d;
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception) { }

            return null;
        }

        private double? GetLevelElevation(string selectedModelName, string levelNameName)
        {
            // 1. Try to read from the cached level elevations dictionary first (highest priority, cached world Z coordinate)
            if (_cachedLevelElevations.TryGetValue(levelNameName, out double cachedElevation))
            {
                return cachedElevation;
            }

            // 2. Fallback: Try properties (transformed to world space) or bounding box Center Z of the level folder (second priority)
            try
            {
                if (Autodesk.Navisworks.Api.Application.ActiveDocument != null)
                {
                    foreach (Autodesk.Navisworks.Api.Model model in Autodesk.Navisworks.Api.Application.ActiveDocument.Models)
                    {
                        string modelName = model.RootItem != null ? model.RootItem.DisplayName : System.IO.Path.GetFileNameWithoutExtension(model.SourceFileName);
                        if (modelName == selectedModelName)
                        {
                            if (model.RootItem != null)
                            {
                                foreach (Autodesk.Navisworks.Api.ModelItem child in model.RootItem.Children)
                                {
                                    if (child.DisplayName == levelNameName)
                                    {
                                        double? propElevation = GetLevelElevationFromProperties(child);
                                        if (propElevation.HasValue)
                                        {
                                            var comps = model.Transform.Factor();
                                            double worldZ = propElevation.Value * comps.Scale.Z + comps.Translation.Z;
                                            return worldZ;
                                        }

                                        // Fallback to bounding box Center Z (world space)
                                        var bbox = child.BoundingBox();
                                        if (bbox != null)
                                        {
                                            return (bbox.Min.Z + bbox.Max.Z) / 2.0;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (System.Exception) { }

            return null;
        }

        private System.Collections.Generic.List<double> GetAllLevelElevations(string selectedModelName)
        {
            var elevations = new System.Collections.Generic.List<double>();
            try
            {
                foreach (var child in PanelLevels.Children)
                {
                    if (child is CheckBox cb)
                    {
                        string levelName = cb.Content.ToString();
                        double? elevation = GetLevelElevation(selectedModelName, levelName);
                        if (elevation.HasValue)
                        {
                            elevations.Add(elevation.Value);
                        }
                    }
                }
            }
            catch (Exception) { }

            // Sort ascending
            elevations.Sort();

            // Remove duplicates
            var uniqueElevations = new System.Collections.Generic.List<double>();
            foreach (double el in elevations)
            {
                if (uniqueElevations.Count == 0 || Math.Abs(uniqueElevations[uniqueElevations.Count - 1] - el) > 0.001)
                {
                    uniqueElevations.Add(el);
                }
            }
            return uniqueElevations;
        }

        private void ApplySectionCut(double? topZ, double? bottomZ)
        {
            try
            {
                var state = Autodesk.Navisworks.Api.ComApi.ComApiBridge.State;
                var curView = state.CurrentView;
                if (curView == null) return;
                var clipColl = (Autodesk.Navisworks.Api.Interop.ComApi.InwClippingPlaneColl2)curView.ClippingPlanes();
                if (clipColl == null) return;

                // Make sure we have at least two planes
                if (clipColl.Count < 1)
                {
                    clipColl.CreatePlane(1);
                }
                if (clipColl.Count < 2)
                {
                    clipColl.CreatePlane(2);
                }

                var plane1 = (Autodesk.Navisworks.Api.Interop.ComApi.InwOaClipPlane)clipColl[1];
                var plane2 = (Autodesk.Navisworks.Api.Interop.ComApi.InwOaClipPlane)clipColl[2];

                // Configure Plane 1 (Top Cut)
                if (topZ.HasValue && plane1 != null)
                {
                    // Normal vector pointing down (0, 0, -1) to cut the top off
                    var normal = (Autodesk.Navisworks.Api.Interop.ComApi.InwLUnitVec3f)state.ObjectFactory(
                        Autodesk.Navisworks.Api.Interop.ComApi.nwEObjectType.eObjectType_nwLUnitVec3f, null, null);
                    normal.SetValue(0, 0, -1);

                    var plane = (Autodesk.Navisworks.Api.Interop.ComApi.InwLPlane3f)state.ObjectFactory(
                        Autodesk.Navisworks.Api.Interop.ComApi.nwEObjectType.eObjectType_nwLPlane3f, null, null);
                    plane.SetValue(normal, -topZ.Value);

                    plane1.Plane = plane;
                    plane1.Enabled = true;
                }
                else if (plane1 != null)
                {
                    plane1.Enabled = false;
                }

                // Configure Plane 2 (Bottom Cut)
                if (bottomZ.HasValue && plane2 != null)
                {
                    // Normal vector pointing up (0, 0, 1) to cut the bottom off
                    var normal = (Autodesk.Navisworks.Api.Interop.ComApi.InwLUnitVec3f)state.ObjectFactory(
                        Autodesk.Navisworks.Api.Interop.ComApi.nwEObjectType.eObjectType_nwLUnitVec3f, null, null);
                    normal.SetValue(0, 0, 1);

                    var plane = (Autodesk.Navisworks.Api.Interop.ComApi.InwLPlane3f)state.ObjectFactory(
                        Autodesk.Navisworks.Api.Interop.ComApi.nwEObjectType.eObjectType_nwLPlane3f, null, null);
                    plane.SetValue(normal, -bottomZ.Value);

                    plane2.Plane = plane;
                    plane2.Enabled = true;
                }
                else if (plane2 != null)
                {
                    plane2.Enabled = false;
                }

                // Save changes back to the current view
                state.CurrentView = curView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying section cut: " + ex.Message);
            }
        }

        private void ClearSectioning()
        {
            try
            {
                var state = Autodesk.Navisworks.Api.ComApi.ComApiBridge.State;
                var curView = state.CurrentView;
                if (curView == null) return;
                var clipColl = (Autodesk.Navisworks.Api.Interop.ComApi.InwClippingPlaneColl2)curView.ClippingPlanes();
                if (clipColl == null) return;
                for (int i = 1; i <= clipColl.Count; i++)
                {
                    var plane = (Autodesk.Navisworks.Api.Interop.ComApi.InwOaClipPlane)clipColl[i];
                    if (plane != null)
                    {
                        plane.Enabled = false;
                    }
                }
                state.CurrentView = curView;
            }
            catch (Exception) { }
        }

        private void ClearSavedViews_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                if (doc != null && doc.SavedViewpoints != null)
                {
                    var result = MessageBox.Show("Are you sure you want to delete all saved viewpoints?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        doc.SavedViewpoints.Clear();
                        TextBlockStatus.Text = "Successfully cleared all saved viewpoints.";
                    }
                }
            }
            catch (Exception ex)
            {
                TextBlockStatus.Text = "Error clearing viewpoints: " + ex.Message;
                MessageBox.Show("Failed to clear viewpoints: " + ex.Message);
            }
        }

        private class ViewpointReportItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string ModelName { get; set; } = "N/A";
            public string Level { get; set; } = "N/A";
            public string TopZ { get; set; } = "N/A";
            public string BottomZ { get; set; } = "N/A";
            public Autodesk.Navisworks.Api.SavedViewpoint SavedVp { get; set; }
        }

        private void CollectViewpoints(Autodesk.Navisworks.Api.SavedItem item, string parentPath, System.Collections.Generic.List<ViewpointReportItem> results)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? item.DisplayName : parentPath + "/" + item.DisplayName;

            if (item.IsGroup)
            {
                var group = (Autodesk.Navisworks.Api.GroupItem)item;
                foreach (var child in group.Children)
                {
                    CollectViewpoints(child, currentPath, results);
                }
            }
            else
            {
                results.Add(new ViewpointReportItem
                {
                    Name = item.DisplayName,
                    Path = parentPath,
                    SavedVp = item as Autodesk.Navisworks.Api.SavedViewpoint
                });
            }
        }

        private ViewpointReportItem ParseViewpointName(string displayName, string path)
        {
            var item = new ViewpointReportItem
            {
                Name = displayName,
                Path = path
            };

            try
            {
                // Format: {ModelName} - {Level} (Top Z: {topZ}, Bottom Z: {bottomZ})
                if (displayName.Contains("(Top Z:") && displayName.Contains("Bottom Z:"))
                {
                    int parenIndex = displayName.IndexOf('(');
                    if (parenIndex > 0)
                    {
                        string mainPart = displayName.Substring(0, parenIndex).Trim();
                        string offsetPart = displayName.Substring(parenIndex).Trim();

                        // Parse main part
                        int dashIndex = mainPart.IndexOf(" - ");
                        if (dashIndex > 0)
                        {
                            item.ModelName = mainPart.Substring(0, dashIndex).Trim();
                            item.Level = mainPart.Substring(dashIndex + 3).Trim();
                        }
                        else
                        {
                            item.Level = mainPart;
                        }

                        // Parse offset part: (Top Z: -2m, Bottom Z: -5m)
                        string cleanedOffsets = offsetPart.Replace("(", "").Replace(")", "").Trim();
                        string[] parts = cleanedOffsets.Split(',');
                        foreach (var part in parts)
                        {
                            string kv = part.Trim();
                            if (kv.StartsWith("Top Z:", StringComparison.OrdinalIgnoreCase))
                            {
                                item.TopZ = kv.Substring(6).Trim();
                            }
                            else if (kv.StartsWith("Bottom Z:", StringComparison.OrdinalIgnoreCase))
                            {
                                item.BottomZ = kv.Substring(9).Trim();
                            }
                        }
                    }
                }
                else if (displayName.Contains(" : "))
                {
                    // Fallback parse: WPH-MBU-MOD-3DM-BWK-00-WHM-50001.rvt : TOWER IIII : location QIC Shar
                    string[] split = displayName.Split(new[] { " : " }, StringSplitOptions.None);
                    if (split.Length > 0) item.ModelName = split[0].Trim();
                    if (split.Length > 1) item.Level = split[1].Trim();
                }
            }
            catch { }

            return item;
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null || doc.SavedViewpoints == null)
            {
                TextBlockStatus.Text = "Error: No active document or viewpoints.";
                return;
            }

            // Collect all saved viewpoints recursively
            var viewpoints = new System.Collections.Generic.List<ViewpointReportItem>();
            foreach (var item in doc.SavedViewpoints.Value)
            {
                CollectViewpoints(item, "", viewpoints);
            }

            if (viewpoints.Count == 0)
            {
                TextBlockStatus.Text = "Error: No saved viewpoints found to export.";
                MessageBox.Show("No saved viewpoints found in the active model.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open Save File Dialog for XLSX
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "Navisworks_Viewpoints_Takeoff_Report.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                TextBlockStatus.Text = "Exporting to Excel...";

                try
                {
                    Type excelType = Type.GetTypeFromProgID("Excel.Application");
                    if (excelType == null)
                    {
                        throw new Exception("Microsoft Excel is not installed on this system.");
                    }

                    dynamic excel = Activator.CreateInstance(excelType);
                    excel.Visible = true; // Show Excel to the user
                    dynamic workbooks = excel.Workbooks;
                    dynamic workbook = workbooks.Add();
                    dynamic sheet = workbook.ActiveSheet;
                    sheet.Name = "Viewpoints Report";

                    // Write Title Header Info
                    sheet.Cells[1, 1] = "RUKN EXPLORER - ENHANCED VIEWPOINTS REPORT";
                    sheet.Cells[2, 1] = $"Total Viewpoints: {viewpoints.Count}";
                    sheet.Cells[3, 1] = $"Exported Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                    // Style Title Block
                    dynamic titleRange = sheet.Range["A1"];
                    titleRange.Font.Bold = true;
                    titleRange.Font.Size = 14;
                    titleRange.Font.Color = 13528603; // Brand Blue (#1B6ECE)

                    dynamic infoRange = sheet.Range["A2", "A3"];
                    infoRange.Font.Italic = true;
                    infoRange.Font.Size = 10;

                    // Write Column Headers on Row 5
                    sheet.Cells[5, 1] = "Folder Path";
                    sheet.Cells[5, 2] = "Viewpoint Display Name";
                    sheet.Cells[5, 3] = "Model/Source File";
                    sheet.Cells[5, 4] = "BIM Level";
                    sheet.Cells[5, 5] = "Top Offset";
                    sheet.Cells[5, 6] = "Bottom Offset";
                    sheet.Cells[5, 7] = "Viewpoint Screenshot";

                    // Style Column Headers (Row 5: A5 to G5)
                    dynamic headerRange = sheet.Range["A5", "G5"];
                    headerRange.Font.Bold = true;
                    headerRange.Font.Color = 16777215; // White text (#FFFFFF)
                    headerRange.Interior.Color = 13528603; // Brand Blue (#1B6ECE)

                    // Make the Screenshot column wider
                    sheet.Columns["G"].ColumnWidth = 22;

                    int row = 6;
                    foreach (var vp in viewpoints)
                    {
                        var parsed = ParseViewpointName(vp.Name, vp.Path);
                        string folderPath = string.IsNullOrEmpty(parsed.Path) ? "Root" : parsed.Path;

                        sheet.Cells[row, 1] = folderPath;
                        sheet.Cells[row, 2] = parsed.Name ?? "";
                        sheet.Cells[row, 3] = parsed.ModelName ?? "";
                        sheet.Cells[row, 4] = parsed.Level ?? "";
                        sheet.Cells[row, 5] = parsed.TopZ ?? "";
                        sheet.Cells[row, 6] = parsed.BottomZ ?? "";

                        // Set Row Height to fit the screenshot image
                        sheet.Rows[row].RowHeight = 85;

                        // Switch viewpoint to capture correct screenshot
                        if (vp.SavedVp != null)
                        {
                            doc.SavedViewpoints.CurrentSavedViewpoint = vp.SavedVp;
                        }

                        // Generate viewpoint thumbnail screenshot image
                        string tempImgPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"vp_thumb_{Guid.NewGuid():N}.png");
                        bool hasImage = false;

                        try
                        {
                            Autodesk.Navisworks.Api.Application.Automation.GenerateThumbnail(320, 240, tempImgPath);
                            if (System.IO.File.Exists(tempImgPath))
                            {
                                hasImage = true;
                            }
                        }
                        catch { }

                        // Embed screenshot into Column 7 (G)
                        if (hasImage)
                        {
                            try
                            {
                                dynamic cell = sheet.Cells[row, 7];
                                double left = (double)cell.Left + 5;
                                double top = (double)cell.Top + 5;
                                double imgWidth = 110;
                                double imgHeight = 75;

                                sheet.Shapes.AddPicture(tempImgPath, false, true, left, top, imgWidth, imgHeight);
                            }
                            catch { }

                            // Delete the temporary image file
                            try
                            {
                                System.IO.File.Delete(tempImgPath);
                            }
                            catch { }
                        }

                        row++;
                    }

                    // Auto-fit columns (except Column G which we styled specifically)
                    dynamic colAtoF = sheet.Range["A1", "F1"].EntireColumn;
                    colAtoF.AutoFit();

                    // Save the workbook
                    workbook.SaveAs(filePath);

                    TextBlockStatus.Text = $"Successfully exported {viewpoints.Count} viewpoints to Excel!";
                }
                catch (Exception ex)
                {
                    TextBlockStatus.Text = "Export failed: " + ex.Message;
                    MessageBox.Show("Excel Export failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetFullMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearSectioning();
                TextBlockStatus.Text = "Reset to Full Mode (Section Cuts Cleared).";
            }
            catch (Exception ex)
            {
                TextBlockStatus.Text = "Error: " + ex.Message;
            }
        }

        private void PopulateModels()
        {
            ComboModel.Items.Clear();
            try
            {
                if (Autodesk.Navisworks.Api.Application.ActiveDocument != null)
                {
                    foreach (Autodesk.Navisworks.Api.Model model in Autodesk.Navisworks.Api.Application.ActiveDocument.Models)
                    {
                        string name = model.RootItem != null ? model.RootItem.DisplayName : System.IO.Path.GetFileNameWithoutExtension(model.SourceFileName);
                        if (!string.IsNullOrEmpty(name))
                        {
                            ComboModel.Items.Add(name);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent catch
            }

            if (ComboModel.Items.Count > 0)
            {
                ComboModel.SelectedIndex = 0;
            }
            else
            {
                ComboModel.Items.Add("No models loaded");
                ComboModel.SelectedIndex = 0;
            }
        }

        private void PopulateLevels(string selectedModelName)
        {
            PanelLevels.Children.Clear();
            if (string.IsNullOrEmpty(selectedModelName) || selectedModelName == "No models loaded")
            {
                return;
            }

            try
            {
                if (Autodesk.Navisworks.Api.Application.ActiveDocument != null)
                {
                    foreach (Autodesk.Navisworks.Api.Model model in Autodesk.Navisworks.Api.Application.ActiveDocument.Models)
                    {
                        string modelName = model.RootItem != null ? model.RootItem.DisplayName : System.IO.Path.GetFileNameWithoutExtension(model.SourceFileName);
                        if (modelName == selectedModelName)
                        {
                            if (model.RootItem != null)
                            {
                                foreach (Autodesk.Navisworks.Api.ModelItem child in model.RootItem.Children)
                                {
                                    string levelName = child.DisplayName;
                                    if (!string.IsNullOrEmpty(levelName))
                                    {
                                        CheckBox cb = new CheckBox();
                                        cb.Content = levelName;
                                        cb.IsChecked = true;
                                        cb.Margin = new Thickness(0, 0, 0, 8);
                                        PanelLevels.Children.Add(cb);
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent catch
            }
        }

        private void ComboModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboModel.SelectedItem != null)
            {
                string selectedModel = ComboModel.SelectedItem.ToString();
                PopulateLevels(selectedModel);
            }
        }
    }
}

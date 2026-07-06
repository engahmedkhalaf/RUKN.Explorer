using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace RUKN.Search.Plugin
{
    public partial class ModelProcessingWindow : Window
    {
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
            TextBlockStatus.Text = "Reading models from source...";
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
            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                string unitText = GetUnitText();
                
                foreach (string levelName in checkedLevels)
                {
                    double? elevation = GetLevelElevation(selectedModel, levelName);
                    if (elevation.HasValue)
                    {
                        double? topZ = null;
                        double? bottomZ = null;

                        if (CheckOffsetTop.IsChecked == true && TryParseDouble(TextOffsetTop.Text, out double offsetTop))
                        {
                            topZ = elevation.Value + ConvertToMeters(offsetTop);
                        }

                        if (CheckOffsetBottom.IsChecked == true && TryParseDouble(TextOffsetBottom.Text, out double offsetBottom))
                        {
                            bottomZ = elevation.Value + ConvertToMeters(offsetBottom);
                        }

                        // Apply the section cut using COM API
                        ApplySectionCut(topZ, bottomZ);

                        // Capture current view state into a new viewpoint
                        Autodesk.Navisworks.Api.Viewpoint vp = doc.CurrentViewpoint.CreateCopy();
                        
                        // Create a SavedViewpoint
                        Autodesk.Navisworks.Api.SavedViewpoint savedVp = new Autodesk.Navisworks.Api.SavedViewpoint(vp);
                        
                        // Determine display name with top/bottom offset details
                        string displayName = $"{selectedModel} - {levelName}";
                        var details = new System.Collections.Generic.List<string>();
                        if (topZ.HasValue && TryParseDouble(TextOffsetTop.Text, out double ot) && ot != 0)
                        {
                            details.Add($"Top Z: {(ot > 0 ? "+" : "")}{ot}{unitText}");
                        }
                        if (bottomZ.HasValue && TryParseDouble(TextOffsetBottom.Text, out double ob) && ob != 0)
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
            }
        }

        private double? GetLevelElevation(string selectedModelName, string levelNameName)
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
                                        var bbox = child.BoundingBox();
                                        if (bbox != null)
                                        {
                                            return bbox.Min.Z;
                                        }
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
                    plane.SetValue(normal, bottomZ.Value);

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

        private void SaveViewpoints_Click(object sender, RoutedEventArgs e)
        {
            TextBlockStatus.Text = "Viewpoints are already saved during generation.";
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            TextBlockStatus.Text = "Exporting data...";
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

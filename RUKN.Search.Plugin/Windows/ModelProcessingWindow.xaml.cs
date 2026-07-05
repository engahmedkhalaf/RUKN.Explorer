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

            // Read Offset
            double offset = 0;
            double.TryParse(TextOffset.Text, out offset);

            // Convert offset to meters based on unit radio selection
            double offsetInMeters = offset;
            string unitText = "m";
            if (RadioMM.IsChecked == true)
            {
                offsetInMeters = offset / 1000.0;
                unitText = "mm";
            }
            else if (RadioCM.IsChecked == true)
            {
                offsetInMeters = offset / 100.0;
                unitText = "cm";
            }
            else if (RadioFT.IsChecked == true)
            {
                offsetInMeters = offset * 0.3048;
                unitText = "ft";
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
                
                foreach (string levelName in checkedLevels)
                {
                    double? elevation = GetLevelElevation(selectedModel, levelName);
                    if (elevation.HasValue)
                    {
                        double cutZ = elevation.Value + offsetInMeters;

                        // Apply the section cut using COM API
                        ApplySectionCut(cutZ);

                        // Capture current view state into a new viewpoint
                        Autodesk.Navisworks.Api.Viewpoint vp = doc.CurrentViewpoint.CreateCopy();
                        
                        // Create a SavedViewpoint
                        Autodesk.Navisworks.Api.SavedViewpoint savedVp = new Autodesk.Navisworks.Api.SavedViewpoint(vp);
                        savedVp.DisplayName = $"{selectedModel} - {levelName}" + (offset != 0 ? $" + {offset}{unitText}" : "");

                        // Save to document
                        doc.SavedViewpoints.AddCopy(savedVp);
                        generatedCount++;
                    }
                }

                // Reset sectioning on the current live view to avoid leaving the screen cut
                ClearSectioning();

                TextBlockStatus.Text = $"Successfully generated {generatedCount} viewpoint(s)!";
            }
            catch (Exception ex)
            {
                TextBlockStatus.Text = "Error: " + ex.Message;
                MessageBox.Show("Viewpoint generation failed: " + ex.Message);
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

        private void ApplySectionCut(double cutZ)
        {
            try
            {
                var state = Autodesk.Navisworks.Api.ComApi.ComApiBridge.State;
                var curView = state.CurrentView;
                var clipColl = (Autodesk.Navisworks.Api.Interop.ComApi.InwClippingPlaneColl2)curView.ClippingPlanes();

                // Make sure we have at least one plane
                if (clipColl.Count == 0)
                {
                    clipColl.CreatePlane(1);
                }

                var plane1 = (Autodesk.Navisworks.Api.Interop.ComApi.InwOaClipPlane)clipColl[1];
                
                // Normal vector pointing down (0, 0, -1) to cut the top off
                var normal = (Autodesk.Navisworks.Api.Interop.ComApi.InwLUnitVec3f)state.ObjectFactory(
                    Autodesk.Navisworks.Api.Interop.ComApi.nwEObjectType.eObjectType_nwLUnitVec3f, null, null);
                normal.SetValue(0, 0, -1);

                var plane = (Autodesk.Navisworks.Api.Interop.ComApi.InwLPlane3f)state.ObjectFactory(
                    Autodesk.Navisworks.Api.Interop.ComApi.nwEObjectType.eObjectType_nwLPlane3f, null, null);
                
                // To cut at height cutZ with normal (0,0,-1), distance is -cutZ
                plane.SetValue(normal, -cutZ);

                plane1.Plane = plane;
                plane1.Enabled = true;
                
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
                var clipColl = (Autodesk.Navisworks.Api.Interop.ComApi.InwClippingPlaneColl2)curView.ClippingPlanes();
                for (int i = 1; i <= clipColl.Count; i++)
                {
                    var plane = (Autodesk.Navisworks.Api.Interop.ComApi.InwOaClipPlane)clipColl[i];
                    plane.Enabled = false;
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

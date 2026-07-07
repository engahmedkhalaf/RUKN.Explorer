using Autodesk.Navisworks.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RUKN.Quant
{
    public partial class MainWindow : Window
    {
        private List<ElementQuantities> _allItems = new List<ElementQuantities>();
        private List<ElementQuantities> _filteredItems = new List<ElementQuantities>();

        public MainWindow()
        {
            InitializeComponent();
            
            // Set keyboard interop so keys work in modeless Navisworks windows
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void BtnExtract_Click(object sender, RoutedEventArgs e)
        {
            GridProgress.Visibility = Visibility.Visible;
            TxtStatus.Text = "Scanning model and extracting properties...";
            TxtTime.Text = "Please wait...";
            
            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Run extraction synchronously on the main thread to prevent COM AccessViolationExceptions in Navisworks API
                _allItems = ExtractQuantitiesFromModel();
                
                watch.Stop();
                
                ApplyFilterAndDisplay();
                
                TxtTotalElements.Text = _allItems.Count.ToString("N0");
                TxtStatus.Text = "Extraction successfully completed!";
                TxtTime.Text = $"Time: {watch.Elapsed.TotalSeconds:F2}s";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during extraction: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Extraction failed.";
                TxtTime.Text = "Error";
            }
            finally
            {
                GridProgress.Visibility = Visibility.Collapsed;
            }
        }

        private List<ElementQuantities> ExtractQuantitiesFromModel()
        {
            var results = new List<ElementQuantities>();
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) return results;

            // Get selected items or fallback to scanning all model items
            var selectedItems = doc.CurrentSelection.SelectedItems;
            IEnumerable<ModelItem> itemsToScan;
            if (selectedItems != null && selectedItems.Count > 0)
            {
                itemsToScan = selectedItems.DescendantsAndSelf;
            }
            else
            {
                var allItems = new List<ModelItem>();
                foreach (Model model in doc.Models)
                {
                    if (model.RootItem != null)
                    {
                        allItems.AddRange(model.RootItem.DescendantsAndSelf);
                    }
                }
                itemsToScan = allItems;
            }

            // Find all unique element instances (parents of geometry leaf nodes)
            var uniqueInstances = new HashSet<ModelItem>();
            foreach (var item in itemsToScan)
            {
                if (item.HasGeometry)
                {
                    // Check if it's a leaf node containing geometry (no children of this item have geometry)
                    bool isLeafGeometry = true;
                    foreach (ModelItem child in item.Children)
                    {
                        if (child.HasGeometry)
                        {
                            isLeafGeometry = false;
                            break;
                        }
                    }

                    if (isLeafGeometry)
                    {
                        // The parent of the geometry leaf is the physical instance element
                        ModelItem instance = item.Parent != null ? item.Parent : item;
                        uniqueInstances.Add(instance);
                    }
                }
            }

            // Now extract quantities from the unique instances
            foreach (var item in uniqueInstances)
            {
                var qItem = new ElementQuantities
                {
                    Name = item.DisplayName ?? item.ClassDisplayName ?? "Element",
                    Category = PropertyHelper.GetPropertyStringDeep(item, "Element", new[] { "Category" }),
                    Family = PropertyHelper.GetPropertyStringDeep(item, "Element", new[] { "Family", "Family Name" }),
                    Type = PropertyHelper.GetPropertyStringDeep(item, "Element", new[] { "Type", "Type Name" }),
                    RevitId = PropertyHelper.GetRevitIdDeep(item),
                    Length = PropertyHelper.GetQuantityDoubleDeep(item, "Element", new[] { "Length", "Length Value", "Height", "Cut Length" }),
                    Area = PropertyHelper.GetQuantityDoubleDeep(item, "Element", new[] { "Area", "Area Value", "Gross Area", "Net Area" }),
                    Volume = PropertyHelper.GetQuantityDoubleDeep(item, "Element", new[] { "Volume", "Volume Value", "Gross Volume", "Net Volume" }),
                    Height = PropertyHelper.GetQuantityDoubleDeep(item, "Element", new[] { "Height", "Unconnected Height" }),
                    Width = PropertyHelper.GetQuantityDoubleDeep(item, "Element", new[] { "Width" }),
                    Thickness = PropertyHelper.GetQuantityDoubleDeep(item, "Element", new[] { "Thickness" }),
                    Count = 1
                };

                // Fallback lookup: Walk up ancestor tree to inherit Category if not set directly
                if (string.IsNullOrEmpty(qItem.Category))
                {
                    foreach (var ancestor in item.Ancestors)
                    {
                        string cat = PropertyHelper.GetPropertyString(ancestor, "Element", new[] { "Category" });
                        if (!string.IsNullOrEmpty(cat))
                        {
                            qItem.Category = cat;
                            break;
                        }
                    }
                }

                // Second fallback: parse parent's display category
                if (string.IsNullOrEmpty(qItem.Category))
                {
                    qItem.Category = item.Parent != null ? (item.Parent.DisplayName ?? item.Parent.ClassDisplayName) : "Other";
                }

                // Normalize naming parameters
                if (string.IsNullOrEmpty(qItem.Family))
                {
                    qItem.Family = item.Parent != null ? item.Parent.DisplayName : "Generic";
                }
                if (string.IsNullOrEmpty(qItem.Type))
                {
                    qItem.Type = item.ClassDisplayName ?? "Generic Type";
                }

                results.Add(qItem);
            }

            return results;
        }

        private void ApplyFilterAndDisplay()
        {
            string query = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                _filteredItems = _allItems.ToList();
            }
            else
            {
                _filteredItems = _allItems.Where(i => 
                    (i.Name != null && i.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.Category != null && i.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.Family != null && i.Family.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.Type != null && i.Type.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.RevitId != null && i.RevitId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }

            // Bind Elements DataGrid
            DataGridElements.ItemsSource = _filteredItems;

            // Generate Summary Groupings
            var summaryData = _filteredItems
                .GroupBy(i => i.Category ?? "Other")
                .Select(g => new ElementQuantities
                {
                    Category = g.Key,
                    Count = g.Sum(x => x.Count),
                    Length = g.Sum(x => x.Length ?? 0),
                    Area = g.Sum(x => x.Area ?? 0),
                    Volume = g.Sum(x => x.Volume ?? 0)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            DataGridSummary.ItemsSource = summaryData;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredItems == null || _filteredItems.Count == 0)
            {
                MessageBox.Show("No quantity data available to export. Please extract quantities first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel CSV File (*.csv)|*.csv",
                FileName = "RUKN_Quantities_BOQ.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelExporter.Export(_filteredItems, saveFileDialog.FileName);
                    MessageBox.Show("Quantities exported successfully to Excel format!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            BtnExtract_Click(sender, e);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterAndDisplay();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            // Modeless text interop focus
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            // Focus release
        }
    }
}

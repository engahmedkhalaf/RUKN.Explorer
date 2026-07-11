using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RUKN.Quant
{
    public static class ExcelExporter
    {
        public static void Export(List<ElementQuantities> items, string filePath)
        {
            try
            {
                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    throw new Exception("Microsoft Excel is not installed on this system.");
                }

                dynamic excel = Activator.CreateInstance(excelType);
                excel.Visible = true; // Make Excel visible to the user
                dynamic workbooks = excel.Workbooks;
                dynamic workbook = workbooks.Add();
                
                // --- Sheet 1: Summary ---
                dynamic sheetSummary = workbook.ActiveSheet;
                sheetSummary.Name = "Category Summary";

                // Headers
                sheetSummary.Cells[1, 1] = "Category";
                sheetSummary.Cells[1, 2] = "Element Count";
                sheetSummary.Cells[1, 3] = "Total Length (m)";
                sheetSummary.Cells[1, 4] = "Total Area (m²)";
                sheetSummary.Cells[1, 5] = "Total Volume (m³)";

                // Style Headers
                dynamic headerRangeSummary = sheetSummary.Range["A1", "E1"];
                headerRangeSummary.Font.Bold = true;
                headerRangeSummary.Interior.Color = 14277081; // Light Gray (#D9D9D9)

                var summary = items
                    .GroupBy(i => i.Category ?? "Other")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Sum(x => x.Count),
                        Length = g.Sum(x => x.Length ?? 0),
                        Area = g.Sum(x => x.Area ?? 0),
                        Volume = g.Sum(x => x.Volume ?? 0)
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();

                int row = 2;
                foreach (var s in summary)
                {
                    sheetSummary.Cells[row, 1] = s.Category;
                    sheetSummary.Cells[row, 2] = s.Count;
                    sheetSummary.Cells[row, 3] = s.Length;
                    sheetSummary.Cells[row, 4] = s.Area;
                    sheetSummary.Cells[row, 5] = s.Volume;
                    row++;
                }
                sheetSummary.Columns.AutoFit();

                // --- Sheet 2: Elements Detail ---
                dynamic sheetDetail = workbook.Sheets.Add(Type.Missing, sheetSummary);
                sheetDetail.Name = "Element Details";

                // Headers
                sheetDetail.Cells[1, 1] = "Name";
                sheetDetail.Cells[1, 2] = "Category";
                sheetDetail.Cells[1, 3] = "Family";
                sheetDetail.Cells[1, 4] = "Type";
                sheetDetail.Cells[1, 5] = "Revit ID";
                sheetDetail.Cells[1, 6] = "Length (m)";
                sheetDetail.Cells[1, 7] = "Area (m²)";
                sheetDetail.Cells[1, 8] = "Volume (m³)";

                // Style Headers
                dynamic headerRangeDetail = sheetDetail.Range["A1", "H1"];
                headerRangeDetail.Font.Bold = true;
                headerRangeDetail.Interior.Color = 14277081; // Light Gray (#D9D9D9)

                row = 2;
                foreach (var i in items)
                {
                    sheetDetail.Cells[row, 1] = i.Name ?? "";
                    sheetDetail.Cells[row, 2] = i.Category ?? "";
                    sheetDetail.Cells[row, 3] = i.Family ?? "";
                    sheetDetail.Cells[row, 4] = i.Type ?? "";
                    sheetDetail.Cells[row, 5] = i.RevitId ?? "";
                    
                    if (i.Length.HasValue) sheetDetail.Cells[row, 6] = i.Length.Value;
                    if (i.Area.HasValue) sheetDetail.Cells[row, 7] = i.Area.Value;
                    if (i.Volume.HasValue) sheetDetail.Cells[row, 8] = i.Volume.Value;
                    row++;
                }
                sheetDetail.Columns.AutoFit();

                // Save Workbook
                workbook.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RUKN.Quant
{
    public static class ExcelExporter
    {
        public static void Export(List<QuantityItem> items, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Instruct Excel to use comma separator dynamically
                writer.WriteLine("sep=,");

                // --- SUMMARY HEADER ---
                writer.WriteLine("RUKN QUANT - CATEGORY SUMMARY REPORT");
                writer.WriteLine("Category,Element Count,Total Length (m),Total Area (m²),Total Volume (m³)");

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
                    .OrderByDescending(s => s.Count);

                foreach (var s in summary)
                {
                    writer.WriteLine($"\"{Escape(s.Category)}\",{s.Count},{s.Length:F2},{s.Area:F2},{s.Volume:F2}");
                }

                writer.WriteLine();
                writer.WriteLine();

                // --- DETAIL HEADER ---
                writer.WriteLine("RUKN QUANT - ELEMENT DETAIL DATA");
                writer.WriteLine("Name,Category,Family,Type,Revit ID,Length (m),Area (m²),Volume (m³),Height (m),Width (m),Thickness (m)");

                foreach (var i in items)
                {
                    writer.WriteLine($"\"{Escape(i.Name)}\",\"{Escape(i.Category)}\",\"{Escape(i.Family)}\",\"{Escape(i.Type)}\",\"{Escape(i.RevitId)}\",{(i.Length.HasValue ? i.Length.Value.ToString("F3") : "")},{(i.Area.HasValue ? i.Area.Value.ToString("F3") : "")},{(i.Volume.HasValue ? i.Volume.Value.ToString("F3") : "")},{(i.Height.HasValue ? i.Height.Value.ToString("F3") : "")},{(i.Width.HasValue ? i.Width.Value.ToString("F3") : "")},{(i.Thickness.HasValue ? i.Thickness.Value.ToString("F3") : "")}");
                }
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\"", "\"\"");
        }
    }
}

namespace RUKN.Quant
{
    public class ElementQuantities
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Family { get; set; }
        public string Type { get; set; }
        public string RevitId { get; set; }
        public double? Area { get; set; }
        public double? Volume { get; set; }
        public double? Length { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public double? Thickness { get; set; }
        public int Count { get; set; } = 1;
    }
}

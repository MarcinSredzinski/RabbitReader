namespace Core.Library.Models
{
    public class BmpMeasurementDto
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime DateTime { get; set; }
        public int Pressure { get; set; }
        public int Temperature { get; set; }
    }
}
namespace Mappy.Models
{
    public class OptionalStringAttribute
    {
        public bool Enabled { get; set; }

        public string Value { get; set; } = string.Empty;
    }
}
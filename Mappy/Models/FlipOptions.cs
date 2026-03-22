namespace Mappy.Models
{
    using Mappy.Models.Enums;

    public class FlipOptions
    {
        public FlipDirection Direction { get; set; } = FlipDirection.Horizontal;

        public bool ApplyShadows { get; set; } = true;

        public bool Cancelled { get; set; } = false;
    }
}
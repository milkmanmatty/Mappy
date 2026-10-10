namespace Mappy.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Models;

    public class SetUseOnlyUnitsOperation : IReplayableOperation
    {
        private readonly IMapModel map;

        private readonly IList<string> newNames;

        private readonly string newFileName;

        private List<string> oldNames;

        private string oldFileName;

        public SetUseOnlyUnitsOperation(IMapModel map, IList<string> names, string fileName)
        {
            this.map = map;
            this.newNames = names ?? new List<string>();
            this.newFileName = fileName ?? string.Empty;
        }

        public void Execute()
        {
            this.oldNames = this.map.UseOnlyUnitNames.ToList();
            this.oldFileName = this.map.Attributes.UseOnlyUnits;
            this.map.ReplaceUseOnlyUnitNames(this.newNames);
            this.map.Attributes.UseOnlyUnits = this.newFileName;
        }

        public void Undo()
        {
            this.map.ReplaceUseOnlyUnitNames(this.oldNames);
            this.map.Attributes.UseOnlyUnits = this.oldFileName ?? string.Empty;
        }
    }
}

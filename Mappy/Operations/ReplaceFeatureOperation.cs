namespace Mappy.Operations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Models;

    public class ReplaceFeatureOperation : IReplayableOperation
    {
        private readonly IMapModel map;

        private readonly IList<Guid> ids;

        private readonly string destinationFeatureName;

        private IList<FeatureInstance> previousFeatures;

        public ReplaceFeatureOperation(IMapModel map, IEnumerable<Guid> ids, string destinationFeatureName)
        {
            this.map = map;
            this.ids = ids.ToList();
            this.destinationFeatureName = destinationFeatureName;
        }

        public void Execute()
        {
            this.previousFeatures = new List<FeatureInstance>();
            foreach (var id in this.ids)
            {
                var current = this.map.GetFeatureInstance(id);
                this.previousFeatures.Add(current);
                this.map.UpdateFeatureInstance(new FeatureInstance(current.Id, this.destinationFeatureName, current.Location));
            }
        }

        public void Undo()
        {
            foreach (var previous in this.previousFeatures)
            {
                this.map.UpdateFeatureInstance(previous);
            }
        }
    }
}

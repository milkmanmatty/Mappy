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

        private readonly bool remove;

        private IList<FeatureInstance> previousFeatures;

        public ReplaceFeatureOperation(IMapModel map, IEnumerable<Guid> ids, string destinationFeatureName)
        {
            this.map = map;
            this.ids = ids.ToList();
            this.destinationFeatureName = destinationFeatureName;
            this.remove = string.IsNullOrWhiteSpace(destinationFeatureName);
        }

        public void Execute()
        {
            this.previousFeatures = new List<FeatureInstance>();
            foreach (var id in this.ids)
            {
                var current = this.map.GetFeatureInstance(id);
                this.previousFeatures.Add(current);

                if (this.remove)
                {
                    this.map.RemoveFeatureInstance(id);
                }
                else
                {
                    this.map.UpdateFeatureInstance(new FeatureInstance(current.Id, this.destinationFeatureName, current.Location));
                }
            }
        }

        public void Undo()
        {
            if (this.remove)
            {
                foreach (var previous in this.previousFeatures)
                {
                    this.map.AddFeatureInstance(previous);
                }
            }
            else
            {
                foreach (var previous in this.previousFeatures)
                {
                    this.map.UpdateFeatureInstance(previous);
                }
            }
        }
    }
}

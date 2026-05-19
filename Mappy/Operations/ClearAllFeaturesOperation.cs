namespace Mappy.Operations
{
    using System.Collections.Generic;

    using Mappy.Models;

    public class ClearAllFeaturesOperation : IReplayableOperation
    {
        private readonly IMapModel map;

        private List<FeatureInstance> removedFeatures;

        public ClearAllFeaturesOperation(IMapModel map)
        {
            this.map = map;
        }

        public void Execute()
        {
            this.removedFeatures = new List<FeatureInstance>(this.map.EnumerateFeatureInstances());
            foreach (var feature in this.removedFeatures)
            {
                this.map.RemoveFeatureInstance(feature.Id);
            }
        }

        public void Undo()
        {
            foreach (var feature in this.removedFeatures)
            {
                this.map.AddFeatureInstance(feature);
            }
        }
    }
}

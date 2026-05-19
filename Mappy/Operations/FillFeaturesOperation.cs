namespace Mappy.Operations
{
    using System.Collections.Generic;

    using Mappy.Models;

    public class FillFeaturesOperation : IReplayableOperation
    {
        private readonly IMapModel map;

        private readonly IReadOnlyList<FeatureInstance> instances;

        public FillFeaturesOperation(IMapModel map, IReadOnlyList<FeatureInstance> instances)
        {
            this.map = map;
            this.instances = instances;
        }

        public void Execute()
        {
            foreach (var instance in this.instances)
            {
                this.map.AddFeatureInstance(instance);
            }
        }

        public void Undo()
        {
            foreach (var instance in this.instances)
            {
                this.map.RemoveFeatureInstance(instance.Id);
            }
        }
    }
}
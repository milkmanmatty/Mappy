namespace Mappy.Models
{
    using System.Collections.Generic;

    using Mappy.Data;
    using Mappy.IO;

    public class SectionFeatureLoadResult
    {
        public SectionFeatureLoadResult(
            IList<SectionInfo> sections,
            IList<FeatureInfo> features,
            IList<string> unitNames,
            List<HpiErrorInfo> errors,
            List<HpiInnerFileErrorInfo> fileErrors)
        {
            this.Sections = sections;
            this.Features = features;
            this.UnitNames = unitNames;
            this.Errors = errors;
            this.FileErrors = fileErrors;
        }

        public IList<SectionInfo> Sections { get; }

        public IList<FeatureInfo> Features { get; }

        public IList<string> UnitNames { get; }

        public List<HpiErrorInfo> Errors { get; }

        public List<HpiInnerFileErrorInfo> FileErrors { get; }
    }
}

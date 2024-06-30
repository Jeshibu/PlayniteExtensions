using System;
using System.Collections.Generic;

namespace PlayniteExtensions.Metadata.Common
{
    public class BulkImportPluginSettings : ObservableObject
    {
        public int MaxDegreeOfParallelism { get; set; } = GetDefaultMaxDegreeOfParallelism();

        public int Version { get; set; } = 0;

        public static int GetDefaultMaxDegreeOfParallelism()
        {
            var processorCount = Environment.ProcessorCount;
            var parallelism = (int)Math.Round(processorCount * .75D, MidpointRounding.AwayFromZero);

            if (parallelism == processorCount)
                parallelism--;

            if (parallelism < 1)
                parallelism = 1;

            return parallelism;
        }
    }
}

using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;

namespace Barnite
{
    public class BarcodeResultsGridViewModel
    {
        public List<BarcodeResultEntry> ResultEntries { get; set; }
        public RelayCommand RetryFailedCommand { get; set; }
        public bool CanRetryFailed => ResultEntries.Any(entry => !entry.IsSuccessful);
    }
}

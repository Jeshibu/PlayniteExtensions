using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barnite
{
    public class BarcodeEntry
    {
        public string Barcode { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }

        public bool IsSuccessful => !string.IsNullOrEmpty(Title) && Title != "Not Found";
    }
}

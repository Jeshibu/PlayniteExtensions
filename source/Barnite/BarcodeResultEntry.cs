using System;

namespace Barnite;

public class BarcodeResultEntry
{
    public string Barcode { get; set; }
    public string Title { get; set; }
    public string Source { get; set; }
    public Guid Guid { get; set; }

    public bool IsSuccessful { get; set; }
}

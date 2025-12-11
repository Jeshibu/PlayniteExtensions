namespace BigFishMetadata.Models;

public class ReviewJsonData
{
    public Advreview advreview { get; set; }
}

public class Advreview
{
    public int totalRecords { get; set; }
    public int ratingSummary { get; set; }
    public double ratingSummaryValue { get; set; }
    public int recomendedPercent { get; set; }
    public int totalRecordsFiltered { get; set; }
}

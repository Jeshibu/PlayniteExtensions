namespace BigFishMetadata.Models;

public class ReviewJsonData
{
    public AdvReview advreview { get; set; }
}

public class AdvReview
{
    public int totalRecords { get; set; }
    public int ratingSummary { get; set; }
    public double ratingSummaryValue { get; set; }
    public int recomendedPercent { get; set; }
    public int totalRecordsFiltered { get; set; }
}

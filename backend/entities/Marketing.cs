using System;

public class PaidAdsPerformance : IModel
{
    public string id {get; set;}
    public Campaign campaign {get; set;}
    public AdGroup adGroup {get; set;}
    public int impressions {get; set;} 
    public int clicks {get; set;}
    public int conversions {get; set;}
    public decimal ctr {get; set;}
    public decimal averageCpc {get; set;}
    public DateTime date {get; set;}
}

public class Campaign
{
    public string id {get; set;}
    public string name {get; set;}
}

public class AdGroup
{
    public string id {get; set;}
    public string name {get; set;}
}
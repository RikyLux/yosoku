using System;
using System.Collections.Generic;

public class Project : IModel
{
    public string id {get; set;}
    public string name {get; set;}
    public string version { get; set; }
    public string companyId { get; set; }
    public List<DataSource> dataSources {get; set;}
    public TimeSettings timeSettings {get; set;}
    public Dictionary<string, object> metadata { get; set; }
    public string _etag {get; set;}
}

public class TimeSettings
{
    public string granularity {get; set;}
    public DateTime from {get; set;}
    public DateTime? to {get; set;}
}

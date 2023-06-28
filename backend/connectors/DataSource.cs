using System;
using System.Collections.Generic;

public class DataSource
{
    public string id {get; set;}
    public string name {get; set;}
    public string source {get; set;}
    public DateTime lastRefresh { get; set; }
    public Dictionary<string, string> metadata {get; set;}
    public bool isOnline { get; set; }
}
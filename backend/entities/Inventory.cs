using System;

public class InventoryLevel : IModel
{
    public string id {get; set;}
    public decimal available {get; set;}
    public InventoryItem inventoryItem {get; set;}
    public Location location {get; set;}
    public DateTime updatedAt {get; set;}
}

public class InventoryItem
{
    public string id {get; set;}
    public string sku {get; set;}
    public decimal cost {get; set;}
}

public class Location
{
    public string id {get; set;}
    public string name {get; set;}
    public string country {get; set;}
    public string city {get; set;}
    public string address {get; set;}
}
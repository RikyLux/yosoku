using System;

public class InventoryItem
{
    public string id {get; set;}
    public string sku {get; set;}
    public decimal cost {get; set;}
}

public class InventoryLevel
{
    public decimal available {get; set;}
    public string inventoryItemId {get; set;}
    public string locationId {get; set;}
    public DateTime updatedAt {get; set;}
}

public class Location
{
    public string id {get; set;}
    public string name {get; set;}
    public string country {get; set;}
    public string city {get; set;}
    public string address {get; set;}
}
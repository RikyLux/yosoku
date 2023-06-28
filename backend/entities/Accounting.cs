using System;

public class Transaction : IModel
{
    public string id {get; set;}
    public decimal amount {get; set;}
    public string category {get; set;}
    public string account {get; set;}
    public DateTime createdAt {get; set;}
}
using System;
using System.Collections.Generic;

public class Order
{
    public string id {get; set;}
    public string number {get; set;}
    public string currency {get; set;}
    public List<OrderDiscount> discounts {get; set;}
    public decimal subTotal {get; set;}
    public decimal shippingCost {get; set;}
    public decimal totalDiscount {get; set;}
    public decimal totalTax {get; set;}
    public decimal totalAmount {get; set;}
    public string financialStatus {get; set;}
    public string fulfillmentStatus {get; set;}
    public string referringSite {get; set;}
    public List<OrderLine> lineItems {get; set;}
    public OrderCustomer customer {get; set;}
    public OrderLocation location {get; set;}
    public DateTime createdAt {get; set;}
    public DateTime? updatedAt {get; set;}
}

public class OrderDiscount
{
    public decimal amount {get; set;}
}

public class OrderLine
{
    public string id {get; set;}
    public OrderProduct product {get; set;}
    public OrderVariant variant {get; set;}
    public decimal quantity {get; set;}
    public decimal totalAmount {get; set;}
}

public class OrderProduct
{
    public string id {get; set;}
    public string name {get; set;}
}

public class OrderVariant
{
    public string id {get; set;}
    public string name {get; set;}
    public string sku {get; set;}
    public string inventoryItemId {get; set;}
}

public class OrderCustomer
{
    public string id {get; set;}
    public string country {get; set;}
    public string city {get; set;}
}

public class OrderLocation
{
    public string id {get; set;}
    public string name {get; set;}
}
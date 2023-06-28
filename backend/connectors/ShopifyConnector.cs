using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Il connector a Shopify funziona in modo diverso dagli altri.
// Il processo di integrazione viene avviato dall'utente su Shopify e non dalla nostra app:
// 1. l'utente fa partire il processo di "installazione" della nostra app sul suo shopify store
// 2. BuildShopUrl(): viene fatto un redirect
// 3. l'utente accetta la connessione e gli scope che richiediamo
// 4. ExchangeToken(): scambiamo il token per l'access token e facciamo un redirect alla nostra app
// 5. l'app prende l'access token e chiama CreateDataSource() rientrando nel flow normale di creazione dei datasource (il token non scade mai, quindi il refresh non deve far niente)
//
// WARNING: le api di shopify possono essere usati o in dev o in produzione, non entrambi dato che devo impostare l'url su cui l'uente viene
// indirizzato quando vuole installare l'app direttamente su shopify e può essere solo uno (o localhost o in produzione) 
// per riferimenti futuri: https://medium.com/ballerina-techblog/authenticate-a-shopify-app-using-oauth-the-ballerina-way-f827ab99f576
public class ShopifyConnector : BaseCosmos, IConnector
{
    private static Regex _regexNextLink = new Regex(@"<(https://[^>]*)>\s*;\s*rel=""next""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public DataSource dataSource { get; set; }
    string clientId = "a72f51729f26b9ce2ff7a259be2fd64d";
    string clientSecret = "42e6e4a7e1ac84cb393ace0750891458";
    string scopes = "read_all_orders,read_assigned_fulfillment_orders,read_checkouts,read_customers,read_discounts,read_fulfillments,read_inventory,read_locations,read_marketing_events,read_orders,read_products,read_product_listings,read_reports,read_shipping,read_returns";

    string authorizeApiPath = "/admin/oauth/authorize";
    string redirectGetToken = Helpers.IsDev() 
        ? "http://localhost:7071/api/ShopifyGetToken" 
        : "https://sturppy-plus-functions.azurewebsites.net/api/ShopifyGetToken?";

    public ShopifyConnector() {}
    public ShopifyConnector(DataSource dataSource) 
    {
        this.dataSource = dataSource;
    }

    public async Task<DataSource> CreateDataSource(Dictionary<string, string> metadata)
    {
        return new DataSource()
        {
            id = dataSource.id,
            metadata = metadata,
            name = dataSource.name,
            source = dataSource.source,
            isOnline = true,
            lastRefresh = TimeHelper.OneYearAgoStartMonth()
        };
    }

    public Task<PrepareDatasourceResult> PrepareDataSource()
    {
        throw new System.NotImplementedException();
    }

    public async Task<List<object>> ReadData(DataPipeline pipeline)
    {
        string shop = dataSource.metadata["shop"];
        string accessToken = dataSource.metadata["token"];
        string from = pipeline.fromDate.ToString("o");
        string to = pipeline.toDate != null ? pipeline.toDate.Value.AddDays(1).ToString("o") : null;

        string url = string.Format("https://{0}/admin/api/2023-04/orders.json?status=any&created_at_min={1}", shop, from);
        if(!string.IsNullOrEmpty(to))
            url += "&created_at_max=" + to;
        url += "&limit=250";

        var allOrders = await this.GetOrders(url, accessToken);
        var orders = allOrders.Where(x => x.financial_status == "paid" || x.financial_status == "partially_paid");
        var ordersByDate = orders.GroupBy(x => Config.UTCDate(x.created_at));

        //TODO: prendi prodotti, varianti, inventory item e associali ai dati dell'ordine

        List<Order> myOrders = new List<Order>();
        foreach (var order in orders)
        {
            var myOrder = new Order()
            {
                id = order.id.ToString(),
                number = order.order_number.ToString(),
                currency = order.currency,
                createdAt = Config.UTCDate(order.created_at),
                updatedAt = Config.UTCDate(order.updated_at),
                customer = new OrderCustomer()
                {
                    id = order.customer?.id.ToString(),
                    city = order.billing_address?.city,
                    country = order.billing_address?.country
                },
                discounts = order.discount_codes?.Select(x => new OrderDiscount()
                {
                    amount = Helpers.ToNumber(x.amount)
                }).ToList(),
                location = new OrderLocation() { id = order.location_id?.ToString() },
                shippingCost = 0,
                subTotal = Helpers.ToNumber(order.current_subtotal_price),
                totalAmount = Helpers.ToNumber(order.current_total_price),
                totalDiscount = Helpers.ToNumber(order.current_total_discounts),
                totalTax = Helpers.ToNumber(order.current_total_tax),
                financialStatus = order.financial_status,
                fulfillmentStatus = order.fulfillment_status,
                referringSite = this.ExtractSite(order.referring_site),
                lineItems = order.line_items.Select(x => new OrderLine()
                {
                    id = x.id.ToString(),
                    product = new OrderProduct() { id = x.product_id.ToString() },
                    variant = new OrderVariant() { id = x.variant_id.ToString(), sku = x.sku },
                    quantity = x.quantity,
                    totalAmount = Helpers.ToNumber(x.price)
                }).ToList(),
            };
            myOrders.Add(myOrder);
        }

        //TODO: import inventory data

        return myOrders.Cast<object>().ToList();
    }

    public async Task<List<object>> ImportData(DataPipeline pipeline, List<object> newData)
    {
        return newData;
    }

    public async Task<DataSource> RefreshTokens()
    {
        return dataSource;
    }

    public string BuildShopUrl(string shop)
    {
        string encodedRedirect = Uri.EscapeDataString(redirectGetToken);
        string queryParameters = "?client_id=" + clientId + "&scope=" + scopes + "&redirect_uri=" + encodedRedirect + "&state=plus" + "&grant_options[]=offline_access";
        string url = "https://" + shop + authorizeApiPath + queryParameters;
        return url;
    }

    public async Task<string> ExchangeToken(string shop, string code)
    {
        var request = new { client_id = clientId, client_secret = clientSecret, code = code };
        string url = "https://" + shop + "/admin/oauth/access_token";
        var res = await WebApi.PostWithResult<AuthorizeResponse, object>(url, request);
        
        string redirectUrl = Helpers.IsDev() ? 
            "http://localhost:3000/shopify-completed" : 
            "https://plus.sturppy.com/shopify-completed";
        redirectUrl += "?shop=" + shop + "&token=" + res.access_token + "&scope=" + res.scope;
        return redirectUrl;
    }

    public class AuthorizeResponse
    {
        public string access_token {get; set;}
        public string scope {get; set;}
    }

    private async Task<List<ShopifyOrder>> GetOrders(string link, string accessToken)
    {
        var response = await WebApi.GET<OrdersResponse>(link, new Dictionary<string, string>()
        {
            { "X-Shopify-Access-Token", accessToken }
        });
        var result = response.data.orders;

        if(response.statusCode == HttpStatusCode.OK)
        {
            if(response.headers.Contains("Link"))
            {
                var match = _regexNextLink.Match(response.headers.GetValues("Link").FirstOrDefault());

                if (match.Success || match.Groups.Count >= 2 || match.Groups[1].Success)
                {
                    string matchedUrl = match.Groups[1].Value;
                    var nextPageOrders = await this.GetOrders(matchedUrl, accessToken);
                    result.AddRange(nextPageOrders);
                }
                
                return result;
            }
            else
            {
                return result;
            }
        }
        else return new List<ShopifyOrder>();
    }

    private string ExtractSite(string referrer)
    {
        if (!string.IsNullOrEmpty(referrer))
        {
            try
            {
                Uri siteUri = new Uri(referrer);
                return siteUri.Host.Replace("www.", "");
            }
            catch
            {
                return referrer;
            }
        }
        return referrer;
    }

    #region GDPR

    public async Task<object> ShopifyCustomerDataRequest(string shopDomain, string customerEmail)
    {
        // We literally do nothing with the customer data, so just return true
        return new object {};
    }

    public async Task<bool> ShopifyCustomerDataErasure(string shopDomain, string customerEmail)
    {
        // We literally do nothing with the customer data, so just return true
        return true;
    }

    public async Task<bool> ShopifyShopDataErasure(string shopDomain)
    {
        var res = (await ExecuteQuery<ProjectResult>("SELECT c as project FROM c JOIN a IN c.dataSources where a.metadata.shop = @shop", projectsContainer, new Dictionary<string, object>()
        {
            { "@shop", shopDomain }
        })).FirstOrDefault();

        if(res != null && res.project != null)
        {
            var project = res.project;
            project.dataSources = project.dataSources.Where(x => {
                if(x.metadata == null || !x.metadata.ContainsKey("shop") || x.metadata["shop"] != shopDomain) return true;
                else return false;
            }).ToList();
            await UpdateItem(project, projectsContainer, true);
            return true;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region API classes
    public class OrdersResponse
    {
        public List<ShopifyOrder> orders { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Addresses
    {
    }

    public class AmountSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class AttributedStaff
    {
        public string id { get; set; }
        public int quantity { get; set; }
    }

    public class BillingAddress
    {
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public object company { get; set; }
        public string country { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string phone { get; set; }
        public string province { get; set; }
        public string zip { get; set; }
        public string name { get; set; }
        public string province_code { get; set; }
        public string country_code { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class ClientDetails
    {
        public string accept_language { get; set; }
        public int browser_height { get; set; }
        public string browser_ip { get; set; }
        public int browser_width { get; set; }
        public string session_hash { get; set; }
        public string user_agent { get; set; }
    }

    public class Company
    {
        public int id { get; set; }
        public int location_id { get; set; }
    }

    public class CurrentSubtotalPriceSet
    {
        public CurrentSubtotalPriceSet current_subtotal_price_set { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class CurrentTotalAdditionalFeesSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class CurrentTotalDiscountsSet
    {
        public CurrentTotalDiscountsSet current_total_discounts_set { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class CurrentTotalDutiesSet
    {
        public CurrentTotalDutiesSet current_total_duties_set { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class CurrentTotalPriceSet
    {
        public CurrentTotalPriceSet current_total_price_set { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class CurrentTotalTaxSet
    {
        public CurrentTotalTaxSet current_total_tax_set { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class Customer
    {
        public int id { get; set; }
        public string email { get; set; }
        public bool accepts_marketing { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string state { get; set; }
        public object note { get; set; }
        public bool verified_email { get; set; }
        public object multipass_identifier { get; set; }
        public bool tax_exempt { get; set; }
        public TaxExemptions tax_exemptions { get; set; }
        public string phone { get; set; }
        public string tags { get; set; }
        public string currency { get; set; }
        public Addresses addresses { get; set; }
        public string admin_graphql_api_id { get; set; }
        public DefaultAddress default_address { get; set; }
    }

    public class DefaultAddress
    {
    }

    public class DiscountAllocation
    {
        public string amount { get; set; }
        public int discount_application_index { get; set; }
        public AmountSet amount_set { get; set; }
    }

    public class DiscountApplication
    {
        public string type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string value { get; set; }
        public string value_type { get; set; }
        public string allocation_method { get; set; }
        public string target_selection { get; set; }
        public string target_type { get; set; }
        public string code { get; set; }
        public List<DiscountApplication> discount_applications { get; set; }
    }

    public class DiscountCode
    {
        public string code { get; set; }
        public string amount { get; set; }
        public string type { get; set; }
    }

    public class DiscountedPriceSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class Duty
    {
        public string id { get; set; }
        public string harmonized_system_code { get; set; }
        public string country_code_of_origin { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
        public List<TaxLine> tax_lines { get; set; }
        public string admin_graphql_api_id { get; set; }
    }

    public class Fulfillment
    {
        public DateTime created_at { get; set; }
        public int id { get; set; }
        public int order_id { get; set; }
        public string status { get; set; }
        public string tracking_company { get; set; }
        public string tracking_number { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class LineItem
    {
        public List<AttributedStaff> attributed_staffs { get; set; }
        public int fulfillable_quantity { get; set; }
        public string fulfillment_service { get; set; }
        public string fulfillment_status { get; set; }
        public int grams { get; set; }
        public int id { get; set; }
        public string price { get; set; }
        public int product_id { get; set; }
        public int quantity { get; set; }
        public bool requires_shipping { get; set; }
        public string sku { get; set; }
        public string title { get; set; }
        public int variant_id { get; set; }
        public string variant_title { get; set; }
        public string vendor { get; set; }
        public string name { get; set; }
        public bool gift_card { get; set; }
        public PriceSet price_set { get; set; }
        public List<Property> properties { get; set; }
        public bool taxable { get; set; }
        public List<TaxLine> tax_lines { get; set; }
        public string total_discount { get; set; }
        public TotalDiscountSet total_discount_set { get; set; }
        public List<DiscountAllocation> discount_allocations { get; set; }
        public OriginLocation origin_location { get; set; }
        public List<Duty> duties { get; set; }
    }

    public class NoteAttribute
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class OrderStatusUrl
    {
        public string order_status_url { get; set; }
    }

    public class OriginalTotalAdditionalFeesSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class OriginalTotalDutiesSet
    {
        public OriginalTotalDutiesSet original_total_duties_set { get; set; }
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class OriginLocation
    {
        public long id { get; set; }
        public string country_code { get; set; }
        public string province_code { get; set; }
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
    }

    public class PaymentDetails
    {
        public string avs_result_code { get; set; }
        public string credit_card_bin { get; set; }
        public string cvv_result_code { get; set; }
        public string credit_card_number { get; set; }
        public string credit_card_company { get; set; }
    }

    public class PaymentSchedule
    {
        public int amount { get; set; }
        public string currency { get; set; }
        public DateTime issued_at { get; set; }
        public DateTime due_at { get; set; }
        public string completed_at { get; set; }
        public string expected_payment_method { get; set; }
    }

    public class PaymentTerms
    {
        public int amount { get; set; }
        public string currency { get; set; }
        public string payment_terms_name { get; set; }
        public string payment_terms_type { get; set; }
        public int due_in_days { get; set; }
        public List<PaymentSchedule> payment_schedules { get; set; }
    }

    public class PresentmentMoney
    {
        public string amount { get; set; }
        public string currency_code { get; set; }
    }

    public class PriceSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class Property
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Refund
    {
        public long id { get; set; }
        public long order_id { get; set; }
        public DateTime created_at { get; set; }
        public object note { get; set; }
        public object user_id { get; set; }
        public DateTime processed_at { get; set; }
        public List<object> refund_line_items { get; set; }
        public List<object> transactions { get; set; }
        public List<object> order_adjustments { get; set; }
    }

    public class ShopifyOrder
    {
        public int app_id { get; set; }
        public BillingAddress billing_address { get; set; }
        public string browser_ip { get; set; }
        public bool buyer_accepts_marketing { get; set; }
        public string cancel_reason { get; set; }
        public object cancelled_at { get; set; }
        public string cart_token { get; set; }
        public string checkout_token { get; set; }
        public ClientDetails client_details { get; set; }
        public DateTime closed_at { get; set; }
        public Company company { get; set; }
        public DateTime created_at { get; set; }
        public string currency { get; set; }
        public CurrentTotalAdditionalFeesSet current_total_additional_fees_set { get; set; }
        public string current_total_discounts { get; set; }
        public CurrentTotalDiscountsSet current_total_discounts_set { get; set; }
        public CurrentTotalDutiesSet current_total_duties_set { get; set; }
        public string current_total_price { get; set; }
        public CurrentTotalPriceSet current_total_price_set { get; set; }
        public string current_subtotal_price { get; set; }
        public CurrentSubtotalPriceSet current_subtotal_price_set { get; set; }
        public string current_total_tax { get; set; }
        public CurrentTotalTaxSet current_total_tax_set { get; set; }
        public Customer customer { get; set; }
        public string customer_locale { get; set; }
        public object discount_applications { get; set; }
        public List<DiscountCode> discount_codes { get; set; }
        public string email { get; set; }
        public bool estimated_taxes { get; set; }
        public string financial_status { get; set; }
        public List<Fulfillment> fulfillments { get; set; }
        public string fulfillment_status { get; set; }
        public string gateway { get; set; }
        public int id { get; set; }
        public string landing_site { get; set; }
        public List<LineItem> line_items { get; set; }
        public int? location_id { get; set; }
        public int merchant_of_record_app_id { get; set; }
        public string name { get; set; }
        public string note { get; set; }
        public List<NoteAttribute> note_attributes { get; set; }
        public int number { get; set; }
        public int order_number { get; set; }
        public OriginalTotalAdditionalFeesSet original_total_additional_fees_set { get; set; }
        public OriginalTotalDutiesSet original_total_duties_set { get; set; }
        public PaymentDetails payment_details { get; set; }
        public PaymentTerms payment_terms { get; set; }
        public List<string> payment_gateway_names { get; set; }
        public string phone { get; set; }
        public string presentment_currency { get; set; }
        public DateTime processed_at { get; set; }
        public string processing_method { get; set; }
        public string referring_site { get; set; }
        public List<Refund> refunds { get; set; }
        public ShippingAddress shipping_address { get; set; }
        public List<ShippingLine> shipping_lines { get; set; }
        public string source_name { get; set; }
        public string source_identifier { get; set; }
        public string source_url { get; set; }
        public string subtotal_price { get; set; }
        public SubtotalPriceSet subtotal_price_set { get; set; }
        public string tags { get; set; }
        public List<TaxLine> tax_lines { get; set; }
        public bool taxes_included { get; set; }
        public bool test { get; set; }
        public string token { get; set; }
        public string total_discounts { get; set; }
        public TotalDiscountsSet total_discounts_set { get; set; }
        public string total_line_items_price { get; set; }
        public TotalLineItemsPriceSet total_line_items_price_set { get; set; }
        public string total_outstanding { get; set; }
        public string total_price { get; set; }
        public TotalPriceSet total_price_set { get; set; }
        public TotalShippingPriceSet total_shipping_price_set { get; set; }
        public string total_tax { get; set; }
        public TotalTaxSet total_tax_set { get; set; }
        public string total_tip_received { get; set; }
        public int total_weight { get; set; }
        public DateTime updated_at { get; set; }
        public int user_id { get; set; }
        public OrderStatusUrl order_status_url { get; set; }
    }

    public class ShippingAddress
    {
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public object company { get; set; }
        public string country { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string phone { get; set; }
        public string province { get; set; }
        public string zip { get; set; }
        public string name { get; set; }
        public string country_code { get; set; }
        public string province_code { get; set; }
    }

    public class ShippingLine
    {
        public string code { get; set; }
        public string price { get; set; }
        public PriceSet price_set { get; set; }
        public string discounted_price { get; set; }
        public DiscountedPriceSet discounted_price_set { get; set; }
        public string source { get; set; }
        public string title { get; set; }
        public List<object> tax_lines { get; set; }
        public string carrier_identifier { get; set; }
        public string requested_fulfillment_service_id { get; set; }
    }

    public class ShopMoney
    {
        public string amount { get; set; }
        public string currency_code { get; set; }
    }

    public class SubtotalPriceSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class TaxExemptions
    {
    }

    public class TaxLine
    {
        public string title { get; set; }
        public string price { get; set; }
        public PriceSet price_set { get; set; }
        public bool channel_liable { get; set; }
        public double rate { get; set; }
    }

    public class TotalDiscountSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class TotalDiscountsSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class TotalLineItemsPriceSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class TotalPriceSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class TotalShippingPriceSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }

    public class TotalTaxSet
    {
        public ShopMoney shop_money { get; set; }
        public PresentmentMoney presentment_money { get; set; }
    }


    #endregion
}
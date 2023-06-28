using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IConnector
{
    public DataSource dataSource { get; set; }
    Task<ReadRegistryDataResult> ReadRegistryData(DataPipeline pipeline);
    Task<ImportDataResult> ImportRegistryData(DataPipeline pipeline, ReadRegistryDataResult newData);
    Task<ReadTimeDataResult> ReadTimeData(DataPipeline pipeline);
    Task<ImportDataResult> ImportTimeData(DataPipeline pipeline, ReadTimeDataResult newData);
    Task<PrepareDatasourceResult> PrepareDataSource();
    Task<DataSource> CreateDataSource(Dictionary<string, string> metadata);
    Task<DataSource> RefreshTokens();
}

public class ReadTimeDataResult
{
    public List<Order> orders {get; set;}
    public List<Transaction> accounting {get; set;}
    public List<InventoryLevel> inventory {get; set;}
    public List<PaidAdsPerformance> adsPerformances {get; set;}
}

public class ReadRegistryDataResult
{
    public List<Product> products {get; set;}
    public List<InventoryLevel> inventory {get; set;}
}

public class ImportDataResult
{

}
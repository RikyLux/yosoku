using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IConnector
{
    public DataSource dataSource { get; set; }
    Task<List<object>> ReadData(DataPipeline pipeline);
    Task<List<object>> ImportData(DataPipeline pipeline, List<object> newData);
    Task<PrepareDatasourceResult> PrepareDataSource();
    Task<DataSource> CreateDataSource(Dictionary<string, string> metadata);
    Task<DataSource> RefreshTokens();
}
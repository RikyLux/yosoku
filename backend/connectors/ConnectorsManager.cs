using System.Collections.Generic;

public class ConnectorsManager
{
    public IConnector Instantiate(DataSource dataSource)
    {
        switch (dataSource.source)
        {
            case "shopify": return new ShopifyConnector(dataSource);
            default: throw new System.Exception("DATASOURCE NOT IMPLEMENTED");
        }
    }

    // Questo perch� alcune datasources mettono la data dell'item come il primo o l'ultimo giorno del mese quindi dobbiamo cominciare dal primo giorno del mese per prendere tutti rawDataItem 
    public string GetGranularity(DataSource dataSource)
    {
        switch (dataSource.source)
        {
            case "shopify": return "DD";
            default: return "DD";
        }
    }

    public IConnector GetDataPipelineConnector(DataPipeline pipeline)
    {
        return Instantiate(pipeline.dataSource);
    }
}

public class PrepareDatasourceResult
{
    public string uri {get; set;}
}
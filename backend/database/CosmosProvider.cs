using Microsoft.Azure.Cosmos;
using System;

public class CosmosProvider
{
    private static readonly string endpointUri = "https://sturppy-plus-db.documents.azure.com:443/";
    private static readonly string primaryKey = "OdAiHNcZbTBEpxlD24p0aElaghGblcrNgldrCmpB9RnUoxoqOgObU113Nc650XlHAQlXoDZstGyUACDbjjY5Pw==";

    private static string databaseId = "Yosoku";
    public static string projectsContainerId = "Projects";
    public static string ordersContainerId = "Orders";
    public static string productsContainerId = "Products";
    public static string inventoryContainerId = "Inventory";
    public static string dataPipelineContainerId = "DataPipelines";

    public static CosmosClient BuildClient()
    {
        return new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions()
        {
            ConnectionMode = ConnectionMode.Direct,
            RequestTimeout = new TimeSpan(0, 2, 0)
        });
    }

    public static CosmosClient BuildBulkClient()
    {
        return new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions()
        {
            ConnectionMode = ConnectionMode.Direct,
            AllowBulkExecution = true
        });
    }

    public static Database BuildDatabase(CosmosClient client)
    {
        return client.GetDatabase(databaseId);
    }

    public static Container BuildContainer(Database database, string containerId)
    {
        return database.GetContainer(containerId);
    }
}

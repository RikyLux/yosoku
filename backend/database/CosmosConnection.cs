using System;
using Microsoft.Azure.Cosmos;

public static class CosmosConnection
{
    public static Lazy<CosmosClient> client {get; set;} = new Lazy<CosmosClient>(() => CosmosProvider.BuildClient());
    public static Lazy<Database> database {get; set;} = new Lazy<Database>(() => CosmosProvider.BuildDatabase(CosmosConnection.client.Value));

    public static Lazy<CosmosClient> bulkClient {get; set;} = new Lazy<CosmosClient>(() => CosmosProvider.BuildBulkClient());
    public static Lazy<Database> bulkDatabase {get; set;} = new Lazy<Database>(() => CosmosProvider.BuildDatabase(CosmosConnection.bulkClient.Value));
}
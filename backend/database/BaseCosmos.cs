using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract class BaseCosmos
{
    protected SmartContainer projectsContainer;
    protected SmartContainer dataPipelineContainer;

    public BaseCosmos()
    {
        this.projectsContainer = new SmartContainer(CosmosProvider.BuildContainer(CosmosConnection.database.Value, CosmosProvider.projectsContainerId), "id");
        this.dataPipelineContainer = new SmartContainer(CosmosProvider.BuildContainer(CosmosConnection.database.Value, CosmosProvider.dataPipelineContainerId), "projectId");
    }

    protected async Task<List<T>> ExecuteQuery<T>(string query, SmartContainer container, Dictionary<string, object> parameters = null)
    {
        QueryDefinition definition = new QueryDefinition(query);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                definition.WithParameter(param.Key, param.Value);
            }
        }

        FeedIterator<T> queryIterator = container.container.GetItemQueryIterator<T>(definition);

        List<T> result = new List<T>();

        while (queryIterator.HasMoreResults)
        {
            FeedResponse<T> currentResultSet = await queryIterator.ReadNextAsync();
            foreach (T item in currentResultSet)
            {
                result.Add(item);
            }
        }

        return result;
    }

    protected async Task<T> ReadItem<T>(string id, SmartContainer container, string partitionKey)
    {
        try
        {
            var itemResponse = await container.container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return itemResponse.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default(T);
        }
    }

    protected async Task<T> CreateItem<T>(T newItem, SmartContainer container) where T: IModel
    {
        string partitionValue = newItem.GetType().GetProperties().FirstOrDefault(x => x.Name == container.partitionName).GetValue(newItem, null) as string;

        ItemResponse<T> newItemResponse = await container.container.CreateItemAsync<T>(newItem, new PartitionKey(partitionValue));
        return newItemResponse.Resource;
    }

    protected async Task<T> UpdateItem<T>(T updatedItem, SmartContainer container, bool checkETag = false) where T: IModel
    {
        string partitionValue = updatedItem.GetType().GetProperties().FirstOrDefault(x => x.Name == container.partitionName).GetValue(updatedItem, null) as string;

        ItemRequestOptions options = null;
        if(checkETag == true)
            options = new ItemRequestOptions() { IfMatchEtag = updatedItem.GetType().GetProperties().FirstOrDefault(x => x.Name == "_etag").GetValue(updatedItem, null) as string };

        try
        {
            ItemResponse<T> updatedItemResponse = await container.container.ReplaceItemAsync<T>(updatedItem, updatedItem.id, new PartitionKey(partitionValue), options);
            return updatedItemResponse.Resource;
        }
        catch (CosmosException cex)
        {
            if(cex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                throw new System.Exception("PreconditionFailed: trying to update with an older value.");
            else
                throw cex;
        }
    }

    protected async Task<T> SafeUpdateItem<T>(Func<T, T> modifyItem, T item, SmartContainer container, int maxCycle = 30, int cycleCount = 0) where T : IModel
    {
        var newItem = modifyItem(item);

        try
        {
            return await UpdateItem(newItem, container, true);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("PreconditionFailed"))
            {
                if (cycleCount < maxCycle)
                {
                    string partitionValue = item.GetType().GetProperties().FirstOrDefault(x => x.Name == container.partitionName).GetValue(item, null) as string;

                    var freshItem = await ReadItem<T>(item.id, container, partitionValue);
                    return await SafeUpdateItem(modifyItem, freshItem, container, maxCycle, cycleCount + 1);
                }
                else
                {
                    // var mailer = new EmailManager();
                    // await mailer.InternalEmail("ERROR Infinite cycle", $"SafeUpdateItem has exceeded its maximum cycles ({maxCycle}), container {container}, item ({item})");
                    throw;
                }
            }
            else
            {
                throw;
            }
        }
    }

    protected async Task<T> DeleteItem<T>(string itemId, SmartContainer container, string partitionKey)
    {
        ItemResponse<T> updateitemResponse = await container.container.DeleteItemAsync<T>(itemId, new PartitionKey(partitionKey));
        return updateitemResponse.Resource;
    }
}

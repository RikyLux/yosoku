using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DataPipelineManager : BaseCosmos
{
    const int MAX_MONTHS = 13;

    public async Task<ProjectAndSupervisor> PreparePipelines(string projectId, string bgJobId = null)
    {
        var prjManager = new ProjectsManager();
        var connManager = new ConnectorsManager();
        var project = await prjManager.GetProject(projectId);
        if (project == null)
            throw new Exception("No project found.");

        List<DataPipeline> pipelines = new List<DataPipeline>();
        string batchId = "btc_" + Guid.NewGuid().ToString();

        foreach (var source in project.dataSources)
        {
            var elapsedMonths = Enumerable.Range(0, MAX_MONTHS)
                .Select(x => source.lastRefresh.AddMonths(x))
                .TakeWhile(x => x <= DateTime.UtcNow).ToList();

            //Refresho i tokens di metadata (access e refresh token)
            var connectorsManager = new ConnectorsManager();
            IConnector connector = connectorsManager.Instantiate(source);
            source.metadata = (await connector.RefreshTokens()).metadata;
            source.lastRefresh = DateTime.UtcNow;

            int order = 0;
            pipelines.Add(await CreatePipeline(projectId, DateTime.UtcNow, null, source, order, batchId, bgJobId));
            order++;

            // Se dall'ultimo import sono passati più di un mese, allora prepara una pipeline per mese
            if (elapsedMonths.Count > 1)
            {
                foreach (var monthInBetween in elapsedMonths)
                {
                    DateTime start = monthInBetween;
                    DateTime end = start.AddMonths(1).AddMilliseconds(-1);
                    pipelines.Add(await CreatePipeline(projectId, start, end, source, order, batchId, bgJobId));
                    order++;
                }
            }
            else
            {
                // Aggiungi una nuova pipeline utilizzando la data di inizio, ecc.
                pipelines.Add(await CreatePipeline(projectId, source.lastRefresh, null, source, 0, batchId, bgJobId));
            }
        }

        project = await UpdateItem(project, projectsContainer);

        return new ProjectAndSupervisor()
        {
            project = project,
            supervisor = new PipelineSupervisor() { pipelines = pipelines, batchId  = batchId },
        };
    }

    public async Task<DataPipeline> CreatePipeline(string projectId, DateTime fromDate, DateTime? toDate, DataSource source, int order, string batchId, string bgJobId = null)
    {
        var start = Config.Formatted(fromDate);

        // Ottieni la granularità della sorgente
        var connectorsManager = new ConnectorsManager();
        var granularity = connectorsManager.GetGranularity(source);

        // Se la granularità è "MM" (mesi), allora impostiamo start come il primo giorno del mese corrente
        // Questo perchè quelle datasources mettono la data dell'item come il primo o l'ultimo giorno del mese quindi dobbiamo cominciare dal primo giorno del mese per prendere i rawDataItem 
        if (granularity == "MM")
            start = new DateTime(start.Year, start.Month, 1);

        var pipeline = new DataPipeline()
        {
            id = "pl_" + Guid.NewGuid().ToString(),
            created = DateTime.UtcNow,
            projectId = projectId,
            order = order,
            status = DataPipelineConfig.CreatedStatus,
            dataSource = source,
            fromDate = start,
            batchId = batchId,
            bgJobId = bgJobId,
            toDate = toDate > DateTime.UtcNow ? null : (toDate != null ? Config.Formatted(toDate.Value) : null),
            steps = new List<DataPipelineStep>()
            {
                new DataPipelineStep()
                {
                    id = "pls_" + Guid.NewGuid().ToString(),
                    name = DataPipelineStepConfig.ReadExternalDataStep
                }
            }
        };

        pipeline = await CreateItem(pipeline, dataPipelineContainer);
        return pipeline;
    }
}
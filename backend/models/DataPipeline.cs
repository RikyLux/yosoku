using System;
using System.Collections.Generic;

public class DataPipeline : IModel
{
    public string id {get; set;}
    public string projectId {get; set;}
    public int order {get; set;}
    public DateTime created {get; set;}
    public DateTime? completed {get; set;}
    public string status {get; set;}
    public DateTime fromDate {get; set;}
    public DateTime? toDate {get; set;}
    public DataSource dataSource {get; set;}
    public List<DataPipelineStep> steps {get; set;}
    public string batchId { get; set; }
    public string bgJobId { get; set; }
}

public class DataPipelineStep
{
    public string id {get; set;}
    public string name {get; set;}
    public DateTime start {get; set;}
    public DateTime? end {get; set;}
    public string error {get; set;}
    public string stackTrace {get; set;}
    public int processedItems {get; set;}
}

public static class DataPipelineConfig
{
    public const string CreatedStatus = "created";
    public const string RunningStatus = "running";
    public const string CompletedStatus = "completed";
    public const string FailedStatus = "failed";
    public const string AbortedStatus = "aborted";
}

public static class DataPipelineStepConfig
{
    public const string ReadExternalDataStep = "read-external-data";
    public const string CleanRawDataStep = "clean-raw-data";
    public const string TransformAndStoreStep = "transform-and-store";
    public const string LaunchNextStep = "launch-next";
}

public class PipelineSupervisor
{
    public string batchId { get; set; }
    public List<DataPipeline> pipelines { get; set;}
}
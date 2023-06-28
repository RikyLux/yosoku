using Microsoft.Azure.Cosmos;

public class SmartContainer
{
    public Container container {get; set;}
    /// <summary>
    /// il nome della partizione impostata in fase di creazione del container
    /// </summary>
    public string partitionName {get; set;}

    public SmartContainer(Container container, string partitionName)
    {
        this.container = container;
        this.partitionName = partitionName;
    }
}
using Microsoft.Azure.Cosmos;

public interface ICosmosContainer
{
    ContainerProperties CosmosContainer { get; set; }
    Task<ContainerProperties> CreateConatinerAsync();
}

using Microsoft.Xrm.Sdk.Metadata;

namespace FieldDefinitionGenerator.Logic.Services.Abstractions
{
    public interface IGenerationService
    {
        string Generate(EntityMetadata entityMetadata);
    }
}

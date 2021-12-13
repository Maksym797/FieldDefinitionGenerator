using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace FieldDefinitionGenerator.Logic.Services.Abstractions
{
    public abstract class GenerationService : IGenerationService
    {
        protected IOrganizationService Service { get; }

        protected GenerationService(IOrganizationService service)
        {
            Service = service;
        }
        public abstract string Generate(EntityMetadata entityMetadata);

        protected string AddTabs(int number)
        {
            return new string(' ', number * 5);
        }
    }
}

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SankalpAPI.Helper
{
    public class RemoveAdditionalPropertiesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.AdditionalProperties = null;
            schema.AdditionalPropertiesAllowed = false;
        }
    }
}

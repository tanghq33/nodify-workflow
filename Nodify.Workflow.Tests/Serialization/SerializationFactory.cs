using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nodify.Workflow.Tests.Serialization
{
    public static class SerializationFactory
    {
        private static JsonSerializerOptions? _options;

        public static JsonSerializerOptions CreateOptions()
        {
            if (_options != null)
                return _options;

            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Add our custom converters
            _options.Converters.Add(new GraphJsonConverter());
            _options.Converters.Add(new JsonStringEnumConverter());

            return _options;
        }
    }
} 
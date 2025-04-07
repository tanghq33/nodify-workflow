using System;
using System.Collections.Generic;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Nodes.Data;

namespace Nodify.Workflow.Tests.Serialization
{
    public static class SerializationTypeMap
    {
        private static readonly Dictionary<string, Type> _typeMap = new()
        {
            { "Graph", typeof(Graph) },
            { "OutputNode", typeof(OutputNode) },
            { "InputJsonNode", typeof(InputJsonNode) },
            { "Connector", typeof(Connector) }
        };

        private static readonly Dictionary<Type, string> _reverseTypeMap;

        static SerializationTypeMap()
        {
            _reverseTypeMap = new Dictionary<Type, string>();
            foreach (var kvp in _typeMap)
            {
                _reverseTypeMap[kvp.Value] = kvp.Key;
            }
        }

        public static Type? GetType(string typeId)
        {
            return _typeMap.TryGetValue(typeId, out var type) ? type : null;
        }

        public static string? GetTypeId(Type type)
        {
            return _reverseTypeMap.TryGetValue(type, out var typeId) ? typeId : null;
        }
    }
} 
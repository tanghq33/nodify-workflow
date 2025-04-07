using Xunit;
using Shouldly;
using System.Text.Json;
using System.Collections.Generic;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Nodes.Data;
using System;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text.Json.Serialization.Metadata;

namespace Nodify.Workflow.Tests.Serialization;

// Custom converter for System.Type that stores the type's AssemblyQualifiedName
public class TypeJsonConverter : JsonConverter<Type>
{
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? assemblyQualifiedName = reader.GetString();
        return assemblyQualifiedName == null ? typeof(object) : Type.GetType(assemblyQualifiedName)!;
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AssemblyQualifiedName);
    }
}

// Custom converter for collections of INode
public class NodeCollectionConverter : JsonConverter<ICollection<INode>>
{
    public override ICollection<INode>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var nodes = new List<INode>();
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var root = jsonDocument.RootElement;

        if (root.TryGetProperty("$values", out var valuesElement))
        {
            foreach (var nodeElement in valuesElement.EnumerateArray())
            {
                if (nodeElement.TryGetProperty("$type", out var typeElement))
                {
                    var typeString = typeElement.GetString();
                    var type = Type.GetType(typeString!);
                    if (type != null)
                    {
                        var node = (INode)JsonSerializer.Deserialize(nodeElement.GetRawText(), type, options)!;
                        nodes.Add(node);
                    }
                }
            }
        }

        return nodes;
    }

    public override void Write(Utf8JsonWriter writer, ICollection<INode> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

// Custom converter for collections of IConnector
public class ConnectorCollectionConverter : JsonConverter<ICollection<IConnector>>
{
    public override ICollection<IConnector>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var connectors = new List<IConnector>();
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var root = jsonDocument.RootElement;

        if (root.TryGetProperty("$values", out var valuesElement))
        {
            foreach (var connectorElement in valuesElement.EnumerateArray())
            {
                if (connectorElement.TryGetProperty("$type", out var typeElement))
                {
                    var typeString = typeElement.GetString();
                    var type = Type.GetType(typeString!);
                    if (type != null)
                    {
                        var connector = (IConnector)JsonSerializer.Deserialize(connectorElement.GetRawText(), type, options)!;
                        connectors.Add(connector);
                    }
                }
            }
        }

        return connectors;
    }

    public override void Write(Utf8JsonWriter writer, ICollection<IConnector> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

// Custom converter for Graph class
public class GraphJsonConverter : JsonConverter<Graph>
{
    public override Graph? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var graph = new Graph();

        // First, deserialize all nodes
        if (root.TryGetProperty("nodes", out var nodesElement))
        {
            foreach (var nodeElement in nodesElement.EnumerateArray())
            {
                if (nodeElement.TryGetProperty("type", out var typeElement) &&
                    nodeElement.TryGetProperty("id", out var idElement))
                {
                    var typeId = typeElement.GetString();
                    var type = SerializationTypeMap.GetType(typeId!);
                    if (type != null)
                    {
                        // Create a new JsonSerializerOptions without our custom converter
                        // to avoid infinite recursion
                        var nodeOptions = new JsonSerializerOptions(options);
                        var converters = nodeOptions.Converters.ToList();
                        converters.RemoveAll(c => c is GraphJsonConverter);
                        nodeOptions.Converters.Clear();
                        foreach (var converter in converters)
                        {
                            nodeOptions.Converters.Add(converter);
                        }

                        // Create an instance with the correct ID
                        var nodeId = Guid.Parse(idElement.GetString()!);
                        var node = (INode)Activator.CreateInstance(type, nodeId)!;

                        // Deserialize the properties into a new instance
                        var tempNode = (INode)JsonSerializer.Deserialize(nodeElement.GetRawText(), type, nodeOptions)!;

                        // Copy properties from tempNode to node (except Id which we want to preserve)
                        foreach (var property in type.GetProperties())
                        {
                            if (property.Name != nameof(INode.Id) && property.CanWrite)
                            {
                                var value = property.GetValue(tempNode);
                                property.SetValue(node, value);
                            }
                        }

                        graph.AddNode(node);
                    }
                }
            }
        }

        // Then, handle connections
        if (root.TryGetProperty("connections", out var connectionsElement))
        {
            foreach (var connectionElement in connectionsElement.EnumerateArray())
            {
                if (connectionElement.TryGetProperty("source", out var sourceElement) &&
                    connectionElement.TryGetProperty("target", out var targetElement))
                {
                    var sourceId = sourceElement.GetProperty("id").GetString();
                    var targetId = targetElement.GetProperty("id").GetString();

                    var sourceConnector = FindConnector(graph, Guid.Parse(sourceId!));
                    var targetConnector = FindConnector(graph, Guid.Parse(targetId!));

                    if (sourceConnector != null && targetConnector != null)
                    {
                        graph.AddConnection(sourceConnector, targetConnector);
                    }
                }
            }
        }

        return graph;
    }

    private IConnector? FindConnector(Graph graph, Guid connectorId)
    {
        foreach (var node in graph.Nodes)
        {
            var connector = node.InputConnectors.FirstOrDefault(c => c.Id == connectorId);
            if (connector != null)
                return connector;

            connector = node.OutputConnectors.FirstOrDefault(c => c.Id == connectorId);
            if (connector != null)
                return connector;
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, Graph value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write type information
        writer.WriteString("type", SerializationTypeMap.GetTypeId(typeof(Graph)));

        // Write nodes
        writer.WritePropertyName("nodes");
        writer.WriteStartArray();
        foreach (var node in value.Nodes)
        {
            writer.WriteStartObject();
            writer.WriteString("type", SerializationTypeMap.GetTypeId(node.GetType()));
            writer.WriteString("id", node.Id.ToString());
            
            // Write input connectors
            writer.WritePropertyName("inputs");
            writer.WriteStartArray();
            foreach (var connector in node.InputConnectors)
            {
                WriteConnector(writer, connector, options);
            }
            writer.WriteEndArray();

            // Write output connectors
            writer.WritePropertyName("outputs");
            writer.WriteStartArray();
            foreach (var connector in node.OutputConnectors)
            {
                WriteConnector(writer, connector, options);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // Write connections
        writer.WritePropertyName("connections");
        writer.WriteStartArray();
        foreach (var connection in value.Connections)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("source");
            WriteConnector(writer, connection.Source, options);
            writer.WritePropertyName("target");
            WriteConnector(writer, connection.Target, options);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private void WriteConnector(Utf8JsonWriter writer, IConnector connector, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", SerializationTypeMap.GetTypeId(connector.GetType()));
        writer.WriteString("id", connector.Id.ToString());
        writer.WriteString("dataType", connector.DataType.FullName);
        writer.WriteEndObject();
    }
}

public class GraphSerializationTests
{
    private JsonSerializerOptions GetSerializerOptions()
    {
        return SerializationFactory.CreateOptions();
    }

    [Fact]
    public void Serialize_EmptyGraph_ShouldSucceed()
    {
        // Arrange
        var graph = new Graph();
        var options = GetSerializerOptions();

        // Act
        var json = JsonSerializer.Serialize(graph, options);
        
        // Assert
        json.ShouldNotBeNullOrEmpty();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        
        // Verify the basic structure
        result.TryGetProperty("type", out var typeElement).ShouldBeTrue("JSON should include type");
        typeElement.GetString().ShouldBe("Graph");
        result.TryGetProperty("nodes", out _).ShouldBeTrue("JSON should have nodes array");
        result.TryGetProperty("connections", out _).ShouldBeTrue("JSON should have connections array");
    }

    [Fact]
    public void Serialize_GraphWithNode_ShouldPreserveBasicStructure()
    {
        // Arrange
        var graph = new Graph();
        var node = new OutputNode();
        graph.AddNode(node);
        
        var options = GetSerializerOptions();

        // Act
        var json = JsonSerializer.Serialize(graph, options);
        
        // Assert
        json.ShouldNotBeNullOrEmpty();
        
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        
        // Check type
        jsonElement.TryGetProperty("type", out var typeElement).ShouldBeTrue();
        typeElement.GetString().ShouldBe("Graph");
        
        // Check nodes array
        jsonElement.TryGetProperty("nodes", out var nodesElement).ShouldBeTrue();
        var nodes = nodesElement.EnumerateArray().ToList();
        nodes.Count.ShouldBe(1);
        
        // Check node structure
        var nodeElement = nodes[0];
        nodeElement.TryGetProperty("type", out var nodeTypeElement).ShouldBeTrue();
        nodeTypeElement.GetString().ShouldBe("OutputNode");
        nodeElement.TryGetProperty("id", out var idElement).ShouldBeTrue();
        Guid.TryParse(idElement.GetString(), out _).ShouldBeTrue("Node ID should be a valid GUID");
        
        // Check node connectors
        nodeElement.TryGetProperty("inputs", out var inputsElement).ShouldBeTrue();
        var inputs = inputsElement.EnumerateArray().ToList();
        inputs.Count.ShouldBeGreaterThan(0, "OutputNode should have at least one input connector");
        
        var firstConnector = inputs[0];
        firstConnector.TryGetProperty("dataType", out var dataTypeElement).ShouldBeTrue();
        dataTypeElement.GetString().ShouldContain("System.Object");
    }

    [Fact]
    public void Deserialize_EmptyGraphJson_ShouldCreateEmptyGraph()
    {
        // Arrange
        var json = @"{
            ""type"": ""Graph"",
            ""nodes"": [],
            ""connections"": []
        }";
        var options = GetSerializerOptions();

        // Act
        var graph = JsonSerializer.Deserialize<Graph>(json, options);

        // Assert
        graph.ShouldNotBeNull();
        graph.Nodes.ShouldBeEmpty();
        graph.Connections.ShouldBeEmpty();
    }

    [Fact]
    public void Deserialize_GraphWithSingleNode_ShouldRecreateNodeAndConnectors()
    {
        // Arrange
        var json = @"{
            ""type"": ""Graph"",
            ""nodes"": [
                {
                    ""type"": ""OutputNode"",
                    ""id"": ""12345678-1234-1234-1234-123456789012"",
                    ""inputs"": [
                        {
                            ""type"": ""Connector"",
                            ""id"": ""87654321-4321-4321-4321-987654321098"",
                            ""dataType"": ""System.Object""
                        }
                    ],
                    ""outputs"": []
                }
            ],
            ""connections"": []
        }";
        var options = GetSerializerOptions();

        // Act
        var graph = JsonSerializer.Deserialize<Graph>(json, options);

        // Assert
        graph.ShouldNotBeNull();
        graph.Nodes.Count.ShouldBe(1);
        
        var node = graph.Nodes.First();
        node.ShouldBeOfType<OutputNode>();
        node.Id.ToString().ShouldBe("12345678-1234-1234-1234-123456789012");
        
        node.InputConnectors.Count.ShouldBe(1);
        var connector = node.InputConnectors.First();
        connector.Id.ToString().ShouldBe("87654321-4321-4321-4321-987654321098");
        connector.DataType.ShouldBe(typeof(object));
    }
} 
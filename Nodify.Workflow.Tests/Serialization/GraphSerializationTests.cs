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
                        // Create an instance with the correct ID
                        var nodeId = Guid.Parse(idElement.GetString()!);
                        INode node;
                        try
                        {
                            // Try to create instance with Guid constructor
                            node = (INode)Activator.CreateInstance(type, nodeId)!;
                        }
                        catch (MissingMethodException)
                        {
                            // If no Guid constructor exists, use parameterless constructor
                            node = (INode)Activator.CreateInstance(type)!;
                            // Set the Id using reflection
                            var idProperty = type.GetProperty(nameof(INode.Id));
                            if (idProperty?.CanWrite == true)
                            {
                                idProperty.SetValue(node, nodeId);
                            }
                        }

                        // Handle input connectors
                        if (nodeElement.TryGetProperty("inputs", out var inputsElement))
                        {
                            foreach (var connectorElement in inputsElement.EnumerateArray())
                            {
                                var connectorId = Guid.Parse(connectorElement.GetProperty("id").GetString()!);
                                var dataTypeName = connectorElement.GetProperty("dataType").GetString()!;
                                var dataType = Type.GetType(dataTypeName) ?? typeof(object);

                                // Check if a connector with this ID already exists
                                var existingConnector = node.InputConnectors.FirstOrDefault(c => c.Id == connectorId);
                                if (existingConnector == null)
                                {
                                    var connector = new Connector(node, ConnectorDirection.Input, dataType, connectorId);
                                    node.AddInputConnector(connector);
                                }
                            }
                        }

                        // Handle output connectors
                        if (nodeElement.TryGetProperty("outputs", out var outputsElement))
                        {
                            foreach (var connectorElement in outputsElement.EnumerateArray())
                            {
                                var connectorId = Guid.Parse(connectorElement.GetProperty("id").GetString()!);
                                var dataTypeName = connectorElement.GetProperty("dataType").GetString()!;
                                var dataType = Type.GetType(dataTypeName) ?? typeof(object);

                                // Check if a connector with this ID already exists
                                var existingConnector = node.OutputConnectors.FirstOrDefault(c => c.Id == connectorId);
                                if (existingConnector == null)
                                {
                                    var connector = new Connector(node, ConnectorDirection.Output, dataType, connectorId);
                                    node.AddOutputConnector(connector);
                                }
                            }
                        }

                        graph.AddNode(node);
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Node type '{typeId}' not found in type map.");
                    }
                }
                else
                {
                    throw new JsonException("Node is missing required 'type' or 'id' property.");
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

        // Write version information
        writer.WriteString("$version", "1.0");

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

    [Fact]
    public void Serialize_ShouldIncludeVersion()
    {
        // Arrange
        var graph = new Graph();
        var options = SerializationFactory.CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(graph, options);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        jsonDoc.RootElement.TryGetProperty("$version", out var versionElement).ShouldBeTrue();
        versionElement.GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var options = SerializationFactory.CreateOptions();

        // Act & Assert
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Graph>(invalidJson, options));
    }

    [Fact]
    public void Deserialize_JsonWithUnknownNodeType_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var jsonWithUnknownType = @"{
            ""type"": ""Graph"",
            ""$version"": ""1.0"",
            ""nodes"": [{
                ""type"": ""UnknownNodeType"",
                ""id"": ""00000000-0000-0000-0000-000000000001"",
                ""inputs"": [],
                ""outputs"": []
            }],
            ""connections"": []
        }";
        var options = SerializationFactory.CreateOptions();

        // Act & Assert
        Should.Throw<KeyNotFoundException>(() => JsonSerializer.Deserialize<Graph>(jsonWithUnknownType, options));
    }

    [Fact]
    public void Deserialize_JsonNodeMissingRequiredProperty_ShouldThrowJsonException()
    {
        // Arrange
        var jsonMissingId = @"{
            ""type"": ""Graph"",
            ""$version"": ""1.0"",
            ""nodes"": [{
                ""type"": ""InputJsonNode"",
                ""inputs"": [],
                ""outputs"": []
            }],
            ""connections"": []
        }";
        var options = SerializationFactory.CreateOptions();

        // Act & Assert
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Graph>(jsonMissingId, options));
    }

    [Fact]
    public void Serialize_GraphWithConnectedNodes_ShouldOutputCorrectJson()
    {
        // Arrange
        var graph = new Graph();
        
        // Create input node
        var inputNode = new InputJsonNode();
        graph.AddNode(inputNode);
        
        // Create output node
        var outputNode = new OutputNode();
        graph.AddNode(outputNode);
        
        // Connect input to output
        var connection = graph.AddConnection(
            inputNode.OutputConnectors.First(),
            outputNode.InputConnectors.First()
        );
        
        var options = SerializationFactory.CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(graph, options);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        jsonDoc.RootElement.GetProperty("nodes").GetArrayLength().ShouldBe(2);
        
        var connections = jsonDoc.RootElement.GetProperty("connections");
        connections.GetArrayLength().ShouldBe(1);
        
        var firstConnection = connections[0];
        firstConnection.GetProperty("source").GetProperty("id").GetString()
            .ShouldBe(inputNode.OutputConnectors.First().Id.ToString());
        firstConnection.GetProperty("target").GetProperty("id").GetString()
            .ShouldBe(outputNode.InputConnectors.First().Id.ToString());
    }

    [Fact]
    public void Deserialize_JsonWithConnectedNodes_ShouldReconstructGraph()
    {
        // Arrange
        var inputNodeId = Guid.NewGuid();
        var outputNodeId = Guid.NewGuid();
        var inputConnectorId = Guid.NewGuid();
        var outputConnectorId = Guid.NewGuid();
        
        var json = @"{
            ""type"": ""Graph"",
            ""$version"": ""1.0"",
            ""nodes"": [
                {
                    ""type"": ""InputJsonNode"",
                    ""id"": """ + inputNodeId + @""",
                    ""inputs"": [],
                    ""outputs"": [
                        {
                            ""type"": ""Connector"",
                            ""id"": """ + outputConnectorId + @""",
                            ""dataType"": ""System.Object, System.Private.CoreLib""
                        }
                    ]
                },
                {
                    ""type"": ""OutputNode"",
                    ""id"": """ + outputNodeId + @""",
                    ""inputs"": [
                        {
                            ""type"": ""Connector"",
                            ""id"": """ + inputConnectorId + @""",
                            ""dataType"": ""System.Object, System.Private.CoreLib""
                        }
                    ],
                    ""outputs"": []
                }
            ],
            ""connections"": [
                {
                    ""source"": {
                        ""type"": ""Connector"",
                        ""id"": """ + outputConnectorId + @""",
                        ""dataType"": ""System.Object, System.Private.CoreLib""
                    },
                    ""target"": {
                        ""type"": ""Connector"",
                        ""id"": """ + inputConnectorId + @""",
                        ""dataType"": ""System.Object, System.Private.CoreLib""
                    }
                }
            ]
        }";
        
        var options = SerializationFactory.CreateOptions();

        // Act
        var graph = JsonSerializer.Deserialize<Graph>(json, options);

        // Assert
        graph.ShouldNotBeNull();
        graph.Nodes.Count.ShouldBe(2);
        graph.Connections.Count.ShouldBe(1);
        
        var inputNode = graph.Nodes.OfType<InputJsonNode>().Single();
        var outputNode = graph.Nodes.OfType<OutputNode>().Single();
        
        inputNode.Id.ShouldBe(inputNodeId);
        outputNode.Id.ShouldBe(outputNodeId);
        
        var connection = graph.Connections.Single();
        connection.Source.Id.ShouldBe(outputConnectorId);
        connection.Target.Id.ShouldBe(inputConnectorId);
        
        // Verify connection endpoints
        var sourceConnector = inputNode.OutputConnectors.Single(c => c.Id == outputConnectorId);
        var targetConnector = outputNode.InputConnectors.Single(c => c.Id == inputConnectorId);
        connection.Source.ShouldBe(sourceConnector);
        connection.Target.ShouldBe(targetConnector);
    }
} 
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

public class GraphSerializationTests
{
    private JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve, // Handle circular references
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new TypeJsonConverter() }
        };
    }

    [Fact]
    public void Serialize_EmptyGraph_ShouldSucceed()
    {
        // Arrange
        var graph = new Graph();
        var options = GetSerializerOptions();

        // Act
        var json = JsonSerializer.Serialize(graph, options);
        
        // Assert - Just verify it doesn't throw and produces some JSON
        json.ShouldNotBeNullOrEmpty();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        
        // Verify the root JSON object has a reference id and references map
        result.TryGetProperty("$id", out _).ShouldBeTrue("JSON should include reference ID");
        result.TryGetProperty("$values", out _).ShouldBeFalse("JSON should not have $values at root");
    }

    [Fact]
    public void Serialize_GraphWithNode_ShouldPreserveBasicStructure()
    {
        // Arrange
        var graph = new Graph();
        var node = new OutputNode();
        graph.AddNode(node);
        
        var options = GetSerializerOptions();

        // Act - We need custom converters to handle Type properties
        var json = JsonSerializer.Serialize(graph, options);
        
        // Assert - Just check that serialization succeeds and has the expected structure
        json.ShouldNotBeNullOrEmpty();
        
        // Check the structure of the serialized JSON
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        
        // Nodes collection should be present and have one value
        jsonElement.TryGetProperty("Nodes", out var nodesElement).ShouldBeTrue();
        nodesElement.TryGetProperty("$values", out var nodeValuesElement).ShouldBeTrue();
        nodeValuesElement.GetArrayLength().ShouldBe(1);
        
        // The node should have a GUID ID
        var nodeElement = nodeValuesElement[0];
        nodeElement.TryGetProperty("Id", out var idElement).ShouldBeTrue();
        Guid.TryParse(idElement.GetString(), out _).ShouldBeTrue("Node ID should be a valid GUID");
        
        // The node should have InputConnectors (OutputNode has one input connector)
        nodeElement.TryGetProperty("InputConnectors", out var inputConnectorsElement).ShouldBeTrue();
        inputConnectorsElement.TryGetProperty("$values", out var inputConnectorValuesElement).ShouldBeTrue();
        inputConnectorValuesElement.GetArrayLength().ShouldBeGreaterThan(0, "OutputNode should have at least one input connector");
        
        // The first connector should have a DataType property that contains "System.Object"
        var firstConnector = inputConnectorValuesElement[0];
        firstConnector.TryGetProperty("DataType", out var dataTypeElement).ShouldBeTrue();
        dataTypeElement.GetString().ShouldContain("System.Object");
    }

    [Fact]
    public void Serialize_GraphWithConnection_ShouldPreserveConnectionStructure()
    {
        // Arrange
        var graph = new Graph();
        
        // Create a source node with output connector
        var inputNode = new InputJsonNode
        {
            JsonContent = "{\"data\": 123}"
        };
        graph.AddNode(inputNode);
        
        // Create a target node with input connector
        var outputNode = new OutputNode
        {
            VariableName = "TestResult"
        };
        graph.AddNode(outputNode);
        
        // Get source output connector and target input connector
        var sourceConnector = inputNode.OutputConnectors.First();
        var targetConnector = outputNode.InputConnectors.First();
        
        // Create a connection
        var connection = graph.AddConnection(sourceConnector, targetConnector);
        connection.ShouldNotBeNull("Connection should have been created");
        
        var options = GetSerializerOptions();

        // Act
        var json = JsonSerializer.Serialize(graph, options);
        
        // Assert
        json.ShouldNotBeNullOrEmpty();
        
        // Check JSON structure for connections
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        
        // Connections collection should exist
        jsonElement.TryGetProperty("Connections", out var connectionsElement).ShouldBeTrue();
        connectionsElement.TryGetProperty("$values", out var connectionValuesElement).ShouldBeTrue();
        
        // Should have one connection
        connectionValuesElement.GetArrayLength().ShouldBe(1, "Should have one connection");
        
        // Verify the connection element - it should be a reference object in the JSON
        var connectionElement = connectionValuesElement[0];
        connectionElement.TryGetProperty("$ref", out var refElement).ShouldBeTrue("Connection should be a reference to an object");
        
        // The reference should be to some ID in the serialized JSON
        var refId = refElement.GetString();
        refId.ShouldNotBeNull();
        
        // Verify the JSON contains nodes with appropriate connectors
        jsonElement.TryGetProperty("Nodes", out var nodesElement).ShouldBeTrue();
        nodesElement.TryGetProperty("$values", out var nodeValuesElement).ShouldBeTrue();
        nodeValuesElement.GetArrayLength().ShouldBe(2, "Should have exactly two nodes");
    }

    // Add more tests here later...
} 
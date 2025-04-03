using Xunit;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using Nodify.Workflow.Core.Registry;
using Nodify.Workflow.Tests.Core.Registry.Helpers; // For test nodes
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Models; // Added for Node base class

namespace Nodify.Workflow.Tests.Core.Registry;

public class NodeRegistryTests
{
    private DefaultNodeRegistry CreateRegistryForTestAssembly()
    {
        // Scan the current assembly where the test nodes are defined
        return new DefaultNodeRegistry(new[] { Assembly.GetExecutingAssembly() });
    }

    [Fact]
    public void GetAvailableNodeTypes_ShouldDiscoverNodesWithAttribute()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();

        // Act
        var nodeTypes = registry.GetAvailableNodeTypes().ToList();

        // Assert
        nodeTypes.Count.ShouldBe(5); // Now access Count property
        nodeTypes.ShouldContain(m => m.NodeType == typeof(SimpleRegisteredNode));
        nodeTypes.ShouldContain(m => m.NodeType == typeof(AnotherRegisteredNode));
        nodeTypes.ShouldContain(m => m.NodeType == typeof(DifferentCategoryNode));
        nodeTypes.ShouldContain(m => m.NodeType == typeof(NodeWithParamConstructor));
        nodeTypes.ShouldContain(m => m.NodeType == typeof(DirectlyImplementingNode));
    }

    [Fact]
    public void GetAvailableNodeTypes_ShouldNotDiscoverNodesWithoutAttribute()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();

        // Act
        var nodeTypes = registry.GetAvailableNodeTypes();

        // Assert
        nodeTypes.ShouldNotContain(m => m.NodeType == typeof(UnregisteredNode));
    }

    [Fact]
    public void GetAvailableNodeTypes_ShouldNotDiscoverAbstractNodes()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();

        // Act
        var nodeTypes = registry.GetAvailableNodeTypes();

        // Assert
        nodeTypes.ShouldNotContain(m => m.NodeType == typeof(AbstractRegisteredNode));
    }

    [Fact]
    public void GetAvailableNodeTypes_ShouldExtractCorrectMetadata()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();

        // Act
        var simpleNodeMeta = registry.GetAvailableNodeTypes().FirstOrDefault(m => m.NodeType == typeof(SimpleRegisteredNode));
        var anotherNodeMeta = registry.GetAvailableNodeTypes().FirstOrDefault(m => m.NodeType == typeof(AnotherRegisteredNode));
        var differentCatMeta = registry.GetAvailableNodeTypes().FirstOrDefault(m => m.NodeType == typeof(DifferentCategoryNode));
        var directMeta = registry.GetAvailableNodeTypes().FirstOrDefault(m => m.NodeType == typeof(DirectlyImplementingNode));

        // Assert
        simpleNodeMeta.ShouldNotBeNull();
        simpleNodeMeta.DisplayName.ShouldBe("Simple Node");
        simpleNodeMeta.Category.ShouldBe("Test Category");
        simpleNodeMeta.Description.ShouldBe("A basic runnable node.");

        anotherNodeMeta.ShouldNotBeNull();
        anotherNodeMeta.DisplayName.ShouldBe("Another Node");
        anotherNodeMeta.Category.ShouldBe("Test Category");
        anotherNodeMeta.Description.ShouldBeEmpty(); // Default description
        
        differentCatMeta.ShouldNotBeNull();
        differentCatMeta.DisplayName.ShouldBe("Different Cat");
        differentCatMeta.Category.ShouldBe("Other Category");
        differentCatMeta.Description.ShouldBeEmpty();

        directMeta.ShouldNotBeNull();
        directMeta.DisplayName.ShouldBe("Direct Interface");
        directMeta.Category.ShouldBe("Test Category");
    }

    [Fact]
    public void CreateNodeInstance_ByType_ShouldCreateInstanceOfRegisteredType()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var targetType = typeof(SimpleRegisteredNode);

        // Act
        var instance = registry.CreateNodeInstance(targetType);

        // Assert
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<SimpleRegisteredNode>();
        // Ensure it's a new instance
        var instance2 = registry.CreateNodeInstance(targetType);
        instance.ShouldNotBeSameAs(instance2);
    }

    [Fact]
    public void CreateNodeInstance_ByType_ShouldWorkForDirectImplementations()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var targetType = typeof(DirectlyImplementingNode);

        // Act
        var instance = registry.CreateNodeInstance(targetType);

        // Assert
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<DirectlyImplementingNode>();
    }

    [Fact]
    public void CreateNodeInstance_ByType_ShouldThrowForUnregisteredType()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var targetType = typeof(UnregisteredNode);

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.CreateNodeInstance(targetType))
            .Message.ShouldContain("not registered or discoverable");
    }

    [Fact]
    public void CreateNodeInstance_ByType_ShouldThrowForAbstractType()
    {
         // Arrange
        var registry = CreateRegistryForTestAssembly();
        // Abstract types aren't added to _registeredTypes, so this check might be redundant
        // but good to ensure it fails correctly if logic changes.
        var targetType = typeof(AbstractRegisteredNode);

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.CreateNodeInstance(targetType))
            .Message.ShouldContain("not registered or discoverable");
    }

    [Fact]
    public void CreateNodeInstance_ByType_ShouldThrowForTypeMissingParameterlessConstructor()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var targetType = typeof(NodeWithParamConstructor);

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.CreateNodeInstance(targetType))
            .Message.ShouldContain("parameterless constructor");
    }

    [Fact]
    public void CreateNodeInstance_ByDisplayName_ShouldCreateInstance()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var displayName = "Simple Node";

        // Act
        var instance = registry.CreateNodeInstance(displayName);

        // Assert
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<SimpleRegisteredNode>();
    }
    
    [Fact]
    public void CreateNodeInstance_ByDisplayName_ShouldBeCaseInsensitive()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var displayNameLower = "simple node";

        // Act
        var instance = registry.CreateNodeInstance(displayNameLower);

        // Assert
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<SimpleRegisteredNode>();
    }

    [Fact]
    public void CreateNodeInstance_ByDisplayName_ShouldThrowForUnknownName()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var displayName = "Unknown Node Name";

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.CreateNodeInstance(displayName))
            .Message.ShouldContain("No node type registered with display name");
    }

     [Fact]
    public void CreateNodeInstance_ByDisplayName_ShouldThrowForTypeMissingParameterlessConstructor()
    {
        // Arrange
        var registry = CreateRegistryForTestAssembly();
        var displayName = "Needs Param"; // Display name of NodeWithParamConstructor

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.CreateNodeInstance(displayName))
            .Message.ShouldContain("parameterless constructor");
    }
} 
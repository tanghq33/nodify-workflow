using System;
using System.Collections.Generic;
using Nodify.Core.Nodes; // Assuming INode is here
using Nodify.Core.Registry; // Assuming NodeRegistry is here
using Shouldly;
using Xunit;

namespace Nodify.Core.Tests.Registry
{
    // Stub node implementations for testing registration
    internal class SimpleTestNode : INode
    {
        public Guid Id { get; } = Guid.NewGuid();
        // Add other minimal INode implementation details if needed by NodeRegistry
    }

    internal class ConditionalTestNode : INode
    {
        public Guid Id { get; } = Guid.NewGuid();
        // Add other minimal INode implementation details if needed by NodeRegistry
    }

    public class NodeRegistryTests
    {
        private readonly NodeRegistry _registry;

        public NodeRegistryTests()
        {
            _registry = new NodeRegistry();
        }

        [Fact]
        public void RegisterNodeType_WhenKeyIsNew_ShouldRegisterSuccessfully()
        {
            // Arrange
            var nodeKey = "SimpleTestNode";
            var nodeType = typeof(SimpleTestNode);

            // Act
            _registry.RegisterNodeType(nodeKey, nodeType);

            // Assert
            // Verify indirectly by trying to retrieve it
            var retrievedType = _registry.GetNodeType(nodeKey);
            retrievedType.ShouldBe(nodeType);
        }

        [Fact]
        public void RegisterNodeType_WhenKeyAlreadyExists_ShouldThrowArgumentException()
        {
            // Arrange
            var nodeKey = "DuplicateNode";
            _registry.RegisterNodeType(nodeKey, typeof(SimpleTestNode));

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
            {
                _registry.RegisterNodeType(nodeKey, typeof(ConditionalTestNode));
            });

            // Optional: Assert that the original registration is unchanged
             var retrievedType = _registry.GetNodeType(nodeKey);
            retrievedType.ShouldBe(typeof(SimpleTestNode));
        }

        [Fact]
        public void RegisterNodeType_WithNullType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var nodeKey = "NullTypeNode";

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
            {
                _registry.RegisterNodeType(nodeKey, null);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")] // Consider if whitespace-only keys are invalid
        public void RegisterNodeType_WithNullOrEmptyKey_ShouldThrowArgumentException(string invalidKey)
        {
            // Arrange
            var nodeType = typeof(SimpleTestNode);

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
            {
                _registry.RegisterNodeType(invalidKey, nodeType);
            });
        }

        [Fact]
        public void GetNodeType_WhenKeyExists_ShouldReturnCorrectType()
        {
            // Arrange
            var nodeKey = "ConditionalNode";
            var expectedType = typeof(ConditionalTestNode);
            _registry.RegisterNodeType(nodeKey, expectedType);

            // Act
            var retrievedType = _registry.GetNodeType(nodeKey);

            // Assert
            retrievedType.ShouldNotBeNull();
            retrievedType.ShouldBe(expectedType);
        }

        [Fact]
        public void GetNodeType_WhenKeyDoesNotExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var unregisteredKey = "UnregisteredNode";

            // Act & Assert
            Should.Throw<KeyNotFoundException>(() =>
            {
                _registry.GetNodeType(unregisteredKey);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")] // Consistent with registration check
        public void GetNodeType_WithNullOrEmptyKey_ShouldThrowArgumentException(string invalidKey)
        {
            // Arrange - No setup needed, registry state doesn't matter

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
            {
                _registry.GetNodeType(invalidKey);
            });
        }

        // Add tests for TryRegisterNodeType, TryGetNodeType, and GetAllRegisteredKeys if implemented
        // Example:
        /*
        [Fact]
        public void GetAllRegisteredKeys_WhenRegistryHasNodes_ShouldReturnAllKeys()
        {
            // Arrange
            var key1 = "Node1";
            var key2 = "Node2";
            _registry.RegisterNodeType(key1, typeof(SimpleTestNode));
            _registry.RegisterNodeType(key2, typeof(ConditionalTestNode));

            // Act
            var keys = _registry.GetAllRegisteredKeys();

            // Assert
            keys.ShouldNotBeNull();
            keys.Count().ShouldBe(2);
            keys.ShouldContain(key1);
            keys.ShouldContain(key2);
        }

        [Fact]
        public void GetAllRegisteredKeys_WhenRegistryIsEmpty_ShouldReturnEmptyCollection()
        {
            // Arrange - Registry is empty by default

            // Act
            var keys = _registry.GetAllRegisteredKeys();

            // Assert
            keys.ShouldNotBeNull();
            keys.ShouldBeEmpty();
        }
        */
    }
} 
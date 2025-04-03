using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nodify.Workflow.Core.Interfaces;

namespace Nodify.Workflow.Core.Registry;

/// <summary>
/// Default implementation of the node registry using reflection.
/// </summary>
public class DefaultNodeRegistry : INodeRegistry
{
    private readonly Dictionary<Type, NodeTypeMetadata> _registeredTypes = new();
    private readonly Dictionary<string, Type> _displayNameMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultNodeRegistry"/> class,
    /// scanning the specified assemblies for nodes marked with <see cref="WorkflowNodeAttribute"/>.
    /// </summary>
    /// <param name="assembliesToScan">The assemblies to scan for node types.</param>
    public DefaultNodeRegistry(IEnumerable<Assembly> assembliesToScan)
    {
        if (assembliesToScan == null) throw new ArgumentNullException(nameof(assembliesToScan));
        ScanAssemblies(assembliesToScan);
    }

    private void ScanAssemblies(IEnumerable<Assembly> assemblies)
    {
        _registeredTypes.Clear();
        _displayNameMap.Clear();
        var nodeInterfaceType = typeof(INode);

        foreach (var assembly in assemblies)
        {
            try
            {
                var potentialNodeTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && nodeInterfaceType.IsAssignableFrom(t));

                foreach (var type in potentialNodeTypes)
                {
                    var attribute = type.GetCustomAttribute<WorkflowNodeAttribute>();
                    if (attribute != null)
                    {
                        var metadata = new NodeTypeMetadata(type, attribute.DisplayName, attribute.Category, attribute.Description);
                        bool typeAdded = false;

                        if (!_registeredTypes.ContainsKey(type))
                        {
                            _registeredTypes.Add(type, metadata);
                            typeAdded = true;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Node type {type.FullName} already registered. Skipping duplicate.");
                        }

                        if (typeAdded)
                        {
                            if (!_displayNameMap.ContainsKey(metadata.DisplayName))
                            {
                                _displayNameMap.Add(metadata.DisplayName, type);
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Display name '{metadata.DisplayName}' from type {type.FullName} is already used by type {_displayNameMap[metadata.DisplayName].FullName}. Overwriting mapping, but this may cause issues.");
                                _displayNameMap[metadata.DisplayName] = type;
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {   
                 Console.WriteLine($"Warning: Could not load types from assembly {assembly.FullName}. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Warning: An unexpected error occurred while scanning assembly {assembly.FullName}. Error: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<NodeTypeMetadata> GetAvailableNodeTypes()
    {
        return _registeredTypes.Values;
    }

    /// <inheritdoc />
    public INode CreateNodeInstance(Type nodeType)
    {
        if (!_registeredTypes.ContainsKey(nodeType))
        {
            throw new ArgumentException($"Node type {nodeType.FullName} is not registered or discoverable.", nameof(nodeType));
        }

        try
        {
            object? instance = Activator.CreateInstance(nodeType);
            if (instance is INode nodeInstance)
            {
                return nodeInstance;
            }
            // This should theoretically not happen if IsAssignableFrom(INode) check passed during scan
            throw new InvalidOperationException($"Created instance of {nodeType.FullName} does not implement INode.");
        }
        catch (MissingMethodException ex) // Catches issues with finding parameterless constructor
        {
             throw new ArgumentException($"Failed to create node instance for {nodeType.FullName}. Ensure it has a parameterless constructor.", nameof(nodeType), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while creating node instance for {nodeType.FullName}.", ex);
        }
    }

    /// <inheritdoc />
    public INode CreateNodeInstance(string displayName)
    {
        if (_displayNameMap.TryGetValue(displayName, out Type? nodeType))
        {
             // Let CreateNodeInstance(Type) handle constructor/registration checks
             return CreateNodeInstance(nodeType);
        }
        else
        {
            throw new ArgumentException($"No node type registered with display name '{displayName}'.", nameof(displayName));
        }
    }
} 
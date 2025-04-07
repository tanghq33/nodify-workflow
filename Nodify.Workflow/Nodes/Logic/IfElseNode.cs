using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution;
using Nodify.Workflow.Core.Execution.Context;
using Nodify.Workflow.Core.Interfaces;
using Nodify.Workflow.Core.Logic; // For ConditionRuleBase, PropertyPathResolver
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Registry;
using System.Diagnostics;

namespace Nodify.Workflow.Nodes.Logic; // Changed namespace to Logic

[WorkflowNode("If/Else", "Logic", Description = "Executes different branches based on conditions.")]
public class IfElseNode : Node
{
    // Configurable Properties
    public string VariableName { get; set; } = string.Empty;
    public List<ConditionRuleBase> Conditions { get; set; } = new List<ConditionRuleBase>();
    
    /// <summary>
    /// Determines how the conditions in the list are combined (AND or OR).
    /// Defaults to AND.
    /// </summary>
    public ConditionCombinationLogic ConditionLogic { get; set; } = ConditionCombinationLogic.And;

    // Connector IDs - made internal for testing
    internal readonly Guid _flowInputId;
    internal readonly Guid _trueOutputId;
    internal readonly Guid _falseOutputId;

    public IfElseNode()
    {
        // Input
        var flowIn = new Connector(this, ConnectorDirection.Input, typeof(object));
        _flowInputId = flowIn.Id;
        AddInputConnector(flowIn);

        // Outputs - Add true output first to match test expectations
        var trueOut = new Connector(this, ConnectorDirection.Output, typeof(object));
        _trueOutputId = trueOut.Id;
        AddOutputConnector(trueOut);

        var falseOut = new Connector(this, ConnectorDirection.Output, typeof(object));
        _falseOutputId = falseOut.Id;
        AddOutputConnector(falseOut);
        Debug.WriteLine($"[IfElseNode Constructor] True Output ID: {_trueOutputId}, False Output ID: {_falseOutputId}"); // DEBUG
    }

    public override Task<NodeExecutionResult> ExecuteAsync(IExecutionContext context, object? inputData, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"[IfElseNode ExecuteAsync Start] Variable: {VariableName}"); // DEBUG
        // InputData is ignored by IfElseNode as it primarily controls flow based on context variables.

        // 1. Validate Input Variable Name
        if (string.IsNullOrWhiteSpace(VariableName))
        {
            Debug.WriteLine("[IfElseNode ExecuteAsync] Error: InputVariableName is empty."); // DEBUG
            return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException("VariableName property cannot be empty.")));
        }

        // 2. Get Target Object from Context
        if (!context.TryGetVariable<object>(VariableName, out var targetObject))
        {
             Debug.WriteLine($"[IfElseNode ExecuteAsync] Error: Variable '{VariableName}' not found."); // DEBUG
             return Task.FromResult(NodeExecutionResult.Failed(new KeyNotFoundException($"Variable '{VariableName}' not found in the execution context.")));
        }
        Debug.WriteLine($"[IfElseNode ExecuteAsync] Input Value: {targetObject ?? "null"}"); // DEBUG
        
        // Handle null target object if necessary - depends on rules
        // NullConditionRule handles null target object implicitly. Others might need it.
        // For now, let path resolution handle nulls if PropertyPath is used.

        // 3. Evaluate Conditions based on ConditionLogic
        bool overallResult;
        if (Conditions == null || Conditions.Count == 0)
        {
            // Default behavior for empty conditions
            overallResult = (ConditionLogic == ConditionCombinationLogic.And); // AND logic defaults true, OR defaults false
            Debug.WriteLine($"[IfElseNode ExecuteAsync] No conditions. Logic: {ConditionLogic}, Result: {overallResult}"); // DEBUG
        }
        else
        {
            // Initialize based on logic
            overallResult = (ConditionLogic == ConditionCombinationLogic.And); 
            Debug.WriteLine($"[IfElseNode ExecuteAsync] Evaluating {Conditions.Count} condition(s) with {ConditionLogic} logic. Initial result: {overallResult}"); // DEBUG

            foreach (var rule in Conditions)
            {
                // Resolve property path for the current rule
                if (!PropertyPathResolver.TryResolvePath(targetObject, rule.PropertyPath, out var propertyValue, out string? resolveError))
                {
                    Debug.WriteLine($"[IfElseNode ExecuteAsync] Error resolving path '{rule.PropertyPath}': {resolveError}"); // DEBUG
                    return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException($"Error resolving property path '{rule.PropertyPath}': {resolveError}")));
                }

                // Evaluate the specific rule
                bool ruleResult;
                try
                {
                    ruleResult = rule.Evaluate(propertyValue);
                    Debug.WriteLine($"[IfElseNode ExecuteAsync] Rule '{rule.GetType().Name}' (Path: '{rule.PropertyPath}') evaluated '{propertyValue}' -> {ruleResult}"); // DEBUG
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"[IfElseNode ExecuteAsync] Error evaluating rule '{rule.GetType().Name}' for path '{rule.PropertyPath}': {ex.Message}"); // DEBUG
                    // Catch unexpected errors during rule evaluation
                     return Task.FromResult(NodeExecutionResult.Failed(new InvalidOperationException($"Error evaluating condition for path '{rule.PropertyPath}': {ex.Message}", ex)));
                }
                
                // Apply logic
                if (ConditionLogic == ConditionCombinationLogic.And)
                {
                    if (!ruleResult) // In AND, if any is false, overall is false
                    {
                        overallResult = false;
                        Debug.WriteLine("[IfElseNode ExecuteAsync] AND logic: Rule false, overall result set to false. Breaking loop."); // DEBUG
                        break; 
                    }
                    Debug.WriteLine("[IfElseNode ExecuteAsync] AND logic: Rule true, continuing."); // DEBUG
                    // If rule is true, continue checking (overallResult remains true initially)
                }
                else // OR Logic
                {
                    if (ruleResult) // In OR, if any is true, overall is true
                    {
                        overallResult = true;
                        Debug.WriteLine("[IfElseNode ExecuteAsync] OR logic: Rule true, overall result set to true. Breaking loop."); // DEBUG
                        break; 
                    }
                    Debug.WriteLine("[IfElseNode ExecuteAsync] OR logic: Rule false, continuing."); // DEBUG
                    // If rule is false, continue checking (overallResult remains false initially)
                }
            }
             Debug.WriteLine($"[IfElseNode ExecuteAsync] Final condition evaluation result: {overallResult}"); // DEBUG
        }

        // 4. Return result activating the correct branch (no output data)
        Guid connectorToActivate = overallResult ? _trueOutputId : _falseOutputId;
        Debug.WriteLine($"[IfElseNode ExecuteAsync End] Overall Result: {overallResult}. Activating Connector ID: {connectorToActivate} (True ID: {_trueOutputId}, False ID: {_falseOutputId})"); // DEBUG
        return Task.FromResult(NodeExecutionResult.Succeeded(connectorToActivate));
    }
} 
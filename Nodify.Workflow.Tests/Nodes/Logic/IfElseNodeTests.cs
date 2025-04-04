using Xunit;
using Shouldly;
using Nodify.Workflow.Nodes.Logic;
using Nodify.Workflow.Core.Registry;
using System.Linq;
using Nodify.Workflow.Core.Models;
using Nodify.Workflow.Core.Interfaces;
using System.Threading.Tasks;
using Nodify.Workflow.Core.Execution.Context;
using NSubstitute;
using System.Threading;
using System;
using Nodify.Workflow.Core.Execution;
using System.Collections.Generic;
using Nodify.Workflow.Core.Logic;
using Nodify.Workflow.Core.Logic.Operators;

namespace Nodify.Workflow.Tests.Nodes.Logic;

// Test data class
public class ConditionTestData
{
    public string Name { get; set; } = "Test";
    public int Count { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public InnerData? Inner { get; set; } = new InnerData();
    public List<string> Tags { get; set; } = new List<string> { "A", "B" };
    public string? NullString { get; set; } = null;
}

public class InnerData
{
    public double Value { get; set; } = 5.5;
}

public class IfElseNodeTests
{
    private IExecutionContext _context = null!;
    private ConditionTestData _testData = null!;
    private IfElseNode _node = null!;
    private Guid _trueOutId;
    private Guid _falseOutId;

    private void SetupNode(List<ConditionRuleBase> conditions)
    {
        _node = new IfElseNode
        {
            InputVariableName = "InputData",
            Conditions = conditions
        };
        _context = Substitute.For<IExecutionContext>();
        _testData = new ConditionTestData();
        
        // NSubstitute setup for TryGetVariable (refined)
        object? testDataObj = _testData;
        _context.TryGetVariable<object>(Arg.Is("InputData"), out Arg.Any<object?>()) 
                .Returns(callInfo => 
                { 
                    callInfo[1] = testDataObj; 
                    return true; 
                });
        _context.TryGetVariable<object>(Arg.Is<string>(s => s != "InputData"), out Arg.Any<object?>())
                .Returns(callInfo => 
                { 
                    callInfo[1] = null; 
                    return false; 
                });

        // Get the actual connector IDs from the created node instance
        _trueOutId = _node._trueOutputId; 
        _falseOutId = _node._falseOutputId;
    }

    [Fact]
    public void Constructor_ShouldCreateCorrectConnectors()
    {
        // Arrange & Act
        var node = new IfElseNode();

        // Assert
        node.InputConnectors.Count.ShouldBe(1);
        node.InputConnectors.First().DataType.ShouldBe(typeof(object));

        node.OutputConnectors.Count.ShouldBe(2);
        node.OutputConnectors.ShouldAllBe(c => c.DataType == typeof(object));
        node.OutputConnectors.ShouldAllBe(c => c.Direction == ConnectorDirection.Output);
        // Verify distinct IDs for output connectors
        node.OutputConnectors.Select(c => c.Id).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_TrueCondition_ShouldActivateTrueBranch()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Name", Operator = StringOperator.Equals, ComparisonValue = "Test" }
        });

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(_trueOutId);
    }

    [Fact]
    public async Task ExecuteAsync_FalseCondition_ShouldActivateFalseBranch()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new NumericConditionRule { PropertyPath = "Count", Operator = NumericOperator.Equals, ComparisonValue = 99 }
        });

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(_falseOutId);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleTrueConditions_ShouldActivateTrueBranch()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Name", Operator = StringOperator.Equals, ComparisonValue = "Test" },
            new NumericConditionRule { PropertyPath = "Count", Operator = NumericOperator.GreaterThan, ComparisonValue = 5 },
            new NullConditionRule { PropertyPath = "Inner", Operator = NullOperator.IsNotNull }
        });

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(_trueOutId);
    }

    [Fact]
    public async Task ExecuteAsync_OneFalseInMultipleConditions_ShouldActivateFalseBranch()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Name", Operator = StringOperator.Equals, ComparisonValue = "Test" },
            new NumericConditionRule { PropertyPath = "Count", Operator = NumericOperator.Equals, ComparisonValue = 99 }, // This is false
            new NullConditionRule { PropertyPath = "Inner", Operator = NullOperator.IsNotNull }
        });

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue(); // Node execution itself succeeded
        result.ActivatedOutputConnectorId.ShouldBe(_falseOutId);
    }
    
    [Theory]
    [InlineData(ConditionCombinationLogic.And, true)] // AND with no conditions is True
    [InlineData(ConditionCombinationLogic.Or, false)] // OR with no conditions is False
    public async Task ExecuteAsync_NoConditions_ShouldActivateCorrectBranch(ConditionCombinationLogic logic, bool expectedResultBranch) // true=True, false=False
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>()); 
        _node.ConditionLogic = logic;

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(expectedResultBranch ? _trueOutId : _falseOutId);
    }

    [Theory]
    [InlineData(ConditionCombinationLogic.And, true)] // AND with null conditions is True
    [InlineData(ConditionCombinationLogic.Or, false)] // OR with null conditions is False
    public async Task ExecuteAsync_NullConditionsList_ShouldActivateCorrectBranch(ConditionCombinationLogic logic, bool expectedResultBranch)
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>()); 
        _node.Conditions = null!;
        _node.ConditionLogic = logic;

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(expectedResultBranch ? _trueOutId : _falseOutId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInputVariableName_ShouldFail()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>());
        _node.InputVariableName = " "; // Empty

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBeOfType<InvalidOperationException>();
        result.Error?.Message.ShouldContain("InputVariableName");
    }

    [Fact]
    public async Task ExecuteAsync_InputVariableNotFound_ShouldFail()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>());
        _node.InputVariableName = "WrongName";
        object? outVar = null;
        // Make TryGetVariable return false for the wrong name
        _context.TryGetVariable<object>("WrongName", out outVar).Returns(false);

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBeOfType<KeyNotFoundException>();
        result.Error?.Message.ShouldContain("WrongName");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPropertyPath_ShouldFail()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Inner.InvalidProp", Operator = StringOperator.Equals, ComparisonValue = "Test" }
        });

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBeOfType<InvalidOperationException>();
        result.Error?.Message.ShouldContain("Inner.InvalidProp");
        result.Error?.Message.ShouldContain("Error resolving property path");
    }

     [Fact]
    public async Task ExecuteAsync_RuleEvaluationError_ShouldFail()
    {
        // Arrange
        // Use a rule that might cause an error if propertyValue is wrong type during Evaluate
         SetupNode(new List<ConditionRuleBase>
        {
            // Although NumericCondition handles type errors, let's simulate a rule throwing
            new FailingConditionRule { PropertyPath = "Name" } 
        });

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBeOfType<InvalidOperationException>(); // Outer exception from IfElseNode
        result.Error?.Message.ShouldContain("Error evaluating condition");
        result.Error?.InnerException.ShouldBeOfType<NotImplementedException>(); // Inner exception from rule
    }

    // Helper rule that throws during evaluation for testing purposes
    private class FailingConditionRule : ConditionRuleBase
    {
        public override bool Evaluate(object? propertyValue)
        {
            throw new NotImplementedException("Simulated evaluation error");
        }
    }

    // --- OR Logic Tests (New) ---
    [Fact]
    public async Task ExecuteAsync_OrLogic_OneTrueCondition_ShouldActivateTrueBranch()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Name", Operator = StringOperator.Equals, ComparisonValue = "Wrong" }, // False
            new NumericConditionRule { PropertyPath = "Count", Operator = NumericOperator.Equals, ComparisonValue = 10 }, // True
            new NullConditionRule { PropertyPath = "Inner", Operator = NullOperator.IsNull } // False
        });
        _node.ConditionLogic = ConditionCombinationLogic.Or;

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(_trueOutId);
    }

    [Fact]
    public async Task ExecuteAsync_OrLogic_MultipleTrueConditions_ShouldActivateTrueBranch()
    {
         // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Name", Operator = StringOperator.Equals, ComparisonValue = "Test" }, // True
            new NumericConditionRule { PropertyPath = "Count", Operator = NumericOperator.Equals, ComparisonValue = 10 }, // True
            new NullConditionRule { PropertyPath = "Inner", Operator = NullOperator.IsNull } // False
        });
        _node.ConditionLogic = ConditionCombinationLogic.Or;

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(_trueOutId);
    }

    [Fact]
    public async Task ExecuteAsync_OrLogic_AllFalseConditions_ShouldActivateFalseBranch()
    {
        // Arrange
        SetupNode(new List<ConditionRuleBase>
        {
            new StringConditionRule { PropertyPath = "Name", Operator = StringOperator.Equals, ComparisonValue = "Wrong" }, // False
            new NumericConditionRule { PropertyPath = "Count", Operator = NumericOperator.Equals, ComparisonValue = 99 }, // False
            new NullConditionRule { PropertyPath = "Inner", Operator = NullOperator.IsNull } // False
        });
        _node.ConditionLogic = ConditionCombinationLogic.Or;

        // Act
        var result = await _node.ExecuteAsync(_context, null, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActivatedOutputConnectorId.ShouldBe(_falseOutId);
    }
} 
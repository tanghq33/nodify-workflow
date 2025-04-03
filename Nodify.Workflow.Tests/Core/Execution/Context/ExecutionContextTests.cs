using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Nodify.Workflow.Core.Execution.Context; // Updated using directive

namespace Nodify.Workflow.Tests.Core.Execution.Context // Updated namespace
{
    public class ExecutionContextTests
    {
        private IExecutionContext CreateContext() => new ExecutionContext();

        // === Variable Management Tests ===

        [Fact]
        public void SetVariable_ShouldStoreVariable()
        {
            var context = CreateContext();
            var key = "testVar";
            var value = 123;
            context.SetVariable(key, value);
            Assert.Equal(value, context.GetVariable(key));
        }

        [Fact]
        public void GetVariable_ExistingVariable_ShouldReturnValue()
        {
            var context = CreateContext();
            var key = "myVar";
            var value = "hello";
            context.SetVariable(key, value);
            var retrievedValue = context.GetVariable(key);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public void GetVariable_NonExistentVariable_ShouldReturnNull()
        {
            var context = CreateContext();
            var key = "nonExistent";
            var retrievedValue = context.GetVariable(key);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void SetVariable_UpdateExistingVariable_ShouldOverwrite()
        {
            var context = CreateContext();
            var key = "counter";
            context.SetVariable(key, 1);
            context.SetVariable(key, 2);
            Assert.Equal(2, context.GetVariable(key));
        }

        [Fact]
        public void SetVariable_DifferentTypes_ShouldStoreCorrectly()
        {
            var context = CreateContext();
            context.SetVariable("myString", "test");
            context.SetVariable("myInt", 42);
            context.SetVariable("myBool", true);
            context.SetVariable("myNull", null);
            Assert.Equal("test", context.GetVariable("myString"));
            Assert.Equal(42, context.GetVariable("myInt"));
            Assert.True((bool?)context.GetVariable("myBool"));
            Assert.Null(context.GetVariable("myNull"));
        }

        [Fact]
        public void GetVariable_CaseInsensitive_ShouldReturnCorrectValue()
        {
             var context = CreateContext();
             var keyUpper = "MyCaseVar";
             var keyLower = "mycasevar";
             var value = "Case Test";
             context.SetVariable(keyUpper, value);
             Assert.Equal(value, context.GetVariable(keyLower));
             Assert.Equal(value, context.GetVariable(keyUpper));
        }

         [Fact]
        public void SetVariable_NullOrEmptyKey_ShouldThrowArgumentException()
        {
             var context = CreateContext();
             Assert.Throws<ArgumentException>(() => context.SetVariable(null!, "value"));
             Assert.Throws<ArgumentException>(() => context.SetVariable("", "value"));
             Assert.Throws<ArgumentException>(() => context.SetVariable("   ", "value"));
        }

        [Fact]
        public void GetVariable_NullOrEmptyKey_ShouldThrowArgumentException()
        {
             var context = CreateContext();
             Assert.Throws<ArgumentException>(() => context.GetVariable(null!));
             Assert.Throws<ArgumentException>(() => context.GetVariable(""));
             Assert.Throws<ArgumentException>(() => context.GetVariable("   "));
        }

         [Fact]
        public void TryGetVariable_ExistingValueCorrectType_ShouldReturnTrueAndValue()
        {
            var context = CreateContext();
            context.SetVariable("myInt", 123);
            bool result = context.TryGetVariable<int>("myInt", out var value);
            Assert.True(result);
            Assert.Equal(123, value);
        }

        [Fact]
        public void TryGetVariable_ExistingValueIncorrectType_ShouldReturnFalse()
        {
            var context = CreateContext();
            context.SetVariable("myString", "hello");
            bool result = context.TryGetVariable<int>("myString", out var value);
            Assert.False(result);
            Assert.Equal(default(int), value);
        }

        [Fact]
        public void TryGetVariable_NonExistentKey_ShouldReturnFalse()
        {
            var context = CreateContext();
            bool result = context.TryGetVariable<string>("nonExistent", out var value);
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryGetVariable_ExistingNullValueNullableType_ShouldReturnTrueAndNull()
        {
            var context = CreateContext();
            context.SetVariable("myNullVar", null);
            bool result = context.TryGetVariable<string?>("myNullVar", out var value);
            Assert.True(result);
            Assert.Null(value);
        }

         [Fact]
        public void TryGetVariable_ExistingNullValueValueType_ShouldReturnFalse()
        {
            var context = CreateContext();
            context.SetVariable("myNullVar", null);
            bool result = context.TryGetVariable<int>("myNullVar", out var value);
            Assert.False(result);
            Assert.Equal(0, value);
        }

        // === Execution Status Tests ===

        [Fact]
        public void InitialStatus_ShouldBeNotStarted()
        {
            var context = CreateContext();
            Assert.Equal(ExecutionStatus.NotStarted, context.CurrentStatus);
        }

        [Fact]
        public void SetStatus_ShouldUpdateCurrentStatus()
        {
            var context = CreateContext();
            var newStatus = ExecutionStatus.Running;
            context.SetStatus(newStatus);
            Assert.Equal(newStatus, context.CurrentStatus);
        }

        // === Execution Log Tests ===

        [Fact]
        public void AddLog_ShouldStoreLogEntry()
        {
            var context = CreateContext();
            var message = "Node executed successfully.";
            context.AddLog(message);
            var logs = context.GetLogs();
            Assert.Contains(message, logs);
            Assert.Single(logs);
        }

        [Fact]
        public void AddLog_NullMessage_ShouldStoreEmptyString()
        {
             var context = CreateContext();
             context.AddLog(null!);
             var logs = context.GetLogs();
             Assert.Contains(string.Empty, logs);
             Assert.Single(logs);
        }

        [Fact]
        public void GetLogs_MultipleEntries_ShouldReturnAllInOrder()
        {
            var context = CreateContext();
            context.AddLog("Log 1");
            context.AddLog("Log 2");
            var logs = context.GetLogs().ToList();
            Assert.Equal(2, logs.Count);
            Assert.Equal("Log 1", logs[0]);
            Assert.Equal("Log 2", logs[1]);
        }

         [Fact]
        public void GetLogs_NoEntries_ShouldReturnEmpty()
        {
             var context = CreateContext();
             var logs = context.GetLogs();
             Assert.Empty(logs);
        }

        // === Conditional Evaluation Support Tests ===

        [Fact]
        public void EvaluateCondition_BooleanVariableTrue_ShouldReturnTrue()
        {
            var context = CreateContext();
            context.SetVariable("isEnabled", true);
            Assert.True(context.EvaluateCondition("isEnabled"));
        }

         [Fact]
        public void EvaluateCondition_BooleanVariableFalse_ShouldReturnFalse()
        {
            var context = CreateContext();
            context.SetVariable("isDisabled", false);
            Assert.False(context.EvaluateCondition("isDisabled"));
        }

         [Fact]
        public void EvaluateCondition_NonBooleanVariable_ShouldReturnFalse()
        {
            var context = CreateContext();
            context.SetVariable("someNumber", 123);
            Assert.False(context.EvaluateCondition("someNumber"));
        }

         [Fact]
        public void EvaluateCondition_NonExistentVariable_ShouldReturnFalse()
        {
            var context = CreateContext();
            Assert.False(context.EvaluateCondition("doesNotExist"));
        }

        // === Context Association Tests ===

        [Fact]
        public void SetCurrentNode_ShouldUpdateCurrentNodeId()
        {
             var context = CreateContext();
             var nodeId = Guid.NewGuid();
             context.SetCurrentNode(nodeId);
             Assert.Equal(nodeId, context.CurrentNodeId);
        }

         [Fact]
        public void ClearCurrentNode_ShouldSetNodeIdToNull()
        {
             var context = CreateContext();
             var nodeId = Guid.NewGuid();
             context.SetCurrentNode(nodeId);
             Assert.NotNull(context.CurrentNodeId);
             context.ClearCurrentNode();
             Assert.Null(context.CurrentNodeId);
        }

         [Fact]
        public void InitialCurrentNodeId_ShouldBeNull()
        {
            var context = CreateContext();
            Assert.Null(context.CurrentNodeId);
        }
    }
} 
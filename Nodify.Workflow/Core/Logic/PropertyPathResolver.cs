using System;
using System.Reflection;
using System.Diagnostics;

namespace Nodify.Workflow.Core.Logic;

/// <summary>
/// Utility class for resolving property values using dot-notation paths.
/// </summary>
public static class PropertyPathResolver
{
    /// <summary>
    /// Resolves the value of a property path against a target object.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="path">The dot-notation property path (e.g., "User.Address.City").</param>
    /// <param name="value">The resolved value if the path is valid and resolution is successful.</param>
    /// <param name="errorMessage">An error message if resolution fails.</param>
    /// <returns>True if the value was resolved successfully, false otherwise.</returns>
    public static bool TryResolvePath(object? target, string path, out object? value, out string? errorMessage)
    {
        value = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Property path cannot be empty.";
            return false;
        }

        if (target == null)
        {
            errorMessage = "Target object is null.";
            return false;
        }

        string[] parts = path.Split('.');
        object? currentObject = target;

        foreach (string part in parts)
        {
            if (currentObject == null)
            {
                errorMessage = $"Cannot resolve path part '{part}' because the intermediate object is null.";
                return false;
            }

            Type currentType = currentObject.GetType();
            PropertyInfo? propertyInfo = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propertyInfo == null)
            {
                errorMessage = $"Property '{part}' not found on type '{currentType.Name}'.";
                return false;
            }

            try
            {
                currentObject = propertyInfo.GetValue(currentObject);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error accessing property '{part}' on type '{currentType.Name}': {ex.Message}";
                Debug.WriteLine($"PropertyPathResolver Error: {errorMessage}");
                return false;
            }
        }

        // Successfully resolved the entire path
        value = currentObject;
        return true;
    }
} 
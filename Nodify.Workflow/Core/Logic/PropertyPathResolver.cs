using System;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json; // Added for JsonElement

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
            value = target;
            return true;
        }

        if (target == null)
        {
            errorMessage = "Target object is null.";
            return false;
        }

        string[] parts = path.Split('.');
        object? currentObject = target;

        if (currentObject is JsonElement jsonElementTarget)
        {
            return TryResolvePathForJsonElement(jsonElementTarget, parts, out value, out errorMessage);
        }

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

        value = currentObject;
        return true;
    }

    private static bool TryResolvePathForJsonElement(JsonElement targetElement, string[] pathParts, out object? value, out string? errorMessage)
    {
        value = null;
        errorMessage = null;
        JsonElement currentElement = targetElement;

        foreach (string part in pathParts)
        {
            if (currentElement.ValueKind != JsonValueKind.Object)
            {
                errorMessage = $"Cannot resolve path part '{part}' because the current JSON element is not an object (found {currentElement.ValueKind}).";
                return false;
            }

            if (!currentElement.TryGetProperty(part, out JsonElement nextElement))
            {
                bool foundCaseInsensitive = false;
                foreach(var property in currentElement.EnumerateObject())
                {
                    if (property.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    {
                        nextElement = property.Value;
                        foundCaseInsensitive = true;
                        break;
                    }
                }
                if (!foundCaseInsensitive)
                {
                    errorMessage = $"JSON property '{part}' not found in the current object.";
                    return false;
                }
            }
            currentElement = nextElement;
        }

        value = ConvertJsonElement(currentElement);
        return true;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.Clone(),
            JsonValueKind.Array => element.Clone(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDouble(out double doubleVal) ? doubleVal : (object)element.GetInt64(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }
}
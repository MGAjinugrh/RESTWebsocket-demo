using System.ComponentModel;
using System.Reflection;

namespace Common;

public static class Helpers
{
    /// <summary>
    /// Reflection method to get specific string value by uint input from a list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="intVal"></param>
    /// <param name="source"></param>
    /// <param name="intValPropName"></param>
    /// <param name="valuePropertyName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static string GetStringValByUInt<T>(uint? intVal, IEnumerable<T> source, 
        string intValPropName = "Id", string valuePropertyName = "Username", string defaultValue = "-")
    {
        if (intVal == null || intVal == 0 || source == null) 
            return defaultValue;

        var type = typeof(T);
        var idProp = type.GetProperty(intValPropName);
        var valueProp = type.GetProperty(valuePropertyName);
        if (idProp == null || valueProp == null)
            return defaultValue;

        foreach (var item in source)
        {
            var itemId = idProp.GetValue(item);
            if (itemId is uint uintId && uintId == intVal)
                return valueProp.GetValue(item)?.ToString() ?? defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Get the display name set on Enumeration Type
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDisplayName(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attr = field.GetCustomAttribute<DisplayNameAttribute>();
        return attr?.DisplayName ?? value.ToString();
    }
}

using System.Reflection;

namespace Mtf.Games.Tests.Fakes;

internal static class Reflection
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static T GetField<T>(object target, string name)
    {
        var field = FindField(target.GetType(), name);
        return (T)field.GetValue(target)!;
    }

    public static void SetField(object target, string name, object? value)
    {
        var field = FindField(target.GetType(), name);
        field.SetValue(target, value);
    }

    private static FieldInfo FindField(Type? type, string name)
    {
        while (type != null)
        {
            var field = type.GetField(name, Flags);
            if (field != null)
            {
                return field;
            }
            type = type.BaseType;
        }
        throw new MissingFieldException(name);
    }
}

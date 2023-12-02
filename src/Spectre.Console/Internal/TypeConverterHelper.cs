namespace Spectre.Console;

internal static class TypeConverterHelper
{
    internal static bool AreGenericTypeConvertersSupported =>
        !AppContext.TryGetSwitch("Spectre.Console.TypeConverterHelper.AreGenericTypeConvertersSupported", out var enabled) || enabled;

    public static string ConvertToString<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T input)
    {
        var result = GetTypeConverter<T>().ConvertToInvariantString(input);
        if (result == null)
        {
            throw new InvalidOperationException("Could not convert input to a string");
        }

        return result;
    }

    public static bool TryConvertFromString<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string input, [MaybeNull] out T? result)
    {
        try
        {
            result = (T?)GetTypeConverter<T>().ConvertFromInvariantString(input);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static bool TryConvertFromStringWithCulture<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string input, CultureInfo? info, [MaybeNull] out T? result)
    {
        try
        {
            if (info == null)
            {
                return TryConvertFromString<T>(input, out result);
            }
            else
            {
                result = (T?)GetTypeConverter<T>().ConvertFromString(null!, info, input);
            }

            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static TypeConverter GetTypeConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        var converter = GetCheckedConverter();
        if (converter != null)
        {
            return converter;
        }

        var attribute = typeof(T).GetCustomAttribute<TypeConverterAttribute>();
        if (attribute != null)
        {
            var type = Type.GetType(attribute.ConverterTypeName, false, false);
            if (type != null)
            {
                converter = Activator.CreateInstance(type) as TypeConverter;
                if (converter != null)
                {
                    return converter;
                }
            }
        }

        throw new InvalidOperationException("Could not find type converter");

        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "TypeConverter is only used for non-generic types.")]
        static TypeConverter? GetCheckedConverter()
        {
            if (!AreGenericTypeConvertersSupported && typeof(T).IsGenericType)
            {
                throw new InvalidOperationException("Generic type converters are not supported when trimming is enabled.");
            }

            var converter = TypeDescriptor.GetConverter(typeof(T));
            return converter;
        }
    }
}
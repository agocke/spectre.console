using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;

namespace Spectre.Console;

internal static class SafeTypeConverter
{
    internal const DynamicallyAccessedMemberTypes ConverterAnnotation = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields;

    public static bool TryConvertFromString<[DAM(ConverterAnnotation)] T>(string input, [MaybeNull] out T? result)
    {
        try
        {
            result = (T?)GetConverter<T>().ConvertFromInvariantString(input);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static bool TryConvertFromStringWithCulture<[DAM(ConverterAnnotation)] T>(string input, CultureInfo? info, [MaybeNull] out T? result)
    {
        try
        {
            if (info == null)
            {
                return TryConvertFromString<T>(input, out result);
            }
            else
            {
                result = (T?)GetConverter<T>().ConvertFromString(null!, info, input);
            }

            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static TypeConverter GetConverter<[DAM(ConverterAnnotation)] T>() => GetConverter(typeof(T));

    public static TypeConverter GetConverter([DAM(ConverterAnnotation)] Type converterType)
    {
        var converter = GetIntrinsicConverter(converterType);
        if (converter != null)
        {
            return converter;
        }

        var attribute = converterType.GetCustomAttribute<TypeConverterAttribute>();
        if (attribute != null)
        {
            var attrType = Type.GetType(attribute.ConverterTypeName, false, false);
            if (attrType != null)
            {
                converter = Activator.CreateInstance(attrType) as TypeConverter;
                if (converter != null)
                {
                    return converter;
                }
            }
        }

        throw new InvalidOperationException("Could not find type converter");
    }

    private delegate TypeConverter FuncWithDam([DAM(ConverterAnnotation)] Type type);

    private static readonly Dictionary<Type, FuncWithDam> _intrinsicConverters;

    static SafeTypeConverter()
    {
        _intrinsicConverters = new()
        {
            [typeof(bool)] = _ => new BooleanConverter(),
            [typeof(byte)] = _ => new ByteConverter(),
            [typeof(sbyte)] = _ => new SByteConverter(),
            [typeof(char)] = _ => new CharConverter(),
            [typeof(double)] = _ => new DoubleConverter(),
            [typeof(string)] = _ => new StringConverter(),
            [typeof(int)] = _ => new Int32Converter(),
            [typeof(short)] = _ => new Int16Converter(),
            [typeof(long)] = _ => new Int64Converter(),
            [typeof(float)] = _ => new SingleConverter(),
            [typeof(ushort)] = _ => new UInt16Converter(),
            [typeof(uint)] = _ => new UInt32Converter(),
            [typeof(ulong)] = _ => new UInt64Converter(),
            [typeof(object)] = _ => new TypeConverter(),
            [typeof(CultureInfo)] = _ => new CultureInfoConverter(),
            [typeof(DateTime)] = _ => new DateTimeConverter(),
            [typeof(DateTimeOffset)] = _ => new DateTimeOffsetConverter(),
            [typeof(decimal)] = _ => new DecimalConverter(),
            [typeof(TimeSpan)] = _ => new TimeSpanConverter(),
            [typeof(Guid)] = _ => new GuidConverter(),
            [typeof(Uri)] = _ => new UriTypeConverter(),

            [typeof(Array)] = _ => new ArrayConverter(),
            [typeof(ICollection)] = _ => new CollectionConverter(),
            [typeof(Enum)] = CreateEnumConverter(),
        };
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111", Justification = "Delegate reflection is safe for all usages in this type.")]
    private static FuncWithDam CreateEnumConverter() => ([DAM(ConverterAnnotation)] Type type) => new EnumConverter(type);

    /// <summary>
    /// A highly-constrained version of <see cref="TypeDescriptor.GetConverter(Type)" /> that only returns intrinsic converters.
    /// </summary>
    private static TypeConverter? GetIntrinsicConverter([DAM(ConverterAnnotation)] Type type)
    {
        if (type.IsArray)
        {
            type = typeof(Array);
        }

        if (typeof(ICollection).IsAssignableFrom(type))
        {
            type = typeof(ICollection);
        }

        if (type.IsEnum)
        {
            type = typeof(Enum);
        }

        if (_intrinsicConverters.TryGetValue(type, out var factory))
        {
            return factory(type);
        }

        return null;
    }
}

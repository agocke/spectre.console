namespace Spectre.Console.Cli;

internal sealed class TrimmableTypeResolver : IDisposable, ITypeResolver
{
    public ComponentRegistry Registry { get; }

    public TrimmableTypeResolver()
        : this(null)
    {
    }

    public TrimmableTypeResolver(ComponentRegistry? registry)
    {
        Registry = registry ?? new ComponentRegistry();
    }

    public void Dispose()
    {
        Registry.Dispose();
    }

    public object? Resolve([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? type)
    {
        if (type == null)
        {
            return null;
        }

        var isEnumerable = false;
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                isEnumerable = true;
                type = type.GenericTypeArguments[0];
            }
        }

        var registrations = Registry.GetRegistrations(type);
        if (registrations != null)
        {
            if (isEnumerable)
            {
                var result = Array.CreateInstance(type, registrations.Count);
                for (var index = 0; index < registrations.Count; index++)
                {
                    var registration = registrations.ElementAt(index);
                    result.SetValue(Resolve(registration), index);
                }

                return result;
            }
        }

        return Resolve(registrations?.LastOrDefault());
    }

    public object? Resolve(ComponentRegistration? registration)
    {
        return registration?.Activator?.Activate(this);
    }
}
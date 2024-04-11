namespace Spectre.Console.Cli;

internal static class CommandPropertyBinder
{
    [RequiresUnreferencedCode("Complex type parsing")]
    [RequiresDynamicCode("Creates new arrays")]
    public static CommandSettings CreateSettings(CommandValueLookup lookup, Type settingsType, ITypeResolver resolver)
    {
        var settings = CreateSettings(resolver, settingsType);

        foreach (var (parameter, value) in lookup)
        {
            if (value != default)
            {
                parameter.Property.SetValue(settings, value);
            }
        }

        // Validate the settings.
        var validationResult = settings.Validate();
        if (!validationResult.Successful)
        {
            throw CommandRuntimeException.ValidationFailed(validationResult);
        }

        return settings;
    }

    [RequiresUnreferencedCode("Complex type parsing")]
    [RequiresDynamicCode("Creates new arrays")]
    private static CommandSettings CreateSettings(ITypeResolver resolver, Type settingsType)
    {
        if (resolver.Resolve(settingsType) is CommandSettings settings)
        {
            return settings;
        }

        if (Activator.CreateInstance(settingsType) is CommandSettings instance)
        {
            return instance;
        }

        throw CommandParseException.CouldNotCreateSettings(settingsType);
    }
}
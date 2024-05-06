namespace Spectre.Console.Cli;

public interface IGeneratedCommandBinder
{
    void BindUnmapped(IEnumerable<CommandParameter> unmapped);
    void BindMapped(IEnumerable<(CommandParameter Parameter, string? Value)> mapped);
    CommandSettings BuildSettings();
}

internal static class GeneratedCommandBinder
{
    public static CommandSettings Bind(CommandTree? tree, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type settingsType, ITypeResolver resolver)
    {
        var lookup = GeneratedCommandValueResolver.GetParameterValues(tree, resolver);

        // Got a constructor with at least one name corresponding to a settings?
        foreach (var constructor in settingsType.GetConstructors())
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length > 0)
            {
                foreach (var parameter in parameters)
                {
                    if (lookup.HasParameterWithName(parameter?.Name))
                    {
                        // Use constructor injection.
                        return CommandConstructorBinder.CreateSettings(lookup, constructor, resolver);
                    }
                }
            }
        }

        return CommandPropertyBinder.CreateSettings(lookup, settingsType, resolver);
    }
}
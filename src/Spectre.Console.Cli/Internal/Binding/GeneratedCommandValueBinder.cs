namespace Spectre.Console.Cli;

internal sealed class GeneratedCommandValueBinder
{
    private readonly CommandValueLookup _lookup;

    public GeneratedCommandValueBinder(CommandValueLookup lookup)
    {
        _lookup = lookup;
    }

    public void Bind(CommandParameter parameter, object? value)
    {
        if (parameter.ParameterKind == ParameterKind.Pair)
        {
            // value = GetLookup(parameter, resolver, value);
            throw new NotSupportedException("Pair parameters are not supported when trimming.");
        }
        else if (parameter.ParameterKind == ParameterKind.Vector)
        {
            // value = GetArray(parameter, value);
            throw new NotSupportedException("Vector parameters are not supported when trimming.");
        }
        else if (parameter.ParameterKind == ParameterKind.FlagWithValue)
        {
            value = GetFlag(parameter, value);
        }

        _lookup.SetValue(parameter, value);
    }

    private object GetFlag(CommandParameter parameter, object? value)
    {
        var flagValue = (IFlagValue?)_lookup.GetValue(parameter);
        if (flagValue == null)
        {
            flagValue = (IFlagValue?)Activator.CreateInstance(parameter.ParameterType);
            if (flagValue == null)
            {
                throw new InvalidOperationException("Could not create flag value.");
            }
        }

        if (value != null)
        {
            // Null means set, but not with a valid value.
            flagValue.Value = value;
        }

        // If the parameter was mapped, then it's set.
        flagValue.IsSet = true;

        return flagValue;
    }
}
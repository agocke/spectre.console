namespace Spectre.Console.Cli;

public static class CommandModelBuilder
{
    // Consider removing this in favor for value tuples at some point.
    private sealed class OrderedProperties
    {
        public int Level { get; }
        public int SortOrder { get; }
        public PropertyInfo[] Properties { get; }

        public OrderedProperties(int level, int sortOrder, PropertyInfo[] properties)
        {
            Level = level;
            SortOrder = sortOrder;
            Properties = properties;
        }
    }

    public static readonly OptionsAndArgs EmptyOptionsAndArgs = Array.Empty<(IEnumerable<CommandOption> Options, IEnumerable<CommandArgument> Args)>();

    [RequiresUnreferencedCode("Building commands requires complex reflection.")]
    internal static CommandModel Build(IConfiguration configuration)
    {
        var result = new List<CommandInfo>();
        foreach (var command in configuration.Commands)
        {
            result.Add(Build(null, command));
        }

        if (configuration.DefaultCommand != null)
        {
            // Add the examples from the configuration to the default command.
            configuration.DefaultCommand.Examples.AddRange(configuration.Examples);

            // Build the default command.
            var defaultCommand = Build(null, configuration.DefaultCommand);

            result.Add(defaultCommand);
        }

        // Create the command model and validate it.
        var model = new CommandModel(configuration.Settings, result, configuration.Examples);
        CommandModelValidator.Validate(model, configuration.Settings);

        return model;
    }

    internal static CommandModel BuildSafe(IConfiguration configuration)
    {
        var result = new List<CommandInfo>();
        foreach (var command in configuration.Commands)
        {
            result.Add(BuildSafe(null, command));
        }

        if (configuration.DefaultCommand != null)
        {
            // Add the examples from the configuration to the default command.
            configuration.DefaultCommand.Examples.AddRange(configuration.Examples);

            // Build the default command.
            var defaultCommand = BuildSafe(null, configuration.DefaultCommand);

            result.Add(defaultCommand);
        }

        // Create the command model and validate it.
        var model = new CommandModel(configuration.Settings, result, configuration.Examples);
        CommandModelValidator.Validate(model, configuration.Settings);

        return model;
    }

    [RequiresUnreferencedCode("Calls Spectre.Console.Cli.CommandModelBuilder.GetOptionsAndArgs(CommandInfo)")]
    private static CommandInfo Build(CommandInfo? parent, ConfiguredCommand command)
    {
        var info = new CommandInfo(parent, command);
        var optionsAndArgs = command.OptionsAndArgs;
        optionsAndArgs ??= GetOptionsAndArgs(info);

        foreach (var parameter in GetParameters(info, optionsAndArgs))
        {
            info.Parameters.Add(parameter);
        }

        foreach (var childCommand in command.Children)
        {
            var child = BuildSafe(info, childCommand);
            info.Children.Add(child);
        }

        // Normalize argument positions.
        var index = 0;
        foreach (var argument in info.Parameters.OfType<CommandArgument>()
            .OrderBy(argument => argument.Position))
        {
            argument.Position = index;
            index++;
        }

        return info;
    }

    private static CommandInfo BuildSafe(CommandInfo? parent, ConfiguredCommand command)
    {
        var info = new CommandInfo(parent, command);
        var optionsAndArgs = command.OptionsAndArgs;
        if (optionsAndArgs == null)
        {
            throw new InvalidOperationException("Options and arguments must be provided.");
        }

        foreach (var parameter in GetParameters(info, optionsAndArgs))
        {
            info.Parameters.Add(parameter);
        }

        foreach (var childCommand in command.Children)
        {
            var child = BuildSafe(info, childCommand);
            info.Children.Add(child);
        }

        // Normalize argument positions.
        var index = 0;
        foreach (var argument in info.Parameters.OfType<CommandArgument>()
            .OrderBy(argument => argument.Position))
        {
            argument.Position = index;
            index++;
        }

        return info;
    }

    [RequiresUnreferencedCode("Building parameters is annotated with RequiresUnreferencedCode.")]
    internal static OptionsAndArgs GetOptionsAndArgs(CommandInfo command)
    {
        // We need to get parameters in order of the class where they were defined.
        // We assign each inheritance level a value that is used to properly sort the
        // arguments when iterating over them.
        static IEnumerable<OrderedProperties> GetPropertiesInOrder(CommandInfo command)
        {
            var current = command.SettingsType;
            var level = 0;
            var sortOrder = 0;
            while (current.BaseType != null)
            {
                yield return new OrderedProperties(level, sortOrder, current.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));
                current = current.BaseType;

                // Things get a little bit complicated now.
                // Only consider a setting's base type part of the
                // setting, if there isn't a parent command that implements
                // the setting's base type. This might come back to bite us :)
                var currentCommand = command.Parent;
                while (currentCommand != null)
                {
                    if (currentCommand.SettingsType == current)
                    {
                        level--;
                        break;
                    }

                    currentCommand = currentCommand.Parent;
                }

                sortOrder--;
            }
        }

        var groups = GetPropertiesInOrder(command);
        var optionsAndArgs = new List<(IEnumerable<CommandOption> Options, IEnumerable<CommandArgument> Args)>();
        foreach (var group in groups.OrderBy(x => x.Level).ThenBy(x => x.SortOrder))
        {
            var options = new List<CommandOption>();
            var arguments = new List<CommandArgument>();
            foreach (var property in group.Properties)
            {
                if (property.IsDefined(typeof(CommandOptionAttribute)))
                {
                    var attribute = property.GetCustomAttribute<CommandOptionAttribute>();
                    if (attribute != null)
                    {
                        options.Add(BuildOptionParameter(property, attribute));
                    }
                }
                else if (property.IsDefined(typeof(CommandArgumentAttribute)))
                {
                    var attribute = property.GetCustomAttribute<CommandArgumentAttribute>();
                    if (attribute != null)
                    {
                        arguments.Add(BuildArgumentParameter(property, attribute));
                    }
                }
            }

            optionsAndArgs.Add((options, arguments));
        }

        return optionsAndArgs;
    }

    /// <summary>
    /// Takes in a command and a set of options and arguments, which are each ordered by the level
    /// of inheritance and the order in which they were defined in the class. This method will then
    /// create a list of parameters.
    /// </summary>
    private static IEnumerable<CommandParameter> GetParameters(CommandInfo command, OptionsAndArgs optionsAndArgs)
    {
        var result = new List<CommandParameter>();
        var argumentPosition = 0;

        foreach (var (options, arguments) in optionsAndArgs)
        {
            var parameters = new List<CommandParameter>();

            foreach (var option in options)
            {
                // Any previous command has this option defined?
                if (command.HaveParentWithOption(option))
                {
                    // Do we allow it to exist on this command as well?
                    if (command.AllowParentOption(option))
                    {
                        option.IsShadowed = true;
                        parameters.Add(option);
                    }
                }
                else
                {
                    // No parent have this option.
                    parameters.Add(option);
                }
            }

            foreach (var argument in arguments)
            {
                // Any previous command has this argument defined?
                // In that case, we should not assign the parameter to this command.
                if (!command.HaveParentWithArgument(argument))
                {
                    parameters.Add(argument);
                }
            }

            // Update the position for the parameters.
            foreach (var argument in parameters.OfType<CommandArgument>().OrderBy(x => x.Position))
            {
                argument.Position = argumentPosition++;
            }

            // Add all parameters to the result.
            foreach (var groupResult in parameters)
            {
                result.Add(groupResult);
            }
        }

        return result;
    }

    public static CommandOption BuildOptionParameter(
        PropertyInfo property,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
            | DynamicallyAccessedMemberTypes.Interfaces)]
        Type propertyType,
        CommandOptionAttribute attribute)
    {
        var description = property.GetCustomAttribute<DescriptionAttribute>();
        var converter = property.GetCustomAttribute<TypeConverterAttribute>();
        var deconstructor = property.GetCustomAttribute<PairDeconstructorAttribute>();
        var valueProvider = property.GetCustomAttribute<ParameterValueProviderAttribute>();
        var validators = property.GetCustomAttributes<ParameterValidationAttribute>(true);
        var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

        var kind = GetOptionKind(propertyType, attribute, deconstructor, converter);

        if (defaultValue == null && property.PropertyType == typeof(bool))
        {
            defaultValue = new DefaultValueAttribute(false);
        }

        return new CommandOption(propertyType, kind,
            property, description?.Description, converter, deconstructor,
            attribute, valueProvider, validators, defaultValue,
            attribute.ValueIsOptional);
    }

    [RequiresUnreferencedCode("Requires annotating the return type of the property, which is not supported.")]
    private static CommandOption BuildOptionParameter(PropertyInfo property, CommandOptionAttribute attribute)
    {
        var description = property.GetCustomAttribute<DescriptionAttribute>();
        var converter = property.GetCustomAttribute<TypeConverterAttribute>();
        var deconstructor = property.GetCustomAttribute<PairDeconstructorAttribute>();
        var valueProvider = property.GetCustomAttribute<ParameterValueProviderAttribute>();
        var validators = property.GetCustomAttributes<ParameterValidationAttribute>(true);
        var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

        var kind = GetOptionKind(property.PropertyType, attribute, deconstructor, converter);

        if (defaultValue == null && property.PropertyType == typeof(bool))
        {
            defaultValue = new DefaultValueAttribute(false);
        }

        return new CommandOption(property.PropertyType, kind,
            property, description?.Description, converter, deconstructor,
            attribute, valueProvider, validators, defaultValue,
            attribute.ValueIsOptional);
    }

    public static CommandArgument BuildArgumentParameter(
        PropertyInfo property,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.Interfaces)]
        Type propertyType,
        CommandArgumentAttribute attribute)
    {
        var description = property.GetCustomAttribute<DescriptionAttribute>();
        var converter = property.GetCustomAttribute<TypeConverterAttribute>();
        var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();
        var valueProvider = property.GetCustomAttribute<ParameterValueProviderAttribute>();
        var validators = property.GetCustomAttributes<ParameterValidationAttribute>(true);

        var kind = GetParameterKind(propertyType);

        return new CommandArgument(
            propertyType, kind, property,
            description?.Description, converter,
            defaultValue, attribute, valueProvider,
            validators);
    }

    [RequiresUnreferencedCode("Requires annotating the return type of the property, which is not supported.")]
    private static CommandArgument BuildArgumentParameter(PropertyInfo property, CommandArgumentAttribute attribute)
    {
        var description = property.GetCustomAttribute<DescriptionAttribute>();
        var converter = property.GetCustomAttribute<TypeConverterAttribute>();
        var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();
        var valueProvider = property.GetCustomAttribute<ParameterValueProviderAttribute>();
        var validators = property.GetCustomAttributes<ParameterValidationAttribute>(true);

        var kind = GetParameterKind(property.PropertyType);

        return new CommandArgument(
            property.PropertyType, kind, property,
            description?.Description, converter,
            defaultValue, attribute, valueProvider,
            validators);
    }

    private static ParameterKind GetOptionKind(
        Type type,
        CommandOptionAttribute attribute,
        PairDeconstructorAttribute? deconstructor,
        TypeConverterAttribute? converter)
    {
        if (attribute.ValueIsOptional)
        {
            return ParameterKind.FlagWithValue;
        }

        if (type.IsPairDeconstructable() && (deconstructor != null || converter == null))
        {
            return ParameterKind.Pair;
        }

        return GetParameterKind(type);
    }

    private static ParameterKind GetParameterKind(Type type)
    {
        if (type == typeof(bool) || type == typeof(bool?))
        {
            return ParameterKind.Flag;
        }

        if (type.IsArray)
        {
            return ParameterKind.Vector;
        }

        return ParameterKind.Scalar;
    }
}
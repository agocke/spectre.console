namespace Spectre.Console.Cli;

internal sealed class ConfiguredCommand
{
    public string Name { get; }
    public HashSet<string> Aliases { get; }
    public string? Description { get; set; }
    public object? Data { get; set; }
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? CommandType { get; }
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type SettingsType { get; }
    public Func<CommandContext, CommandSettings, Task<int>>? Delegate { get; }
    public bool IsDefaultCommand { get; }
    public bool IsHidden { get; set; }

    public IList<ConfiguredCommand> Children { get; }
    public IList<string[]> Examples { get; }

    private ConfiguredCommand(
        string name,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type? commandType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type settingsType,
        Func<CommandContext, CommandSettings, Task<int>>? @delegate,
        bool isDefaultCommand)
    {
        Name = name;
        Aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CommandType = commandType;
        SettingsType = settingsType;
        Delegate = @delegate;
        IsDefaultCommand = isDefaultCommand;

        // Default commands are always created as hidden.
        IsHidden = IsDefaultCommand;

        Children = new List<ConfiguredCommand>();
        Examples = new List<string[]>();
    }

    public static ConfiguredCommand FromBranch(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type settings, string name)
    {
        return new ConfiguredCommand(name, null, settings, null, false);
    }

    public static ConfiguredCommand FromBranch<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TSettings>(string name)
        where TSettings : CommandSettings
    {
        return new ConfiguredCommand(name, null, typeof(TSettings), null, false);
    }

    public static ConfiguredCommand FromType<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand>(string name, bool isDefaultCommand = false)
        where TCommand : class, ICommand
    {
        var settingsType = ConfigurationHelper.GetSettingsType(typeof(TCommand));
        if (settingsType == null)
        {
            throw CommandRuntimeException.CouldNotGetSettingsType(typeof(TCommand));
        }

        return new ConfiguredCommand(name, typeof(TCommand), settingsType, null, isDefaultCommand);
    }

    public static ConfiguredCommand FromDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TSettings>(
        string name, Func<CommandContext, CommandSettings, Task<int>>? @delegate = null)
        where TSettings : CommandSettings
    {
        return new ConfiguredCommand(name, null, typeof(TSettings), @delegate, false);
    }
}
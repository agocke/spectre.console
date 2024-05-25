using Spectre.Console.Cli.Internal.Configuration;

namespace Spectre.Console.Cli;

/// <summary>
/// The entry point for a command line application with a default command.
/// </summary>
/// <typeparam name="TDefaultCommand">The type of the default command.</typeparam>
public sealed class GeneratedCommandApp<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.Interfaces
    | DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicProperties)] TDefaultCommand> : ICommandApp
    where TDefaultCommand : class, ICommand
{
    private readonly GeneratedCommandApp _app;
    private readonly DefaultCommandConfigurator _defaultCommandConfigurator;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedCommandApp{TDefaultCommand}"/> class.
    /// </summary>
    /// <param name="optionsAndArgs">The options and arguments.</param>
    public GeneratedCommandApp(OptionsAndArgs optionsAndArgs)
    {
        _app = new GeneratedCommandApp(new TrimmableTypeRegistrar());
        _defaultCommandConfigurator = _app.SetDefaultCommand<TDefaultCommand>(optionsAndArgs);
    }

    /// <summary>
    /// Configures the command line application.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void Configure(Action<IConfigurator> configuration)
    {
        _app.Configure(configuration);
    }

    /// <summary>
    /// Runs the command line application with specified arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The exit code from the executed command.</returns>
    public int Run(IEnumerable<string> args)
    {
        return _app.Run(args);
    }

    /// <summary>
    /// Runs the command line application with specified arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The exit code from the executed command.</returns>
    public Task<int> RunAsync(IEnumerable<string> args)
    {
        return _app.RunAsync(args);
    }

    internal Configurator GetConfigurator()
    {
        return _app.GetConfigurator();
    }

    /// <summary>
    /// Sets the description of the default command.
    /// </summary>
    /// <param name="description">The default command description.</param>
    /// <returns>The same <see cref="CommandApp{TDefaultCommand}"/> instance so that multiple calls can be chained.</returns>
    public GeneratedCommandApp<TDefaultCommand> WithDescription(string description)
    {
        _defaultCommandConfigurator.WithDescription(description);
        return this;
    }

    /// <summary>
    /// Sets data that will be passed to the command via the <see cref="CommandContext"/>.
    /// </summary>
    /// <param name="data">The data to pass to the default command.</param>
    /// <returns>The same <see cref="CommandApp{TDefaultCommand}"/> instance so that multiple calls can be chained.</returns>
    public GeneratedCommandApp<TDefaultCommand> WithData(object data)
    {
        _defaultCommandConfigurator.WithData(data);
        return this;
    }
}
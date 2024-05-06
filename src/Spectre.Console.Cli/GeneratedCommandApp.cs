using Spectre.Console.Cli.Internal.Configuration;

namespace Spectre.Console.Cli;

/// <summary>
/// The entry point for a command line application.
/// </summary>
public sealed class GeneratedCommandApp : ICommandApp
{
    private readonly Configurator _configurator;
    private readonly GeneratedCommandExecutor _executor;
    private bool _executed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedCommandApp"/> class.
    /// </summary>
    /// <param name="registrar">The registrar.</param>
    public GeneratedCommandApp(ITypeRegistrar registrar)
    {
        _configurator = new Configurator(registrar);
        _executor = new GeneratedCommandExecutor(registrar);
    }

    /// <summary>
    /// Configures the command line application.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public void Configure(Action<IConfigurator> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        configuration(_configurator);
    }

    /// <summary>
    /// Sets the default command.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <returns>A <see cref="DefaultCommandConfigurator"/> that can be used to configure the default command.</returns>
    [RequiresUnreferencedCode("OptionsAndArgs must be provided for trim-compatibility.")]
    public DefaultCommandConfigurator SetDefaultCommand<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.Interfaces
        | DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>()
        where TCommand : class, ICommand
    {
        return new DefaultCommandConfigurator(GetConfigurator().SetDefaultCommand<TCommand>());
    }

    /// <summary>
    /// Sets the default command.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="optionsAndArgs">The options and arguments.</param>
    /// <returns>A <see cref="DefaultCommandConfigurator"/> that can be used to configure the default command.</returns>
    public DefaultCommandConfigurator SetDefaultCommand<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.Interfaces
        | DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(OptionsAndArgs optionsAndArgs)
        where TCommand : class, ICommand
    {
        return new DefaultCommandConfigurator(GetConfigurator().SetDefaultCommand<TCommand>(optionsAndArgs));
    }

    /// <summary>
    /// Runs the command line application with specified arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The exit code from the executed command.</returns>
    public int Run(IEnumerable<string> args)
    {
        return RunAsync(args).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs the command line application with specified arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The exit code from the executed command.</returns>
    public async Task<int> RunAsync(IEnumerable<string> args)
    {
        try
        {
            if (!_executed)
            {
                // Add built-in (hidden) commands.
                _configurator.AddBranch(CliConstants.Commands.Branch, cli =>
                {
                    cli.HideBranch();
                    cli.AddCommand<VersionCommand>(CliConstants.Commands.Version, CommandModelBuilder.EmptyOptionsAndArgs);
                    cli.AddCommand<XmlDocCommand>(CliConstants.Commands.XmlDoc, CommandModelBuilder.EmptyOptionsAndArgs);
                    cli.AddCommand<ExplainCommand>(CliConstants.Commands.Explain, CommandModelBuilder.EmptyOptionsAndArgs);
                });

                _executed = true;
            }

            return await _executor
                .Execute(_configurator, args)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Should we always propagate when debugging?
            if (Debugger.IsAttached
                && ex is CommandAppException appException
                && appException.AlwaysPropagateWhenDebugging)
            {
                throw;
            }

            if (_configurator.Settings.PropagateExceptions)
            {
                throw;
            }

            if (_configurator.Settings.ExceptionHandler != null)
            {
                return _configurator.Settings.ExceptionHandler(ex);
            }

            // Render the exception.
            var pretty = GetRenderableErrorMessage(ex);
            if (pretty != null)
            {
                _configurator.Settings.Console.SafeRender(pretty);
            }

            return -1;
        }
    }

    internal Configurator GetConfigurator()
    {
        return _configurator;
    }

    private static List<IRenderable?>? GetRenderableErrorMessage(Exception ex, bool convert = true)
    {
        if (ex is CommandAppException renderable && renderable.Pretty != null)
        {
            return new List<IRenderable?> { renderable.Pretty };
        }

        if (convert)
        {
            var converted = new List<IRenderable?>
                {
                    new Composer()
                        .Text("[red]Error:[/]")
                        .Space()
                        .Text(ex.Message.EscapeMarkup())
                        .LineBreak(),
                };

            // Got a renderable inner exception?
            if (ex.InnerException != null)
            {
                var innerRenderable = GetRenderableErrorMessage(ex.InnerException, convert: false);
                if (innerRenderable != null)
                {
                    converted.AddRange(innerRenderable);
                }
            }

            return converted;
        }

        return null;
    }
}
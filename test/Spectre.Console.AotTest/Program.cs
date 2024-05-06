using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;
using static Spectre.Console.Cli.CommandModelBuilder;

var searchPathProp = typeof(FileSizeCommand.Settings).GetProperty(nameof(FileSizeCommand.Settings.SearchPath))!;
var searchPatternProp = typeof(FileSizeCommand.Settings).GetProperty(nameof(FileSizeCommand.Settings.SearchPattern))!;
var includeHiddenProp = typeof(FileSizeCommand.Settings).GetProperty(nameof(FileSizeCommand.Settings.IncludeHidden))!;

IEnumerable<CommandOption> options = [
    BuildOptionParameter(searchPatternProp, typeof(string), searchPatternProp.GetCustomAttribute<CommandOptionAttribute>()!),
    BuildOptionParameter(includeHiddenProp, typeof(bool), includeHiddenProp.GetCustomAttribute<CommandOptionAttribute>()!),
];
IEnumerable<CommandArgument> arguments = [
    BuildArgumentParameter(searchPathProp, typeof(string), searchPathProp.GetCustomAttribute<CommandArgumentAttribute>()!),
];
var app = new GeneratedCommandApp<FileSizeCommand>(
    new DefaultTypeRegistrar(),
    [(options, arguments)]);

return app.Run(args);

internal sealed class GeneratedBinder : IGeneratedCommandBinder
{
    private string? _searchPath = null;
    private string? _searchPattern = null;
    private bool _includeHidden = true;

    public void BindMapped(IEnumerable<(CommandParameter Parameter, string? Value)> mapped)
    {
        throw new System.NotImplementedException();
    }

    public void BindUnmapped(IEnumerable<CommandParameter> unmapped)
    {
        foreach (var parameter in unmapped)
        {
        }
    }

    public CommandSettings BuildSettings() => new FileSizeCommand.Settings
    {
        SearchPath = _searchPath,
        SearchPattern = _searchPattern,
        IncludeHidden = _includeHidden,
    };
}

internal sealed class FileSizeCommand : Command<FileSizeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to search. Defaults to current directory.")]
        [CommandArgument(0, "[searchPath]")]
        public string? SearchPath { get; init; }

        [CommandOption("-p|--pattern")]
        public string? SearchPattern { get; init; }

        [CommandOption("--hidden")]
        [DefaultValue(true)]
        public bool IncludeHidden { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var searchOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.Hidden | FileAttributes.System
                : FileAttributes.System,
        };

        var searchPattern = settings.SearchPattern ?? "*.*";
        var searchPath = settings.SearchPath ?? Directory.GetCurrentDirectory();
        var files = new DirectoryInfo(searchPath)
            .GetFiles(searchPattern, searchOptions);

        var totalFileSize = files
            .Sum(fileInfo => fileInfo.Length);

        AnsiConsole.MarkupLine($"Total file size for [green]{searchPattern}[/] files in [green]{searchPath}[/]: [blue]{totalFileSize:N0}[/] bytes");

        return 0;
    }
}
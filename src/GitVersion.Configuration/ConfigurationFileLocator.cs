using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration;

internal class ConfigurationFileLocator(
    IFileSystem fileSystem,
    ILog log,
    IOptions<GitVersionOptions> options)
    : IConfigurationFileLocator
{
    public const string DefaultFileName = "GitVersion.yml";
    public const string DefaultAlternativeFileName = "GitVersion.yaml";
    public const string DefaultFileNameDotted = $".{DefaultFileName}";
    public const string DefaultAlternativeFileNameDotted = $".{DefaultAlternativeFileName}";

    private readonly string[] SupportedConfigFileNames =
    [
        DefaultFileName,
        DefaultAlternativeFileName,
        DefaultFileNameDotted,
        DefaultAlternativeFileNameDotted
    ];

    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ILog log = log.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    private string? ConfigurationFile => options.Value.ConfigurationInfo.ConfigurationFile;

    public void Verify(string? workingDirectory, string? projectRootDirectory)
    {
        if (PathHelper.IsPathRooted(this.ConfigurationFile)) return;
        if (PathHelper.Equal(workingDirectory, projectRootDirectory)) return;
        WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
    }

    public string? GetConfigurationFile(string? directoryPath)
    {
        string[] configurationFilePaths = string.IsNullOrWhiteSpace(this.ConfigurationFile)
        ? this.SupportedConfigFileNames : [this.ConfigurationFile, .. this.SupportedConfigFileNames];

        foreach (var item in configurationFilePaths)
        {
            this.log.Debug($"Trying to find configuration file {item} at '{directoryPath}'");

            string configurationFilePath;
            if (!PathHelper.IsPathRooted(item))
            {
                if (string.IsNullOrEmpty(directoryPath))
                {
                    this.log.Info($"The configuration file '{item}' is relative and no directory path known.");
                    continue;
                }
                configurationFilePath = Path.Combine(directoryPath, item);
            }
            else
            {
                configurationFilePath = item;
            }

            if (fileSystem.File.Exists(configurationFilePath))
            {
                this.log.Info($"Found configuration file at '{configurationFilePath}'");
                return configurationFilePath;
            }

            this.log.Debug($"Configuration file {configurationFilePath} not found at '{directoryPath}'");
        }

        return null;
    }

    private void WarnAboutAmbiguousConfigFileSelection(string? workingDirectory, string? projectRootDirectory)
    {
        var workingConfigFile = GetConfigurationFile(workingDirectory);
        var projectRootConfigFile = GetConfigurationFile(projectRootDirectory);

        var hasConfigInWorkingDirectory = workingConfigFile is not null;
        var hasConfigInProjectRootDirectory = projectRootConfigFile is not null;

        if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
        {
            throw new WarningException($"Ambiguous configuration file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
        }

        if (hasConfigInProjectRootDirectory || hasConfigInWorkingDirectory || this.SupportedConfigFileNames.Any(entry => entry.Equals(this.ConfigurationFile, StringComparison.OrdinalIgnoreCase))) return;

        workingConfigFile = PathHelper.Combine(workingDirectory, this.ConfigurationFile);
        projectRootConfigFile = PathHelper.Combine(projectRootDirectory, this.ConfigurationFile);
        throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
    }
}

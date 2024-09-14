using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

using Moq;

using MovieAutoMerge.Configuration;
using MovieAutoMerge.ScheduledTasks.Model;
using MovieAutoMerge.Tests.Utils;
using MovieAutoMerge.Utils;

namespace MovieAutoMerge.Tests.Tests;

public class BaseTest
{
    private static readonly NLog.ILogger Logger = NLog.LogManager.GetLogger(nameof(BaseTest));

    protected readonly Mock<ILogManager> _logManager = new();
    protected readonly Mock<IFileSystem> _fileSystem = new();
    protected readonly Mock<IApplicationPaths> _applicationPaths = new();
    protected readonly Mock<IXmlSerializer> _xmlSerializer = new();
    protected readonly Mock<ILibraryManager> _libraryManager = new();
    protected readonly Mock<ILocalizationManager> _localizationManager = new();
    protected readonly Mock<IServerConfigurationManager> _serverConfigurationManager = new();
    protected readonly Mock<IServerApplicationHost> _serverApplicationHost = new();
    protected readonly Mock<IItemRepository> _itemRepository = new();

    protected readonly EmbyJsonSerializer _jsonSerializer = new();

    internal TaskTranslation? GetTranslation(string key, string language)
    {
        var resourcePath = $"MovieAutoMerge.i18n.{key}.{language}.json";
        using var stream = typeof(EmbyHelper).Assembly.GetManifestResourceStream(resourcePath);
        return _jsonSerializer.DeserializeFromStream<TaskTranslation>(stream!);
    }

    protected void VerifyNoOtherCalls()
    {
        try
        {
            _applicationPaths.VerifyNoOtherCalls();
            _fileSystem.VerifyNoOtherCalls();
            _itemRepository.VerifyNoOtherCalls();
            _libraryManager.VerifyNoOtherCalls();
            _localizationManager.VerifyNoOtherCalls();
            _logManager.VerifyNoOtherCalls();
            _serverApplicationHost.VerifyNoOtherCalls();
            _serverConfigurationManager.VerifyNoOtherCalls();
            _xmlSerializer.VerifyNoOtherCalls();
        }
        catch (Exception e)
        {
            Logger.Error(e);
            PrintMocks();
            throw;
        }
    }

    private static void PrintMockInvocations(Mock mock)
    {
        Logger.Info($"Name: {mock.Object.GetType().Name}");
        foreach (IInvocation? invocation in mock.Invocations)
        {
            Logger.Info(invocation);
        }
    }

    private void PrintMocks()
    {
        PrintMockInvocations(_applicationPaths);
        PrintMockInvocations(_fileSystem);
        PrintMockInvocations(_itemRepository);
        PrintMockInvocations(_libraryManager);
        PrintMockInvocations(_localizationManager);
        PrintMockInvocations(_logManager);
        PrintMockInvocations(_serverApplicationHost);
        PrintMockInvocations(_serverConfigurationManager);
        PrintMockInvocations(_xmlSerializer);
    }

    protected void CommonConfig(PluginConfiguration pluginConfiguration)
    {
        _ = _logManager
            .Setup(lm => lm.GetLogger(Plugin.Instance.Name))
            .Returns(new EmbyLogger(NLog.LogManager.GetLogger(Plugin.Instance.Name)));

        _ = _xmlSerializer
            .Setup(xs => xs.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(pluginConfiguration);

        _ = _serverConfigurationManager
            .SetupGet(scm => scm.Configuration)
            .Returns(new ServerConfiguration
            {
                UICulture = "ru"
            });
    }
}

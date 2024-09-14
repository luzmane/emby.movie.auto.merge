using FluentAssertions;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

using Moq;

using MovieAutoMerge.Configuration;
using MovieAutoMerge.ScheduledTasks;
using MovieAutoMerge.Tests.Utils;

namespace MovieAutoMerge.Tests.Tests;

public class SplitMoviesTaskTests : BaseTest
{
    private static readonly NLog.ILogger Logger = NLog.LogManager.GetLogger(nameof(SplitMoviesTaskTests));

    private readonly PluginConfiguration _pluginConfiguration;
    private readonly SplitMoviesTask _splitMoviesTask;
    private Dictionary<long, BaseItem> _librariesVault;
    private Dictionary<long, BaseItem> _moviesVault;

    public SplitMoviesTaskTests()
    {
        _pluginConfiguration = new PluginConfiguration();

        BaseItem.ConfigurationManager = _serverConfigurationManager.Object;
        BaseItem.FileSystem = _fileSystem.Object;
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.LocalizationManager = _localizationManager.Object;
        BaseItem.ItemRepository = _itemRepository.Object;
        BaseItem.ApplicationHost = _serverApplicationHost.Object;
        CollectionFolder.XmlSerializer = _xmlSerializer.Object;

        _ = new Plugin(
            _applicationPaths.Object,
            _xmlSerializer.Object,
            _libraryManager.Object
        );
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));

        CommonConfig();

        _splitMoviesTask = new SplitMoviesTask(
            _libraryManager.Object,
            _logManager.Object,
            _serverConfigurationManager.Object,
            _jsonSerializer
        );
    }

    private void CommonConfig()
    {
        base.CommonConfig(_pluginConfiguration);

        _librariesVault = new Dictionary<long, BaseItem>
        {
            {
                2L, new CollectionFolder
                {
                    Name = "Movies",
                    InternalId = 2L,
                    ParentId = 1L
                }
            },
            {
                3L, new CollectionFolder
                {
                    Name = "TV Shows",
                    InternalId = 3L,
                    ParentId = 1L
                }
            },
            {
                4L, new CollectionFolder
                {
                    Name = "Top Picks",
                    InternalId = 4L,
                    ParentId = 1L
                }
            }
        };

        _moviesVault = new Dictionary<long, BaseItem>
        {
            {
                101L, new Movie
                {
                    Name = "Merged 111",
                    InternalId = 101L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "111" }
                    },
                    PresentationUniqueKey = "merged_111"
                }
            },
            {
                102L, new Movie
                {
                    Name = "Merged 111",
                    InternalId = 102L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "111" }
                    },
                    PresentationUniqueKey = "merged_111"
                }
            },
            {
                103L, new Movie
                {
                    Name = "Merged 111",
                    InternalId = 103L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "111" }
                    },
                    PresentationUniqueKey = "merged_111"
                }
            },
            {
                107L, new Movie
                {
                    Name = "Merged 111_Locked",
                    InternalId = 107L,
                    ParentId = 2L,
                    IsLocked = true,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "111" }
                    },
                    PresentationUniqueKey = "merged_111"
                }
            },
            {
                104L, new Movie
                {
                    Name = "Merged 111_2",
                    InternalId = 104L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "111" }
                    },
                    PresentationUniqueKey = "merged_111_2"
                }
            },
            {
                105L, new Movie
                {
                    Name = "Merged 111_2",
                    InternalId = 105L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "111" }
                    },
                    PresentationUniqueKey = "merged_111_2"
                }
            },
            {
                106L, new Movie
                {
                    Name = "Merged 222",
                    InternalId = 106L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "222" }
                    },
                    PresentationUniqueKey = "merged_222"
                }
            },
            {
                109L, new Movie
                {
                    Name = "Merged 222",
                    InternalId = 109L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "222" }
                    },
                    PresentationUniqueKey = "merged_222"
                }
            },
            {
                108L, new Movie
                {
                    Name = "No Pair",
                    InternalId = 108L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "987" }
                    },
                    PresentationUniqueKey = "no pair"
                }
            },
        };

        _ = _libraryManager // List all movies
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.MediaTypes.Length == 1
                && nameof(MediaType.Video).Equals(query.MediaTypes[0], StringComparison.Ordinal)
                && true.Equals(query.Recursive)
                && true.Equals(query.HasPath)
                && false.Equals(query.IsVirtualItem))))
            .Returns(_moviesVault.Values.ToArray());

        _ = _libraryManager // find the item by Provider ID
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                true.Equals(query.Recursive)
                && query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.MediaTypes.Length == 1
                && false.Equals(query.IsVirtualItem)
                && nameof(MediaType.Video).Equals(query.MediaTypes[0], StringComparison.Ordinal)
                && true.Equals(query.HasPath)
                && query.AnyProviderIdEquals.Count == 1
            )))
            .Returns((InternalItemsQuery query) => _moviesVault.Values
                .Where(l => string.Equals(l.ProviderIds[query.AnyProviderIdEquals.First().Key], query.AnyProviderIdEquals.First().Value, StringComparison.Ordinal))
                .ToArray());

        _ = _libraryManager // GetAlternateVersionIds
            .Setup(m => m.GetInternalItemIds(It.Is<InternalItemsQuery>(query =>
                query.ExcludeItemIds.Length == 1
                && query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && false.Equals(query.IncludeLiveTVView)
                && !string.IsNullOrWhiteSpace(query.PresentationUniqueKey))))
            .Returns((InternalItemsQuery query) => _moviesVault.Values
                .Where(i =>
                    query.PresentationUniqueKey.Equals(i.PresentationUniqueKey, StringComparison.Ordinal)
                    && query.ExcludeItemIds[0] != i.InternalId)
                .Select(i => i.InternalId)
                .ToArray());

        _ = _libraryManager // Get library by ID
            .Setup(m => m.GetItemById(It.IsInRange(2L, 4L, Moq.Range.Inclusive)))
            .Returns((long id) => _librariesVault[id]);

        _ = _libraryManager // Get movie by ID
            .Setup(m => m.GetItemById(It.IsInRange(100L, 110L, Moq.Range.Inclusive)))
            .Returns((long id) => _moviesVault[id]);

        _ = _libraryManager
            .SetupGet(m => m.RootFolderId)
            .Returns(1L);
    }


    [Fact]
    public async Task Execute_DoNotChangeLockedItems_true()
    {
        Logger.Info($"Start '{nameof(Execute_DoNotChangeLockedItems_true)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_DoNotChangeLockedItems_true));

        _pluginConfiguration.DoNotChangeLockedItems = true;

        using var cancellationTokenSource = new CancellationTokenSource();
        await _splitMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _xmlSerializer.Verify(xs => xs.DeserializeFromFile(typeof(PluginConfiguration), $"{nameof(Execute_DoNotChangeLockedItems_true)}/MovieAutoMerge.xml"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive)), Times.Exactly(27));
        _libraryManager.Verify(lm => lm.GetInternalItemIds(It.IsAny<InternalItemsQuery>()), Times.Exactly(33));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(9));
        _libraryManager.Verify(lm => lm.SplitItems(It.IsAny<BaseItem>()), Times.Exactly(4));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_DoNotChangeLockedItems_true)}'");
    }

    [Fact]
    public async Task Execute_DoNotChangeLockedItems_false()
    {
        Logger.Info($"Start '{nameof(Execute_DoNotChangeLockedItems_false)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_DoNotChangeLockedItems_false));

        _pluginConfiguration.DoNotChangeLockedItems = false;

        using var cancellationTokenSource = new CancellationTokenSource();
        await _splitMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _xmlSerializer.Verify(xs => xs.DeserializeFromFile(typeof(PluginConfiguration), $"{nameof(Execute_DoNotChangeLockedItems_false)}/MovieAutoMerge.xml"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive)), Times.Exactly(27));
        _libraryManager.Verify(lm => lm.GetInternalItemIds(It.IsAny<InternalItemsQuery>()), Times.Exactly(17));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(9));
        _libraryManager.Verify(lm => lm.SplitItems(It.IsAny<BaseItem>()), Times.Exactly(8));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_DoNotChangeLockedItems_false)}'");
    }

    [Fact]
    public void SplitMovies_Success()
    {
        Logger.Info($"Start '{nameof(SplitMovies_Success)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(SplitMovies_Success));

        _pluginConfiguration.DoNotChangeLockedItems = false;

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = _splitMoviesTask.SplitMovies(MetadataProviders.Tmdb.ToString(), "111");
        result.Should().BeTrue();

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetInternalItemIds(It.IsAny<InternalItemsQuery>()), Times.Exactly(6));
        _libraryManager.Verify(lm => lm.SplitItems(It.IsAny<BaseItem>()), Times.Exactly(6));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(SplitMovies_Success)}'");
    }

    [Fact]
    public void SplitMovies_NothingFound()
    {
        Logger.Info($"Start '{nameof(SplitMovies_NothingFound)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(SplitMovies_NothingFound));

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = _splitMoviesTask.SplitMovies(MetadataProviders.Tmdb.ToString(), "INVALID");
        result.Should().BeFalse();

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(SplitMovies_NothingFound)}'");
    }

    [Fact]
    public void SplitMovies_EmptyInput()
    {
        Logger.Info($"Start '{nameof(SplitMovies_EmptyInput)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(SplitMovies_EmptyInput));

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = _splitMoviesTask.SplitMovies(MetadataProviders.Tmdb.ToString(), "");
        result.Should().BeFalse();

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(SplitMovies_EmptyInput)}'");
    }

    [Fact]
    public void ForCodeCoverage()
    {
        Logger.Info($"Start '{nameof(ForCodeCoverage)}'");

        _splitMoviesTask.IsHidden.Should().BeFalse();
        _splitMoviesTask.IsEnabled.Should().BeTrue();
        _splitMoviesTask.IsLogged.Should().BeTrue();
        _splitMoviesTask.Key.Should().NotBeNull();

        _splitMoviesTask.GetDefaultTriggers().Should().BeEmpty();

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(ForCodeCoverage)}'");
    }

    [Fact]
    public void GetTranslation_RU()
    {
        Logger.Info($"Start '{nameof(GetTranslation_RU)}'");

        _ = _serverConfigurationManager
            .SetupGet(scm => scm.Configuration)
            .Returns(new ServerConfiguration
            {
                UICulture = "ru"
            });

        var translation = GetTranslation("ScheduledTasks.SplitMoviesTask", "ru");

        translation?.Name.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Name.Should().Be(translation!.Name);
        translation.Description.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Description.Should().Be(translation.Description);
        translation.Category.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Category.Should().Be(translation.Category);

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.Exactly(6));
        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_RU)}'");
    }

    [Fact]
    public void GetTranslation_EnUs()
    {
        Logger.Info($"Start '{nameof(GetTranslation_EnUs)}'");

        _ = _serverConfigurationManager
            .SetupGet(scm => scm.Configuration)
            .Returns(new ServerConfiguration
            {
                UICulture = "en-us"
            });

        var translation = GetTranslation("ScheduledTasks.SplitMoviesTask", "en-US");

        translation?.Name.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Name.Should().Be(translation!.Name);
        translation.Description.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Description.Should().Be(translation.Description);
        translation.Category.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Category.Should().Be(translation.Category);

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.Exactly(6));
        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_EnUs)}'");
    }

    [Fact]
    public void GetTranslation_BG()
    {
        Logger.Info($"Start '{nameof(GetTranslation_BG)}'");

        _ = _serverConfigurationManager
            .SetupGet(scm => scm.Configuration)
            .Returns(new ServerConfiguration
            {
                UICulture = "bg"
            });

        var translation = GetTranslation("ScheduledTasks.SplitMoviesTask", "en-US");

        translation?.Name.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Name.Should().Be(translation!.Name);
        translation.Description.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Description.Should().Be(translation.Description);
        translation.Category.Should().NotBeNullOrWhiteSpace();
        _splitMoviesTask.Category.Should().Be(translation.Category);

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.Exactly(6));
        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_EnUs)}'");
    }
}

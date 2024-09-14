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

public class MergeMoviesTaskTests : BaseTest
{
    private static readonly NLog.ILogger Logger = NLog.LogManager.GetLogger(nameof(MergeMoviesTaskTests));

    private readonly PluginConfiguration _pluginConfiguration;
    private readonly MergeMoviesTask _mergeMoviesTask;

    public MergeMoviesTaskTests()
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

        _mergeMoviesTask = new MergeMoviesTask(
            _libraryManager.Object,
            _logManager.Object,
            _serverConfigurationManager.Object,
            _jsonSerializer
        );
    }

    private void CommonConfig()
    {
        base.CommonConfig(_pluginConfiguration);

        Dictionary<long, BaseItem> librariesVault = new Dictionary<long, BaseItem>
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

        Dictionary<long, BaseItem> moviesVault = new Dictionary<long, BaseItem>
        {
            #region Movies

            {
                101L, new Movie
                {
                    Name = "Same in MOVIE, TV SHOW",
                    InternalId = 101L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "112" }
                    }
                }
            },
            {
                102L, new Movie
                {
                    Name = "No Pair",
                    InternalId = 102L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { "KinopoiskRu", "211" },
                        { MetadataProviders.Tmdb.ToString(), "tt211" },
                        { MetadataProviders.Imdb.ToString(), "211" }
                    }
                }
            },
            {
                103L, new Movie
                {
                    Name = "Same in MOVIE, different providers",
                    InternalId = 103L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { "KinopoiskRu", "222" }
                    }
                }
            },
            {
                104L, new Movie
                {
                    Name = "Same in MOVIE, different providers",
                    InternalId = 104L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { "KinopoiskRu", "222" },
                        { MetadataProviders.Imdb.ToString(), "222" }
                    }
                }
            },
            {
                105L, new Movie
                {
                    Name = "Same in MOVIE, different providers",
                    InternalId = 105L,
                    ParentId = 2L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Imdb.ToString(), "222" }
                    }
                }
            },
            {
                106L, new Movie
                {
                    Name = "Same in MOVIE, different providers. Locked",
                    InternalId = 106L,
                    ParentId = 2L,
                    IsLocked = true,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { "KinopoiskRu", "222" },
                        { MetadataProviders.Imdb.ToString(), "222" }
                    }
                }
            },
            {
                107L, new Movie
                {
                    Name = "Same in MOVIE, TV SHOW. Locked",
                    InternalId = 107L,
                    ParentId = 2L,
                    IsLocked = true,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "212" }
                    }
                }
            },

            #endregion

            #region TV Shows

            {
                108L, new Movie
                {
                    Name = "Same in MOVIE, TV SHOW",
                    InternalId = 108L,
                    ParentId = 3L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "112" }
                    }
                }
            },
            {
                109L, new Movie
                {
                    Name = "No pair",
                    InternalId = 109L,
                    ParentId = 3L,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { "KinopoiskRu", "311" },
                        { MetadataProviders.Tmdb.ToString(), "tt311" },
                        { MetadataProviders.Imdb.ToString(), "311" }
                    }
                }
            },
            {
                110L, new Movie
                {
                    Name = "Same in MOVIE, TV SHOW. Locked",
                    InternalId = 110L,
                    ParentId = 3L,
                    IsLocked = true,
                    ProviderIds = new ProviderIdDictionary
                    {
                        { MetadataProviders.Tmdb.ToString(), "212" }
                    }
                }
            }

            #endregion
        };

        _ = _libraryManager // List all libraries
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(CollectionFolder).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && true.Equals(query.IsFolder)
                && false.Equals(query.IsVirtualItem))))
            .Returns(librariesVault.Values.ToArray());

        _ = _libraryManager // List Movie library
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(CollectionFolder).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.ItemIds.Length == 1L
                && 2L == query.ItemIds[0]
                && true.Equals(query.IsFolder)
                && false.Equals(query.IsVirtualItem))))
            .Returns(librariesVault.Values.Where(i => i.InternalId == 2L).ToArray());

        _ = _libraryManager // List TV Show library
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(CollectionFolder).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.ItemIds.Length == 1L
                && 3L == query.ItemIds[0]
                && true.Equals(query.IsFolder)
                && false.Equals(query.IsVirtualItem))))
            .Returns(librariesVault.Values.Where(i => i.InternalId == 3L).ToArray());

        _ = _libraryManager // List all movies
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.MediaTypes.Length == 1
                && nameof(MediaType.Video).Equals(query.MediaTypes[0], StringComparison.Ordinal)
                && query.ParentIds.Length == 2
                && true.Equals(query.Recursive)
                && true.Equals(query.HasPath)
                && false.Equals(query.IsVirtualItem))))
            .Returns(moviesVault.Values.ToArray());

        _ = _libraryManager // List "Movies" movies
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.MediaTypes.Length == 1
                && nameof(MediaType.Video).Equals(query.MediaTypes[0], StringComparison.Ordinal)
                && query.ParentIds.Length == 1
                && 2L == query.ParentIds[0]
                && true.Equals(query.Recursive)
                && true.Equals(query.HasPath)
                && false.Equals(query.IsVirtualItem))))
            .Returns(moviesVault.Values.Where(i => i.ParentId == 2L).ToArray());

        _ = _libraryManager // List "TV Show" movies
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.MediaTypes.Length == 1
                && nameof(MediaType.Video).Equals(query.MediaTypes[0], StringComparison.Ordinal)
                && query.ParentIds.Length == 1
                && 3L == query.ParentIds[0]
                && true.Equals(query.Recursive)
                && true.Equals(query.HasPath)
                && false.Equals(query.IsVirtualItem))))
            .Returns(moviesVault.Values.Where(i => i.ParentId == 3L).ToArray());

        _ = _libraryManager // Get library by ID
            .Setup(m => m.GetItemById(It.IsInRange(2L, 4L, Moq.Range.Inclusive)))
            .Returns((long id) => librariesVault[id]);

        _ = _libraryManager // Get movie by ID
            .Setup(m => m.GetItemById(It.IsInRange(100L, 110L, Moq.Range.Inclusive)))
            .Returns((long id) => moviesVault[id]);

        _ = _libraryManager
            .SetupGet(m => m.RootFolderId)
            .Returns(1L);
    }

    [Fact]
    public async Task Execute_MergeAcrossLibs_true()
    {
        Logger.Info($"Start '{nameof(Execute_MergeAcrossLibs_true)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_MergeAcrossLibs_true));

        _pluginConfiguration.MergeAcrossLibraries = true;
        _pluginConfiguration.DoNotChangeLockedItems = true;

        using var cancellationTokenSource = new CancellationTokenSource();
        await _mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _xmlSerializer.Verify(xs => xs.DeserializeFromFile(typeof(PluginConfiguration), $"{nameof(Execute_MergeAcrossLibs_true)}/MovieAutoMerge.xml"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive)), Times.Exactly(21));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(7));
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Exactly(2));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_MergeAcrossLibs_true)}'");
    }

    [Fact]
    public async Task Execute_MergeAcrossLibs_false()
    {
        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_MergeAcrossLibs_false));

        _pluginConfiguration.MergeAcrossLibraries = false;
        _pluginConfiguration.DoNotChangeLockedItems = true;

        using var cancellationTokenSource = new CancellationTokenSource();
        await _mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _xmlSerializer.Verify(xs => xs.DeserializeFromFile(typeof(PluginConfiguration), $"{nameof(Execute_MergeAcrossLibs_false)}/MovieAutoMerge.xml"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(4));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive)), Times.Exactly(21));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(7));
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Once());

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_MergeAcrossLibs_false)}'");
    }

    [Fact]
    public async Task Execute_DoNotChangeLockedItems_false()
    {
        Logger.Info($"Start '{nameof(Execute_DoNotChangeLockedItems_false)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_DoNotChangeLockedItems_false));

        _pluginConfiguration.MergeAcrossLibraries = true;
        _pluginConfiguration.DoNotChangeLockedItems = false;

        using var cancellationTokenSource = new CancellationTokenSource();
        await _mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _xmlSerializer.Verify(xs => xs.DeserializeFromFile(typeof(PluginConfiguration), $"{nameof(Execute_DoNotChangeLockedItems_false)}/MovieAutoMerge.xml"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive)), Times.Exactly(30));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(10));
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Exactly(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_DoNotChangeLockedItems_false)}'");
    }

    [Fact]
    public void ForCodeCoverage()
    {
        Logger.Info($"Start '{nameof(ForCodeCoverage)}'");

        _mergeMoviesTask.IsHidden.Should().BeFalse();
        _mergeMoviesTask.IsEnabled.Should().BeTrue();
        _mergeMoviesTask.IsLogged.Should().BeTrue();
        _mergeMoviesTask.Key.Should().NotBeNull();

        _mergeMoviesTask.GetDefaultTriggers().Should().BeEmpty();

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

        var translation = GetTranslation("ScheduledTasks.MergeMoviesTask", "ru");

        translation?.Name.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Name.Should().Be(translation!.Name);
        translation.Description.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Description.Should().Be(translation.Description);
        translation.Category.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Category.Should().Be(translation.Category);

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

        var translation = GetTranslation("ScheduledTasks.MergeMoviesTask", "en-US");

        translation?.Name.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Name.Should().Be(translation!.Name);
        translation.Description.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Description.Should().Be(translation.Description);
        translation.Category.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Category.Should().Be(translation.Category);

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

        var translation = GetTranslation("ScheduledTasks.MergeMoviesTask", "en-US");

        translation?.Name.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Name.Should().Be(translation!.Name);
        translation.Description.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Description.Should().Be(translation.Description);
        translation.Category.Should().NotBeNullOrWhiteSpace();
        _mergeMoviesTask.Category.Should().Be(translation.Category);

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.Exactly(6));
        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_EnUs)}'");
    }
}

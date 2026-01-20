using FluentAssertions;

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Data;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

using Moq;

using MovieAutoMerge.I18n;
using MovieAutoMerge.ScheduledTasks;
using MovieAutoMerge.Tests.Utils;

namespace MovieAutoMerge.Tests.Tests;

public class SplitMoviesTaskTests : BaseTest
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(SplitMoviesTaskTests));

    private Dictionary<long, BaseItem> _librariesVault;
    private Dictionary<long, BaseItem> _moviesVault;

    public SplitMoviesTaskTests()
    {
        BaseItem.ConfigurationManager = _serverConfigurationManager.Object;
        BaseItem.FileSystem = _fileSystem.Object;
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.LocalizationManager = _localizationManager.Object;
        BaseItem.ItemRepository = _itemRepository.Object;
        BaseItem.ApplicationHost = _serverApplicationHost.Object;

        CommonCtorConfig();
    }

    private void CommonCtorConfig()
    {
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
                        { nameof(MetadataProviders.Tmdb), "111" }
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
                        { nameof(MetadataProviders.Tmdb), "111" }
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
                        { nameof(MetadataProviders.Tmdb), "111" }
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
                        { nameof(MetadataProviders.Tmdb), "111" }
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
                        { nameof(MetadataProviders.Tmdb), "111" }
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
                        { nameof(MetadataProviders.Tmdb), "111" }
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
                        { nameof(MetadataProviders.Tmdb), "222" }
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
                        { nameof(MetadataProviders.Tmdb), "222" }
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
                        { nameof(MetadataProviders.Tmdb), "987" }
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

        _ = _libraryManager // Get library by ID
            .Setup(m => m.GetItemById(It.IsInRange(2L, 4L, Moq.Range.Inclusive), It.IsAny<IDataContext>()))
            .Returns((long id, IDataContext _) => _librariesVault[id]);

        _ = _libraryManager // Get movie by ID
            .Setup(m => m.GetItemById(It.IsInRange(100L, 110L, Moq.Range.Inclusive), It.IsAny<IDataContext>()))
            .Returns((long id, IDataContext _) => _moviesVault[id]);

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

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var splitMoviesTask = new SplitMoviesTask(_libraryManager.Object, _logManager.Object);

        Plugin.Instance.Options.DoNotChangeLockedItems = true;

        using var cancellationTokenSource = new CancellationTokenSource();
        await splitMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // the main check
        _libraryManager.Verify(lm => lm.SplitItems(It.IsAny<BaseItem>()), Times.Exactly(4));

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), It.IsAny<IDataContext>()), Times.Exactly(27));
        _libraryManager.Verify(lm => lm.GetInternalItemIds(It.IsAny<InternalItemsQuery>()), Times.Exactly(33));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(9));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_DoNotChangeLockedItems_true"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_DoNotChangeLockedItems_true"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_DoNotChangeLockedItems_true/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

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

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var splitMoviesTask = new SplitMoviesTask(_libraryManager.Object, _logManager.Object);

        Plugin.Instance.Options.DoNotChangeLockedItems = false;

        using var cancellationTokenSource = new CancellationTokenSource();
        await splitMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // the main check
        _libraryManager.Verify(lm => lm.SplitItems(It.IsAny<BaseItem>()), Times.Exactly(8));

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), It.IsAny<IDataContext>()), Times.Exactly(27));
        _libraryManager.Verify(lm => lm.GetInternalItemIds(It.IsAny<InternalItemsQuery>()), Times.Exactly(17));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(9));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_DoNotChangeLockedItems_false"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_DoNotChangeLockedItems_false"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_DoNotChangeLockedItems_false/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

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

        var splitMoviesTask = new SplitMoviesTask(_libraryManager.Object, _logManager.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = splitMoviesTask.SplitMovies(nameof(MetadataProviders.Tmdb), "111");

        // the main check
        result.Should().BeTrue();
        _libraryManager.Verify(lm => lm.SplitItems(It.IsAny<BaseItem>()), Times.Exactly(6));

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetInternalItemIds(It.IsAny<InternalItemsQuery>()), Times.Exactly(6));

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

        var splitMoviesTask = new SplitMoviesTask(_libraryManager.Object, _logManager.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = splitMoviesTask.SplitMovies(nameof(MetadataProviders.Tmdb), "INVALID");

        // the main check
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

        var splitMoviesTask = new SplitMoviesTask(_libraryManager.Object, _logManager.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var result = splitMoviesTask.SplitMovies(nameof(MetadataProviders.Tmdb), "");

        // the main check
        result.Should().BeFalse();

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(SplitMovies_EmptyInput)}'");
    }

    [Fact]
    public void ForCodeCoverage()
    {
        Logger.Info($"Start '{nameof(ForCodeCoverage)}'");

        var splitMoviesTask = new SplitMoviesTask(_libraryManager.Object, _logManager.Object);

        splitMoviesTask.IsHidden.Should().BeFalse();
        splitMoviesTask.IsEnabled.Should().BeTrue();
        splitMoviesTask.IsLogged.Should().BeTrue();
        splitMoviesTask.Key.Should().NotBeNull();

        splitMoviesTask.GetDefaultTriggers().Should().BeEmpty();

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Once());
        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(ForCodeCoverage)}'");
    }

    [Fact]
    public void GetTranslation_RU()
    {
        Logger.Info($"Start '{nameof(GetTranslation_RU)}'");

        ServerConfigurationObject.UICulture = "ru";
        PluginResource.JsonSerializer = _jsonSerializer;
        PluginResource.ServerConfigurationManager = _serverConfigurationManager.Object;

        var name = PluginResource.ResourceManager.GetString("SplitMoviesTask_Name");
        var description = PluginResource.ResourceManager.GetString("SplitMoviesTask_Description");
        var category = PluginResource.ResourceManager.GetString("PluginTasks_Category");

        Assert.Equal("Разделение фильмов", name);
        Assert.Equal("Разделить все версии фильмов", description);
        Assert.Equal("Объединение фильмов", category);

        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_RU)}'");
    }


    [Fact]
    public void GetTranslation_EnUs()
    {
        Logger.Info($"Start '{nameof(GetTranslation_EnUs)}'");

        ServerConfigurationObject.UICulture = "en-us";
        PluginResource.JsonSerializer = _jsonSerializer;
        PluginResource.ServerConfigurationManager = _serverConfigurationManager.Object;

        var name = PluginResource.ResourceManager.GetString("SplitMoviesTask_Name");
        var description = PluginResource.ResourceManager.GetString("SplitMoviesTask_Description");
        var category = PluginResource.ResourceManager.GetString("PluginTasks_Category");

        Assert.Equal("Split movies", name);
        Assert.Equal("Split all versioned movies", description);
        Assert.Equal("Merge Movies", category);

        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_EnUs)}'");
    }

    [Fact]
    public void GetTranslation_BG()
    {
        Logger.Info($"Start '{nameof(GetTranslation_BG)}'");

        ServerConfigurationObject.UICulture = "bg";
        PluginResource.JsonSerializer = _jsonSerializer;
        PluginResource.ServerConfigurationManager = _serverConfigurationManager.Object;

        var name = PluginResource.ResourceManager.GetString("SplitMoviesTask_Name");
        var description = PluginResource.ResourceManager.GetString("SplitMoviesTask_Description");
        var category = PluginResource.ResourceManager.GetString("PluginTasks_Category");

        Assert.Equal("Split movies", name);
        Assert.Equal("Split all versioned movies", description);
        Assert.Equal("Merge Movies", category);

        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_EnUs)}'");
    }
}

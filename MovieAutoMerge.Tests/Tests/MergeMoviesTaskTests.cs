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
using MovieAutoMerge.UI;

namespace MovieAutoMerge.Tests.Tests;

public class MergeMoviesTaskTests : BaseTest
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(MergeMoviesTaskTests));

    public MergeMoviesTaskTests()
    {
        BaseItem.ConfigurationManager = _serverConfigurationManager.Object;
        BaseItem.FileSystem = _fileSystem.Object;
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.LocalizationManager = _localizationManager.Object;
        BaseItem.ItemRepository = _itemRepository.Object;
        BaseItem.ApplicationHost = _serverApplicationHost.Object;

        CommonConfig();
    }

    private void CommonConfig()
    {
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
                        { nameof(MetadataProviders.Tmdb), "112" }
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
                        { nameof(MetadataProviders.Tmdb), "tt211" },
                        { nameof(MetadataProviders.Imdb), "211" }
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
                        { nameof(MetadataProviders.Imdb), "222" }
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
                        { nameof(MetadataProviders.Imdb), "222" }
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
                        { nameof(MetadataProviders.Imdb), "222" }
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
                        { nameof(MetadataProviders.Tmdb), "212" }
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
                        { nameof(MetadataProviders.Tmdb), "112" }
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
                        { nameof(MetadataProviders.Tmdb), "tt311" },
                        { nameof(MetadataProviders.Imdb), "311" }
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
                        { nameof(MetadataProviders.Tmdb), "212" }
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
                && query.AncestorIds.Length == 2
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
                && query.AncestorIds.Length == 1
                && 2L == query.AncestorIds[0]
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
                && query.AncestorIds.Length == 1
                && 3L == query.AncestorIds[0]
                && true.Equals(query.Recursive)
                && true.Equals(query.HasPath)
                && false.Equals(query.IsVirtualItem))))
            .Returns(moviesVault.Values.Where(i => i.ParentId == 3L).ToArray());

        _ = _libraryManager // Get library by ID
            .Setup(m => m.GetItemById(It.IsInRange(2L, 4L, Moq.Range.Inclusive), It.IsAny<IDataContext>()))
            .Returns((long id, IDataContext _) => librariesVault[id]);

        _ = _libraryManager // Get movie by ID
            .Setup(m => m.GetItemById(It.IsInRange(100L, 110L, Moq.Range.Inclusive), It.IsAny<IDataContext>()))
            .Returns((long id, IDataContext _) => moviesVault[id]);

        _ = _libraryManager
            .SetupGet(m => m.RootFolderId)
            .Returns(1L);
    }

    [Fact]
    public async Task Execute_DefaultConfiguration_WithItems()
    {
        Logger.Info($"Start '{nameof(Execute_DefaultConfiguration_WithItems)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_DefaultConfiguration_WithItems));

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        Plugin.Instance.Options.SelectedLibraries = string.Empty;
        Plugin.Instance.Options.DoNotChangeLockedItems = true;
        Plugin.Instance.Options.MergeAcrossLibraries = true;
        Plugin.Instance.Options.SelectedProviders = MainPageUI.DefaultProviders;

        using var cancellationTokenSource = new CancellationTokenSource();
        await mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // main check
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Exactly(2));

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), null), Times.Exactly(21));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(7));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_DefaultConfiguration_WithItems"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_DefaultConfiguration_WithItems"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_DefaultConfiguration_WithItems/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_DefaultConfiguration_WithItems)}'");
    }

    [Fact]
    public async Task Execute_DefaultConfiguration_NoItems()
    {
        Logger.Info($"Start '{nameof(Execute_DefaultConfiguration_NoItems)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_DefaultConfiguration_NoItems));

        _ = _libraryManager // List all movies
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(query =>
                query.IncludeItemTypes.Length == 1
                && nameof(Movie).Equals(query.IncludeItemTypes[0], StringComparison.Ordinal)
                && query.MediaTypes.Length == 1
                && nameof(MediaType.Video).Equals(query.MediaTypes[0], StringComparison.Ordinal)
                && query.AncestorIds.Length == 2
                && true.Equals(query.Recursive)
                && true.Equals(query.HasPath)
                && false.Equals(query.IsVirtualItem))))
            .Returns([]);

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        Plugin.Instance.Options.SelectedLibraries = string.Empty;
        Plugin.Instance.Options.DoNotChangeLockedItems = true;
        Plugin.Instance.Options.MergeAcrossLibraries = true;
        Plugin.Instance.Options.SelectedProviders = MainPageUI.DefaultProviders;

        using var cancellationTokenSource = new CancellationTokenSource();
        await mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // main check
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Never());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_DefaultConfiguration_NoItems"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_DefaultConfiguration_NoItems"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_DefaultConfiguration_NoItems/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_DefaultConfiguration_NoItems)}'");
    }

    [Fact]
    public async Task Execute_WithProvider()
    {
        Logger.Info($"Start '{nameof(Execute_WithProvider)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_WithProvider));

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        // main condition
        Plugin.Instance.Options.SelectedProviders = nameof(MetadataProviders.Tmdb);

        Plugin.Instance.Options.SelectedLibraries = string.Empty;
        Plugin.Instance.Options.DoNotChangeLockedItems = true;
        Plugin.Instance.Options.MergeAcrossLibraries = true;

        using var cancellationTokenSource = new CancellationTokenSource();
        await mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // mian check
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Once());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), null), Times.Exactly(21));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(7));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_WithProvider"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_WithProvider"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_WithProvider/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_WithProvider)}'");
    }

    [Fact]
    public async Task Execute_WithLibrary()
    {
        Logger.Info($"Start '{nameof(Execute_WithLibrary)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_WithLibrary));

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        // main condition
        Plugin.Instance.Options.SelectedLibraries = "2";

        Plugin.Instance.Options.SelectedProviders = MainPageUI.DefaultProviders;
        Plugin.Instance.Options.DoNotChangeLockedItems = true;
        Plugin.Instance.Options.MergeAcrossLibraries = true;

        using var cancellationTokenSource = new CancellationTokenSource();
        await mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // mian check
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Once());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once());
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), null), Times.Exactly(15));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(5));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_WithLibrary"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_WithLibrary"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_WithLibrary/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_WithLibrary)}'");
    }

    [Fact]
    public async Task Execute_DoNotChangeLockedItems_False()
    {
        Logger.Info($"Start '{nameof(Execute_DoNotChangeLockedItems_False)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_DoNotChangeLockedItems_False));

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        // main condition
        Plugin.Instance.Options.DoNotChangeLockedItems = false;

        Plugin.Instance.Options.SelectedLibraries = string.Empty;
        Plugin.Instance.Options.MergeAcrossLibraries = true;
        Plugin.Instance.Options.SelectedProviders = MainPageUI.DefaultProviders;

        using var cancellationTokenSource = new CancellationTokenSource();
        await mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // main check
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Exactly(3));

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), null), Times.Exactly(30));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(10));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_DoNotChangeLockedItems_False"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_DoNotChangeLockedItems_False"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_DoNotChangeLockedItems_False/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_DoNotChangeLockedItems_False)}'");
    }

    [Fact]
    public async Task Execute_MergeAcrossLibraries_False()
    {
        Logger.Info($"Start '{nameof(Execute_MergeAcrossLibraries_False)}'");

        _ = _applicationPaths
            .SetupGet(m => m.PluginConfigurationsPath)
            .Returns(nameof(Execute_MergeAcrossLibraries_False));

        _ = new Plugin(_serverApplicationHost.Object, _logManager.Object);
        Plugin.Instance.SetAttributes("MovieAutoMerge.dll", string.Empty, new Version(1, 0, 0));
        ServerConfigurationObject.UICulture = "en-us";
        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        // main condition
        Plugin.Instance.Options.MergeAcrossLibraries = false;

        Plugin.Instance.Options.DoNotChangeLockedItems = true;
        Plugin.Instance.Options.SelectedLibraries = string.Empty;
        Plugin.Instance.Options.SelectedProviders = MainPageUI.DefaultProviders;

        using var cancellationTokenSource = new CancellationTokenSource();
        await mergeMoviesTask.Execute(cancellationTokenSource.Token, new EmbyProgress());

        // main check
        _libraryManager.Verify(lm => lm.MergeItems(It.IsAny<BaseItem[]>()), Times.Once());

        _logManager.Verify(lm => lm.GetLogger("Movie Auto Merge"), Times.Exactly(2));
        _applicationPaths.VerifyGet(ap => ap.PluginConfigurationsPath, Times.Once());
        _libraryManager.Verify(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Exactly(4));
        _libraryManager.Verify(lm => lm.GetItemById(It.IsInRange(1L, 3L, Moq.Range.Inclusive), null), Times.Exactly(21));
        _libraryManager.VerifyGet(lm => lm.RootFolderId, Times.Exactly(7));
        _fileSystem.Verify(fs => fs.DirectoryExists("Execute_MergeAcrossLibraries_False"), Times.Once());
        _fileSystem.Verify(fs => fs.CreateDirectory("Execute_MergeAcrossLibraries_False"), Times.Once());
        _fileSystem.Verify(fs => fs.FileExists("Execute_MergeAcrossLibraries_False/MovieAutoMerge.json"), Times.Once());
        _serverApplicationHost.Verify(sah => sah.Resolve<IJsonSerializer>(), Times.Exactly(2));
        _serverApplicationHost.Verify(sah => sah.Resolve<IServerConfigurationManager>(), Times.Once());
        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(Execute_MergeAcrossLibraries_False)}'");
    }

    [Fact]
    public void ForCodeCoverage()
    {
        Logger.Info($"Start '{nameof(ForCodeCoverage)}'");

        var mergeMoviesTask = new MergeMoviesTask(_libraryManager.Object, _logManager.Object);

        mergeMoviesTask.IsHidden.Should().BeFalse();
        mergeMoviesTask.IsEnabled.Should().BeTrue();
        mergeMoviesTask.IsLogged.Should().BeTrue();
        mergeMoviesTask.Key.Should().NotBeNull();

        mergeMoviesTask.GetDefaultTriggers().Should().BeEmpty();

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

        var name = PluginResource.ResourceManager.GetString("MergeMoviesTask_Name");
        var description = PluginResource.ResourceManager.GetString("MergeMoviesTask_Description");
        var category = PluginResource.ResourceManager.GetString("PluginTasks_Category");

        Assert.Equal("Объединение фильмов", name);
        Assert.Equal("Объединить все версии фильмов согласно настройкам", description);
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

        var name = PluginResource.ResourceManager.GetString("MergeMoviesTask_Name");
        var description = PluginResource.ResourceManager.GetString("MergeMoviesTask_Description");
        var category = PluginResource.ResourceManager.GetString("PluginTasks_Category");

        Assert.Equal("Merge movies", name);
        Assert.Equal("Merge all versioned movies based on configuration", description);
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

        var name = PluginResource.ResourceManager.GetString("MergeMoviesTask_Name");
        var description = PluginResource.ResourceManager.GetString("MergeMoviesTask_Description");
        var category = PluginResource.ResourceManager.GetString("PluginTasks_Category");

        Assert.Equal("Merge movies", name);
        Assert.Equal("Merge all versioned movies based on configuration", description);
        Assert.Equal("Merge Movies", category);

        _serverConfigurationManager.VerifyGet(scm => scm.Configuration, Times.AtMost(3));

        VerifyNoOtherCalls();

        Logger.Info($"Finished '{nameof(GetTranslation_EnUs)}'");
    }
}

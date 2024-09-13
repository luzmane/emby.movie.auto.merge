using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using MovieAutoMerge.Configuration;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MovieAutoMerge
{
    /// <summary>
    /// The main plugin.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Plugin : BasePlugin<PluginConfiguration>, IHasThumbImage, IHasWebPages, IHasTranslations
    {
        internal const string PluginName = "Movie Auto Merge";

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => PluginName;

        /// <inheritdoc />
        public override string Description => "Auto merge movies based on provider id";

        /// <inheritdoc />
        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        /// <inheritdoc />
        public override Guid Id => new Guid("7f6902cc-a3ba-40d9-868f-98f73291fdf7");


        internal readonly ILibraryManager LibraryManager;


        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="libraryManager"></param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            SetId(new Guid("7f6902cc-a3ba-40d9-868f-98f73291fdf7"));
            LibraryManager = libraryManager;
        }

        /// <inheritdoc />
        public Stream GetThumbImage()
        {
            Type type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "MovieAutoMerge",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.MovieAutoMerge.html"
                },
                new PluginPageInfo
                {
                    Name = "MovieAutoMergeJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.MovieAutoMerge.js"
                }
            };
        }

        /// <inheritdoc />
        public TranslationInfo[] GetTranslations()
        {
            var basePath = GetType().Namespace + ".i18n.Configuration.";
            return GetType().Assembly.GetManifestResourceNames()
                .Where(i => i.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                .Select(i =>
                    new TranslationInfo
                    {
                        Locale = Path.GetFileNameWithoutExtension(i.Substring(basePath.Length)),
                        EmbeddedResourcePath = i
                    })
                .ToArray();
        }
    }
}

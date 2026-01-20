using System.Collections.Generic;
using System.ComponentModel;

using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;

using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;

using MovieAutoMerge.I18n;

namespace MovieAutoMerge.UI
{
    public class MainPageUI : EditableOptionsBase
    {
        internal const string SplitButtonId = "SplitButton";
        internal const string DefaultProviders = "KinopoiskRu,Imdb,Tmdb";

        public override string EditorTitle => Plugin.PluginName;
        public override string EditorDescription => "";

        public CaptionItem MergeItemsSection { get; set; }

        [DisplayNameL("MainPageUI_Merge_Across_Libraries", typeof(PluginResource))]
        [DescriptionL("MainPageUI_Merge_Across_Libraries_Desc", typeof(PluginResource))]
        public bool MergeAcrossLibraries { get; set; } = true;

        [DisplayNameL("MainPageUI_Do_Not_Change_Locked_Items", typeof(PluginResource))]
        [DescriptionL("MainPageUI_Do_Not_Change_Locked_Items_Desc", typeof(PluginResource))]
        public bool DoNotChangeLockedItems { get; set; } = true;

        [DisplayNameL("MainPageUI_Run_Automatically", typeof(PluginResource))]
        [DescriptionL("MainPageUI_Run_Automatically_Desc", typeof(PluginResource))]
        public bool RunAutomatically { get; set; } = true;

        [Browsable(false)]
        public List<EditorSelectOption> Providers { get; set; }

        [DisplayNameL("MainPageUI_Providers", typeof(PluginResource))]
        [DescriptionL("MainPageUI_Providers_Desc", typeof(PluginResource))]
        [EditMultilSelect]
        [SelectItemsSource(nameof(Providers))]
        public string SelectedProviders { get; set; } = DefaultProviders;

        [Browsable(false)]
        public List<EditorSelectOption> Libraries { get; set; }

        [DisplayNameL("MainPageUI_Libraries", typeof(PluginResource))]
        [DescriptionL("MainPageUI_Libraries_Desc", typeof(PluginResource))]
        [EditMultilSelect]
        [SelectItemsSource(nameof(Libraries))]
        public string SelectedLibraries { get; set; } = string.Empty;

        public SpacerItem SpacerBeforeSplit { get; set; } = new SpacerItem(SpacerSize.Medium);

        public CaptionItem SplitMoviesSection { get; set; }

        [DisplayNameL("MainPageUI_Provider_Name_To_Split", typeof(PluginResource))]
        [DescriptionL("MainPageUI_Provider_Name_To_Split_Desc", typeof(PluginResource))]
        [SelectItemsSource(nameof(Providers))]
        public string SplitProviderType { get; set; } = string.Empty;

        [DisplayNameL("MainPageUI_ProviderId_To_Split", typeof(PluginResource))]
        [DescriptionL("MainPageUI_ProviderId_To_Split_Desc", typeof(PluginResource))]
        public string SplitProviderId { get; set; } = string.Empty;

        public StatusItem SplitStatusItem { get; set; } = new StatusItem(
            PluginResource.ResourceManager.GetString("MainPageUI_Movie_Split_Status"),
            PluginResource.ResourceManager.GetString("MainPageUI_Operation_Not_Started_Yet"),
            ItemStatus.Unavailable);

        public ButtonItem SplitMoviesButton { get; set; } = new ButtonItem(PluginResource.ResourceManager.GetString("MainPageUI_Split"))
        {
            Icon = IconNames.call_split,
            Data1 = SplitButtonId
        };

    }
}

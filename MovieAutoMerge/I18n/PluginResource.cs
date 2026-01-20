using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Serialization;

namespace MovieAutoMerge.I18n
{
    public class PluginResource
    {
        private static PluginResourceManager s_resourceMan;

        internal static IJsonSerializer JsonSerializer { get; set; }
        internal static IServerConfigurationManager ServerConfigurationManager { get; set; }

        public static PluginResourceManager ResourceManager
        {
            get
            {
                return s_resourceMan ?? (s_resourceMan = new PluginResourceManager(JsonSerializer, ServerConfigurationManager));
            }
        }

        public static string MainPageUI_Merge_Movies => ResourceManager.GetString("MainPageUI_Merge_Movies");

        public static string MainPageUI_Merge_Across_Libraries => ResourceManager.GetString("MainPageUI_Merge_Across_Libraries");

        public static string MainPageUI_Merge_Across_Libraries_Desc => ResourceManager.GetString("MainPageUI_Merge_Across_Libraries_Desc");

        public static string MainPageUI_Do_Not_Change_Locked_Items => ResourceManager.GetString("MainPageUI_Do_Not_Change_Locked_Items");

        public static string MainPageUI_Do_Not_Change_Locked_Items_Desc => ResourceManager.GetString("MainPageUI_Do_Not_Change_Locked_Items_Desc");

        public static string MainPageUI_Run_Automatically => ResourceManager.GetString("MainPageUI_Run_Automatically");

        public static string MainPageUI_Run_Automatically_Desc => ResourceManager.GetString("MainPageUI_Run_Automatically_Desc");

        public static string MainPageUI_Providers => ResourceManager.GetString("MainPageUI_Providers");

        public static string MainPageUI_Providers_Desc => ResourceManager.GetString("MainPageUI_Providers_Desc");

        public static string MainPageUI_Libraries => ResourceManager.GetString("MainPageUI_Libraries");

        public static string MainPageUI_Libraries_Desc => ResourceManager.GetString("MainPageUI_Libraries_Desc");

        public static string MainPageUI_Split_Movies => ResourceManager.GetString("MainPageUI_Split_Movies");

        public static string MainPageUI_Provider_Name_To_Split => ResourceManager.GetString("MainPageUI_Provider_Name_To_Split");

        public static string MainPageUI_Provider_Name_To_Split_Desc => ResourceManager.GetString("MainPageUI_Provider_Name_To_Split_Desc");

        public static string MainPageUI_ProviderId_To_Split => ResourceManager.GetString("MainPageUI_ProviderId_To_Split");

        public static string MainPageUI_ProviderId_To_Split_Desc => ResourceManager.GetString("MainPageUI_ProviderId_To_Split_Desc");

        public static string MainPageUI_Movie_Split_Status => ResourceManager.GetString("MainPageUI_Movie_Split_Status");

        public static string MainPageUI_Operation_Not_Started_Yet => ResourceManager.GetString("MainPageUI_Operation_Not_Started_Yet");

        public static string MainPageUI_Split => ResourceManager.GetString("MainPageUI_Split");

        public static string MainPageUI_Provider_Name_And_Provider_Id_Are_Required => ResourceManager.GetString("MainPageUI_Provider_Name_And_Provider_Id_Are_Required");

        public static string MainPageUI_Work_In_Progress => ResourceManager.GetString("MainPageUI_Work_In_Progress");

        public static string MainPageUI_Operation_Completed_Successfully => ResourceManager.GetString("MainPageUI_Operation_Completed_Successfully");

        public static string MainPageUI_Such_Item_Not_Found => ResourceManager.GetString("MainPageUI_Such_Item_Not_Found");

        public static string MergeMoviesTask_Name => ResourceManager.GetString("MergeMoviesTask_Name");

        public static string MergeMoviesTask_Description => ResourceManager.GetString("MergeMoviesTask_Description");

        public static string PluginTasks_Category => ResourceManager.GetString("PluginTasks_Category");

        public static string SplitMoviesTask_Name => ResourceManager.GetString("SplitMoviesTask_Name");

        public static string SplitMoviesTask_Description => ResourceManager.GetString("SplitMoviesTask_Description");
    }
}

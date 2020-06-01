using Microsoft.Extensions.Configuration;

namespace DevelopingInsanity.KDM.kdacli
{
    public class AppSettings
    {
        public string SASToken { get; set; }
        
        public string StorageAccountName { get; set; }
        
        public static AppSettings LoadAppSettings()
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder().AddJsonFile("AppSettings.json").Build();
            AppSettings appSettings = configRoot.Get<AppSettings>();
            return appSettings;
        }       
    }
}
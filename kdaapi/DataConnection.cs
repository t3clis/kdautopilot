using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevelopingInsanity.KDM.kdaapi
{
    public static class DataConnection
    {
        private const string KEYVAULT_PATH = "https://kdap.vault.azure.net/";
        private const string SECRET_NAME = "kdap-storage-key";
        private const string STORAGE_ACCOUNT_NAME = "kdautopilot";

        private static CloudStorageAccount _storageAccount = null;
        private static CloudTableClient _cloudTableClient = null;
        private static object _syncLock = new object();

        public static CloudStorageAccount StorageAccount
        {
            get
            {
                lock (_syncLock)
                {
                    if (_storageAccount == null)
                    {
                        _storageAccount = CreateStorageAccount();
                    }

                    return _storageAccount;
                }
            }
        }

        public static CloudTableClient TableClient
        {
            get 
            {
                lock (_syncLock)
                {
                    if (_cloudTableClient == null)
                    {
                        _cloudTableClient = StorageAccount.CreateCloudTableClient(new TableClientConfiguration());
                    }

                    return _cloudTableClient;
                }
            }
        }

        private static CloudStorageAccount CreateStorageAccount()
        {
            string storageAccountPrimaryKey = GetStorageAccountPrimaryKey();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={STORAGE_ACCOUNT_NAME};AccountKey={storageAccountPrimaryKey}";
            return CloudStorageAccount.Parse(connectionString);
        }

        private static string GetStorageAccountPrimaryKey()
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
            {
                Delay= TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                MaxRetries = 5,
                Mode = RetryMode.Exponential
             }
            };
            
            var client = new SecretClient(new Uri(KEYVAULT_PATH), new DefaultAzureCredential(), options);
            
            KeyVaultSecret secret = client.GetSecret(SECRET_NAME);

            return secret.Value;
        }
    }
}

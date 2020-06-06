using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace DevelopingInsanity.KDM.kdacli
{
    public class Common
    {
        public static CloudStorageAccount CreateStorageAccountFromSASToken(string SASToken, string accountName)
        {
            CloudStorageAccount storageAccount = null;

            Trace.TraceInformation($"Creating a storage account from name {accountName} and SAS token <secret>");

            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(SASToken);
                storageAccount = new CloudStorageAccount(storageCredentials, accountName, null, true);
                Trace.TraceInformation("Storage account created");
            }
            catch (FormatException fex)
            {
                Trace.TraceError($"Invalid Storage Account information provided. Please confirm the SAS token is valid and did not expire\n{fex}");
                throw;
            }
            catch (ArgumentException aex)
            {
                Trace.TraceError($"Invalid Storage Account information provided. Please confirm the SAS token is valid and did not expire\n{aex}");
                throw;
            }

            
            return storageAccount;
        }

        public static async Task<CloudTable> CreateTableAsync(CloudStorageAccount storageAccount, string tableName)
        {
            CloudTableClient tableClient = null;
            CloudTable table = null;

            Trace.TraceInformation($"Creating table (if not exists) {tableName}"); 
            
            tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            table = tableClient.GetTableReference(tableName);

            if (await table.CreateIfNotExistsAsync())
            {
                Trace.TraceInformation($"Table {tableName} did not exist and was created");
            }
            else
            {
                Trace.TraceInformation($"Table {tableName} already existed, reference retrieved");
            }

            return table;
        }
    }
}

using System;
using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace DevelopingInsanity.KDM.kdacli
{
    public class TableUtils<T> where T : TableEntity
    {
        private CloudTable _table;

        public TableUtils(CloudTable table)
        {
            _table = table;
        }

        public async Task<T> InsertOrMergeEntityAsync(T entity, bool replaceInsteadOfMerge)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            try
            {
                TableOperation op;
                TableResult result;
                T insertedEntity;

                if (replaceInsteadOfMerge)
                    op = TableOperation.InsertOrReplace(entity);
                else
                    op = TableOperation.InsertOrMerge(entity);

                result = await _table.ExecuteAsync(op);
                insertedEntity = result.Result as T;

                Trace.TraceInformation($"{(replaceInsteadOfMerge ? "Insert/Replace" : "Insert/Merge")} of {typeof(T)}({entity.PartitionKey}, {entity.RowKey}) completed");

                return insertedEntity;
            }
            catch (StorageException sex)
            {
                Trace.TraceError($"Error during Insert/Merge of {typeof(T)}({entity.PartitionKey}, {entity.RowKey})\n{sex}");
                throw;
            }
        }

        public async Task<TableBatchResult> BatchInsertOrMergeEntityAsync(IList<T> entities, bool replaceInsteadOfMerge)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");

            try
            {
                TableBatchOperation batchOp = new TableBatchOperation();
                TableBatchResult batchRes;

                foreach (T entity in entities)
                {
                    if (replaceInsteadOfMerge)
                        batchOp.InsertOrReplace(entity);
                    else
                        batchOp.InsertOrMerge(entity);

                    Trace.TraceInformation($"Batch {(replaceInsteadOfMerge ? "insert/replace" : "insert/merge")} of {typeof(T)}({entity.PartitionKey}, {entity.RowKey}) scheduled");

                }

                batchRes = await _table.ExecuteBatchAsync(batchOp);

                Trace.TraceInformation($"Batch {(replaceInsteadOfMerge ? "insert/replace" : "insert/merge")} of {entities.Count} {typeof(T)} entities completed");

                return batchRes;

            }
            catch (StorageException sex)
            {
                Trace.TraceError($"Error during execution of batch operation:\n{sex}");
                throw;
            }
        }

        public async Task<T> RetrieveEntityUsingPointQueryAsync(string partitionKey, string rowKey)
        {
            try
            {
                TableOperation queryOp = TableOperation.Retrieve<T>(partitionKey, rowKey);
                TableResult result = await _table.ExecuteAsync(queryOp);

                Trace.TraceInformation($"Point Query for {typeof(T)}({partitionKey},{rowKey}) returned {(result.Result == null ? "no results" : "one result")}");

                T entity = result.Result as T;
                return entity;
            }
            catch (StorageException sex)
            {
                Trace.TraceError($"Error while executing point query for {typeof(T)}({partitionKey}, {rowKey}): \n{sex}");
                throw;
            }
        }

        public async Task DeleteEntityAsync(T entityToDelete)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException("entityToDelete");

            try
            {
                TableOperation deleteOp = TableOperation.Delete(entityToDelete);
                TableResult result = await _table.ExecuteAsync(deleteOp);

                Trace.TraceInformation($"Issued a delete for entity {typeof(T)}({entityToDelete.PartitionKey}, {entityToDelete.RowKey})");
            }
            catch (StorageException sex)
            {
                Trace.TraceError($"Error while trying to delete entity {typeof(T)}({entityToDelete.PartitionKey},{entityToDelete.RowKey}):\n{sex}");
                throw;
            }
        }
    }
}

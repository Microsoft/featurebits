﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace FeatureBits.Data.AzureTableStorage
{
    public class FeatureBitsTableStorageRepo : IFeatureBitsRepo
    {
        private readonly CloudTable _table;
        public FeatureBitsTableStorageRepo(string connectionString, string tableName)
        {
            _table = CloudStorageAccount.Parse(connectionString)
                .CreateCloudTableClient()
                .GetTableReference(tableName);
            _table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }
        public async Task<IEnumerable<IFeatureBitDefinition>> GetAllAsync()
        {
            TableContinuationToken token = null;
            var results = new List<FeatureBitTableDefinition>();
            TableQuery<FeatureBitTableDefinition> query = new TableQuery<FeatureBitTableDefinition>();
            do
            {
                var resultSegment = await _table.ExecuteQuerySegmentedAsync(query, token);
                results.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;

            } while (token != null);
            return results.Select(x => (IFeatureBitDefinition)x).ToList();
        }

        public async Task<IFeatureBitDefinition> AddAsync(IFeatureBitDefinition definition)
        {
            if (await GetExistingFeatureBit(definition) != null)
            {
                throw new DataException($"Cannot add. Feature bit with name '{definition.Name}' already exists.");
            }
            definition.Id = await GetNextId();
            var insertOp = TableOperation.Insert(definition.ToTableDefinition(_table.Name));
            var result = await _table.ExecuteAsync(insertOp);
            return (IFeatureBitDefinition)result.Result;
        }

        public async Task UpdateAsync(IFeatureBitDefinition definition)
        {
            var existing = await GetExistingFeatureBit(definition);
            if (existing == null)
            {
                throw new DataException($"Could not update.  Feature bit with name '{definition.Name}' does not exist");
            }
            existing.Update(definition);
            var replaceOp = TableOperation.Replace(existing.ToTableDefinition(_table.Name));
            await _table.ExecuteAsync(replaceOp);
        }

        public async Task RemoveAsync(IFeatureBitDefinition definition)
        {
            var existing = await GetExistingFeatureBit(definition);
            if (existing != null)
            {
                await _table.ExecuteAsync(TableOperation.Delete(definition.ToTableDefinition(_table.Name)));
            }
        }

        private async Task<IFeatureBitDefinition> GetExistingFeatureBit(IFeatureBitDefinition definition)
        {
            var tableDefinition = definition.ToTableDefinition(_table.Name);
            var tableResult = await _table.ExecuteAsync(TableOperation.Retrieve<FeatureBitTableDefinition>(tableDefinition.PartitionKey, tableDefinition.RowKey));
            return (IFeatureBitDefinition)tableResult.Result;
        }

        private async Task<int> GetNextId()
        {
            TableContinuationToken token = null;
            TableQuery<DynamicTableEntity> projectionQuery = new TableQuery<DynamicTableEntity>().Select(new string[] { "Id" });
            EntityResolver<int> resolver = (pk, rk, ts, props, etag) => props.ContainsKey("Id") ? props["Id"].Int32Value.GetValueOrDefault() : -1;

            var maxInt = -1;
            do
            {
                var resultSegment = await _table.ExecuteQuerySegmentedAsync(projectionQuery, resolver, token, null, null);
                var currentMax = resultSegment.Results.Max();
                maxInt = Math.Max(currentMax, maxInt);
                token = resultSegment.ContinuationToken;
            }
            while (token != null);
            return maxInt + 1;
        }

        public async Task<IFeatureBitDefinition> GetByNameAsync(string featureBitName)
        {
            var tableResult = await _table.ExecuteAsync(TableOperation.Retrieve<FeatureBitTableDefinition>(_table.Name, featureBitName));
            return (IFeatureBitDefinition)tableResult.Result;
        }
    }
}

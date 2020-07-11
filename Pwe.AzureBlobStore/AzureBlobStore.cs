using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pwe.AzureBloBStore
{
    public class AzureBlobStoreService : IBlobStoreService
    {
        public const string ContainerName = "maps";

        private readonly CloudStorageAccount _account;
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _container;

        public AzureBlobStoreService(IConfiguration config)
        {
            string connectionString = config["BlobConnectionString"];
            if (!CloudStorageAccount.TryParse(connectionString, out _account))
                throw new ArgumentException($"Could not parse connection string");

            _client = _account.CreateCloudBlobClient();
            _client.DefaultRequestOptions = new BlobRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 5),
                MaximumExecutionTime = TimeSpan.FromMinutes(2),
                ServerTimeout = TimeSpan.FromMinutes(3),
            };

            _container = _client.GetContainerReference(ContainerName);
        }

        public async Task<bool> Exists(string path)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
            return await blockBlob.ExistsAsync();
        }

        public async Task<bool> Delete(string path)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
            return await blockBlob.DeleteIfExistsAsync();
        }

        public async Task<string> GetText(string path)
        {
            var bytes = await InternalGetBlobAsync(path);
            return Encoding.UTF8.GetString(bytes);
        }

        public async Task StoreText(string path, string text, bool overwriteExisting = true)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await InternalStoreBlobAsync(path, bytes, overwriteExisting);
        }

        public async Task<IEnumerable<string>> GetBlobsInFolder(string path, bool includeSubfolders = false, bool returnFullPath = false)
        {
            CloudBlobDirectory directory = _container.GetDirectoryReference(path);
            var blobs = directory.ListBlobs(useFlatBlobListing: includeSubfolders).OfType<CloudBlockBlob>().Cast<CloudBlockBlob>().ToList();
            var names = blobs.Select(b => returnFullPath ? b.Name : GetSubfoldersAndFilenameFromFullPath(path, b.Name));
            return await Task.FromResult(names);
        }

        async Task<byte[]> InternalGetBlobAsync(string path)
        {
            try
            {
                CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
                var result = new MemoryStream();
                await blockBlob.DownloadToStreamAsync(result);
                return result.ToArray();
            }
            catch (StorageException e)
            {
                throw new ArgumentException($"No blob found at path {path}", e);
            }
        }

        async Task InternalStoreBlobAsync(string path, byte[] bytes, bool overwriteExisting = true)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
            if (!overwriteExisting && await blockBlob.ExistsAsync())
                throw new ArgumentException($"Path already exists: {path}");

            await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// If root path is   :             myRoot/
        /// and full path is  : [container]/myRoot/123/abc/myFile.txt
        /// this will return  :                    123/abc/myFile.txt
        /// </summary>
        string GetSubfoldersAndFilenameFromFullPath(string rootPath, string fullPath)
        {
            int rootIdx = fullPath.IndexOf(rootPath);
            return fullPath.Substring(rootIdx + rootPath.Length);
        }
    }
}

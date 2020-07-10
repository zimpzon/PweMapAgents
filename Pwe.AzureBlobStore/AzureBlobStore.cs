using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
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
        public const string ContainerName = "pinfo-store-v1";

        CloudStorageAccount _account;
        CloudBlobClient _client;
        CloudBlobContainer _container;
        BlobContainerPermissions _permissions;

        public AzureBlobStoreService(ISecretSettingsService secretSettings)
        {
            System.Configuration.
            string connectionString = secretSettings.GetSecret(SecretSettingsKeys.BlobStoreConnectionString);

            if (!CloudStorageAccount.TryParse(connectionString, out _account))
                throw new ArgumentException($"Could not parse connection string. Please verify secret setting {nameof(SecretSettingsKeys.BlobStoreConnectionString)}");

            _client = _account.CreateCloudBlobClient();
            _client.DefaultRequestOptions = new BlobRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 5),
                MaximumExecutionTime = TimeSpan.FromMinutes(2),
                ServerTimeout = TimeSpan.FromMinutes(3),
            };

            _container = _client.GetContainerReference(ContainerName);
        }

        public async Task<bool> DeleteBlobAsync(string path)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
            return await blockBlob.DeleteIfExistsAsync();
        }

        public async Task<string> GetText(string path)
        {
            var bytes = await InternalGetBlobAsync(path);
            return Encoding.UTF8.GetString(bytes);
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

        public async Task StoreTextBlob(string path, string text, bool overwriteExisting = true)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await InternalStoreBlobAsync(path, bytes, overwriteExisting);
        }

        async Task InternalStoreBlobAsync(string path, byte[] bytes, bool overwriteExisting = true)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
            if (!overwriteExisting && await blockBlob.ExistsAsync())
                throw new ArgumentException($"Path already exists: {path}");

            await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
        }

        public async Task<bool> BlobExistsAsync(string path)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(path);
            return await blockBlob.ExistsAsync();
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

        public async Task<IEnumerable<string>> GetBlobsInFolder(string path, bool includeSubfolders = false, bool returnFullPath = false)
        {
            CloudBlobDirectory directory = _container.GetDirectoryReference(path);
            var blobs = directory.ListBlobs(useFlatBlobListing: includeSubfolders).OfType<CloudBlockBlob>().Cast<CloudBlockBlob>().ToList();
            var names = blobs.Select(b => returnFullPath ? b.Name : GetSubfoldersAndFilenameFromFullPath(path, b.Name));
            return await Task.FromResult(names);
        }
    }
}

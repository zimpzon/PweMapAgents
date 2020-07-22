using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.AzureBloBStore
{
    public interface IBlobStoreService
    {
        Task<bool> Exists(string path);
        Task<bool> Delete(string path);
        Task<string> GetText(string path, bool throwIfNotFound = true);
        Task<byte[]> GetBytes(string path, bool throwIfNotFound = true);
        Task StoreText(string path, string text, bool overwriteExisting = true);
        Task StoreBytes(string path, byte[] bytes, bool overwriteExisting = true);
        Task<IEnumerable<string>> GetBlobsInFolder(string path, bool includeSubfolders = false, bool returnFullPath = false);
    }
}

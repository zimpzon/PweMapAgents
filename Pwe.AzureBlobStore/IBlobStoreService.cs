using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.AzureBloBStore
{
    internal interface IBlobStoreService
    {
        Task<bool> Exists(string path);
        Task<bool> Delete(string path);
        Task<string> GetText(string path);
        Task StoreText(string path, string text, bool overwriteExisting = true);
        Task<IEnumerable<string>> GetBlobsInFolder(string path, bool includeSubfolders = false, bool returnFullPath = false);
    }
}

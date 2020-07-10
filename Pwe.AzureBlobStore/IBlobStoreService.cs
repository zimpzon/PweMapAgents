using System.Threading.Tasks;

namespace Pwe.AzureBloBStore
{
    internal interface IBlobStoreService
    {
        Task<string> GetText(string path);
        Task StoreText(string path, string text);
    }
}

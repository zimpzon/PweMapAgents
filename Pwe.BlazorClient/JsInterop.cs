using Microsoft.JSInterop;

namespace Pwe.BlazorClient
{
    public static class JsInterop
    {
        public static void SetPath(IJSRuntime jsRuntime, string mapId, string encodedPath, long ms)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid ("leafletanimated_setpath", mapId, encodedPath, ms);
        }

        public static void DebugLog(IJSRuntime jsRuntime, string msg)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("debuglog", msg);
        }
    }
}

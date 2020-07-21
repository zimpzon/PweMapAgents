using Microsoft.JSInterop;
using System;

namespace Pwe.BlazorClient
{
    public static class JsInterop
    {
        public static void InitMap(IJSRuntime jsRuntime, string mapId)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("initMap", mapId);
        }

        public static void SetView(IJSRuntime jsRuntime, double lat, double lon, double zoom)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("setview", lat, lon, zoom);
        }

        public static void UpdateMarker(IJSRuntime jsRuntime, double lat, double lon)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("updateMarker", lat, lon);
        }

        public static void DebugLog(IJSRuntime jsRuntime, string msg)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("debuglog", msg);
        }

        // Events
        public static Action OnMapZoomBegin;
        public static Action OnMapZoomEnd;

        [JSInvokable]
        public static void MapZoomBegin()
        {
            OnMapZoomBegin?.Invoke();
        }

        [JSInvokable]
        public static void MapZoomEnd()
        {
            OnMapZoomEnd?.Invoke();
        }
    }
}

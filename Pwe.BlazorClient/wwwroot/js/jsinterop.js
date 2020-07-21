var map;
var isZooming;
var icon;
var marker;

window.debuglog = (msg) => { console.log(msg); }

window.initMap = (mapId) => {
    map = L.map(mapId);
    L.tileLayer('http://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>',
        maxZoom: 17,
        id: 'OSM'
    }).addTo(map);

    icon = L.icon({
        iconUrl: 'cactus.png',
        iconSize: [40, 40],
        iconAnchor: [20, 40],
        className: 'markerClass'
    });

    map.on("zoomstart", function (e) { isZooming = true; DotNet.invokeMethod("Pwe.BlazorClient", "MapZoomBegin"); });
    map.on("zoomend", function (e) { isZooming = false; DotNet.invokeMethod("Pwe.BlazorClient", "MapZoomEnd"); });
}

window.setview = (lat, lon, zoom) => {
    map.setView([lat, lon], zoom);
}

window.updateMarker = (lat, lon) => {
    if (marker == undefined) {
        marker = L.marker([lat, lon], { icon: icon }).addTo(map);
    }
    marker.setLatLng([lat, lon]);
}

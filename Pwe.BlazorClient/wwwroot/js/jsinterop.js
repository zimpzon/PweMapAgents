var map;
var isZooming;
var icon;
var marker;
var mapTileLayer;
var coverageTileLayer;

window.debuglog = (msg) => { console.log(msg); }

window.initMap = (mapId) => {
    map = L.map(mapId);
    mapTileLayer = L.tileLayer('http://a.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>',
        maxZoom: 17,
        id: 'OSM'
    }).addTo(map);

    coverageTileLayer = L.tileLayer('https://maps0pwe0sa.blob.core.windows.net/maps/coveragetiles/{x}-{y}-{z}.png', {
        attribution: null,
        maxZoom: 17,
        transparent: true,
        zoomOffset: 0,
        tileSize: 256,
        className: 'coveragelayer',
        id: 'COVRERAGELAYER'
    }).addTo(map);

    var overlayMap = {
        "Kort": mapTileLayer
    };

    var overlayCoverage = {
        "Kort": mapTileLayer,
        "Udforsket": coverageTileLayer
    };

    L.control.layers(null, overlayCoverage).addTo(map);

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

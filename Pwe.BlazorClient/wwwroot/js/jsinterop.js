var map;
var isZooming;
var icon;
var marker;
var mapTileLayer;
var coverageTileLayer;

window.debuglog = (msg) => { console.log(msg); }

window.initMap = (mapId) => {
    L.mapbox.accessToken = 'pk.eyJ1IjoiemltcG8iLCJhIjoiY2tjenVwanBtMG5lajJ1cDRhMzVuMmFvbSJ9.xQi8BmQYyjNBeCtEGLO9-A';
    map = L.mapbox.map(mapId);
    var mapLight = L.mapbox.tileLayer('mapbox.light', { maxZoom: 16, });
    var mapDark = L.mapbox.tileLayer('mapbox.dark', { maxZoom: 16, });
    var mapOutdoors = L.mapbox.tileLayer('mapbox.outdoors', { maxZoom: 16, }).addTo(map);
   
    coverageTileLayer = L.tileLayer('https://maps0pwe0sa.blob.core.windows.net/maps/coveragetiles/{x}-{y}-{z}.png', {
        attribution: null,
        maxZoom: 16,
        transparent: true,
        zoomOffset: 0,
        tileSize: 256,
        className: 'coveragelayer',
    }).addTo(map);

    var baseMaps = {
        "Kort": mapOutdoors,
        "Lys": mapLight,
        "Mørk": mapDark,
    };

    var overlayMaps = {
        "Udforsket": coverageTileLayer
    };

    L.control.layers(baseMaps, overlayMaps).addTo(map);

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

window.leafletanimated_setpath = (mapId, encodedLine, ms) => {
    var line = L.PolylineUtil.decode(encodedLine);
    var animatedMarker = L.animatedMarker(line);
    var map = maps[mapId];
    map.addLayer(animatedMarker);
}

window.debuglog = (msg) => { console.log(msg); }

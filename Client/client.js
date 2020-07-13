var polyline = null;
var delay = 16;
var mymap;
var isZooming = false;
var prevLocIdx = 0;
var L;
var google;

function init()
{
    mymap = L.map('map').setView([55.6712674, 12.5938239], 12);
    L.tileLayer('http://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>',
        maxZoom: 18,
        id: 'OSM'
    }).addTo(mymap);
    mymap.on("zoomstart", function (e) { isZooming = true; });
    mymap.on("zoomend", function (e) { isZooming = false; });

    getPath();
    //setTimeout(update, delay);
}

function update()
{
    updatePos();
    setTimeout(update, delay);
}

function updatePos()
{
    if (isZooming)
        return;

    var t = (new Date().getTime() - e.PathStartTimeMs) / (e.PolyLineMs * 1.0);
    if (t < 0)
        t = 0;
    if (t > 1)
        t = 1;
    var distT = e.PolyLineTotalLength * t;
    // Find the correct segment
    var i;
    for (i = e.LastPathIdx; i < e.PolyLineSummedDistances.length - 1; i++) {
        if (e.PolyLineSummedDistances[i] > distT)
            break;
    }
    // i is now one too far. 
    i--;
    e.LastPathIdx = i;
    var seg0 = e.PolyLine[i + 0];
    var seg1 = e.PolyLine[i + 1];
    var t0 = e.PolyLineSummedDistances[i + 0] / e.PolyLineTotalLength;
    var t1 = e.PolyLineSummedDistances[i + 1] / e.PolyLineTotalLength;
    var segT = (t - t0) * 1 / (t1 - t0);
    var interpolated = google.maps.geometry.spherical.interpolate(seg0, seg1, segT);
    e.marker.setLatLng([interpolated.lat(), interpolated.lng()]);
    //        console.log(interpolated.lat() + ', ' + interpolated.lng());
}

function decodeData(data)
{
    console.log('Got data: ' + data);
    var items = data.split("!");
    var id = items[0];
    var e;
    if (!(id in entities)) {
        var myIcon = L.icon({
            iconUrl: 'https://s3.eu-central-1.amazonaws.com/12-22-d/Paint/cactus.png',
            iconSize: [60, 60],
            iconAnchor: [30, 56]
        });
        e = {};
        e.marker = L.marker([0, 0], { icon: myIcon }).addTo(mymap);
        entities[id] = e;
        console.log('Entity created');
    }
    else {
        e = entities[id];
        //            console.log('Entity updated');
    }
    e.Id = id;
    e.PathAgeMs = parseInt(items[1]);
    e.PolyLineMs = parseInt(items[2]);
    e.PathStartTimeMs = (new Date().getTime()) - e.PathAgeMs;
    e.PolyLine = google.maps.geometry.encoding.decodePath(items[3]);
    e.PolyLineTotalLength = 0;
    e.PolyLineSummedDistances = [];
    e.PolyLineSummedDistances[0] = 0;
    e.LastPathIdx = 0;
    var cnt = e.PolyLine.length;
    if (cnt > 1) {
        var prev = e.PolyLine[0];
        for (var i = 1; i < cnt; i++) {
            var current = e.PolyLine[i];
            var len = distance(prev.lat(), prev.lng(), current.lat(), current.lng());
            e.PolyLineTotalLength += len;
            e.PolyLineSummedDistances[i] = e.PolyLineTotalLength;
            prev = current;
        }
    }
    entities[e.Id] = e;
}

function distance(lat1, lon1, lat2, lon2)
{
    var p = 0.017453292519943295; // Math.PI / 180
    var c = Math.cos;
    var a = 0.5 - c((lat2 - lat1) * p) / 2 +
        c(lat1 * p) * c(lat2 * p) *
            (1 - c((lon2 - lon1) * p)) / 2;
    return 12742 * Math.asin(Math.sqrt(a)); // 2 * R; R = 6371 km
}

function getPath()
{
    console.log('Get path');
    $.getJSON('http://localhost:7071/api/GetAgentPath?id=1', function (data) {
        console.log('Got path');
        console.log(data);
    });
}

window.onload = init;

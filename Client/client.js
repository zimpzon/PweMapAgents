var polyline = null;
var delay = 16;
var pathReloadDelay = 60 * 5 * 1000;
var mymap;
var isZooming = false;
var prevLocIdx = 0;
var L;
var google;
var agent;
var icon;
var marker;
var pathRequested = false;
var isFirstPathLoad = true;

function init()
{
    mymap = L.map('map');
    //mymap.on('zoomend', function () {
    //    var newzoom = '' + (8 * (mymap.getZoom())) + 'px';
    //    $('.markerClass').css({ 'width': newzoom, 'height': newzoom });
    //});

    var imageUrl = 'https://maps0pwe0sa.blob.core.windows.net/$web/coverage.png',
        imageBounds = [[55.555528, 12.591849], [56.555528, 13.591849]];
    L.imageOverlay(imageUrl, imageBounds).addTo(mymap).on('error', function (e) { console.log(e) });
    L.tileLayer('http://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>',
        maxZoom: 17,
        id: 'OSM'
    }).addTo(mymap);
    mymap.on("zoomstart", function (e) { isZooming = true; });
    mymap.on("zoomend", function (e) { isZooming = false; });

    updatePath();
    updateAgent();
}

function updateAgent()
{
    updatePos();
    setTimeout(updateAgent, delay);
}

function updatePos()
{
    if (agent == undefined)
        return;

    if (isZooming)
        return;

    var polyLineMs = agent.MsEnd - agent.MsStart;
    var t = (new Date().getTime() - agent.MsStart) / (polyLineMs * 1.0);
    if (t < 0)
        t = 0;
    if (t > 1)
        t = 1;

    if (t > 0.95 && !pathRequested) {
        console.log("Getting path, t = " + t);
        updatePath();
    }

    var distT = agent.PolyLineTotalLength * t;

    // Find the correct segment
    var i;
    for (i = agent.LastPathIdx; i < agent.PolyLineSummedDistances.length - 1; i++)
    {
        if (agent.PolyLineSummedDistances[i] > distT)
            break;
    }

    // i is now one too far.
    i--;
    agent.LastPathIdx = i;
    var seg0 = agent.PolyLine[i + 0];
    var seg1 = agent.PolyLine[i + 1];
    var t0 = agent.PolyLineSummedDistances[i + 0] / agent.PolyLineTotalLength;
    var t1 = agent.PolyLineSummedDistances[i + 1] / agent.PolyLineTotalLength;
    var segT = (t - t0) * 1 / (t1 - t0);
    var interpolated = google.maps.geometry.spherical.interpolate(seg0, seg1, segT);
    marker.setLatLng([interpolated.lat(), interpolated.lng()]);

    if (isFirstPathLoad)
    {
        isFirstPathLoad = false;
        mymap.setView([interpolated.lat(), interpolated.lng()], 14);
    }
}

function decodeData(data)
{
    if (icon == undefined)
    {
        icon = L.icon({
            iconUrl: 'cactus.png',
            iconSize: [40, 40],
            iconAnchor: [20, 40],
            className: 'markerClass'
        });
        marker = L.marker([0, 0], { icon: icon }).addTo(mymap);
    }

    agent = data;
    agent.PolyLine = google.maps.geometry.encoding.decodePath(agent.EncodedPolyline);
    agent.PolyLineTotalLength = 0;
    agent.PolyLineSummedDistances = [];
    agent.PolyLineSummedDistances[0] = 0;
    agent.LastPathIdx = 0;

    var cnt = agent.PolyLine.length;
    if (cnt > 1) {
        var prev = agent.PolyLine[0];
        for (var i = 1; i < cnt; i++) {
            var current = agent.PolyLine[i];
            var len = distance(prev.lat(), prev.lng(), current.lat(), current.lng());
            agent.PolyLineTotalLength += len;
            agent.PolyLineSummedDistances[i] = agent.PolyLineTotalLength;
            prev = current;
        }
    }
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

function updatePath()
{
    pathRequested = true;
    $.ajax({
        type: "GET",
        url: "https://maps0pwe0sa.blob.core.windows.net/maps/agents/1-clientpath.json?sv=2019-02-02&st=2020-07-14T11%3A06%3A00Z&se=2030-07-15T11%3A06%3A00Z&sr=b&sp=r&sig=rVzcJjXwrpfk6zPnbZ1jeoBmjzjZ7nLyHHmyAGpW2XU%3D",
        dataType: "json",
        success: function (json)
        {
            console.log("Got path");
            decodeData(json);
            pathRequested = false;
        },
        error: function (x, y, z)
        {
            alert("Error getting data: " + x.responseText);
        }
    });

    $.ajax({
        type: "GET",
        url: "https://maps0pwe0sa.blob.core.windows.net/maps/agents/1-geojson.json?sv=2019-02-02&st=2020-07-14T12%3A00%3A21Z&se=2030-01-01T12%3A00%3A00Z&sr=b&sp=r&sig=8wMnQccvBvXXT55JCH5Gpo3L5LSqZ8J96mUwD7t6YK4%3D",
        dataType: "json",
        success: function (json) {
            L.geoJSON(json).addTo(mymap);
        },
        error: function (x, y, z) {
            alert("Error getting geojson: " + x.responseText);
        }
    });
}

window.onload = init;

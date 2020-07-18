﻿using Pwe.Shared;
using System.Threading.Tasks;

namespace GoogleApis
{
    public interface ILocationInformation
    {
        Task<string> GetInformation(GeoCoord point);
    }
}

public class LocationInfo
{
    public Plus_Code plus_code { get; set; }
    public Result[] results { get; set; }
    public string status { get; set; }

    public class Plus_Code
    {
        public string compound_code { get; set; }
        public string global_code { get; set; }
    }

    public class Result
    {
        public Address_Components[] address_components { get; set; }
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public string place_id { get; set; }
        public Plus_Code1 plus_code { get; set; }
        public string[] types { get; set; }

        public override string ToString()
            => $"{formatted_address} : {string.Join(" | ", types)} : {geometry.location_type}";
    }

    public class Geometry
    {
        public Location location { get; set; }
        public string location_type { get; set; }
        public Viewport viewport { get; set; }
        public Bounds bounds { get; set; }
    }

    public class Location
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }

    public class Viewport
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Northeast
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }

    public class Southwest
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }

    public class Bounds
    {
        public Northeast1 northeast { get; set; }
        public Southwest1 southwest { get; set; }
    }

    public class Northeast1
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }

    public class Southwest1
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }

    public class Plus_Code1
    {
        public string compound_code { get; set; }
        public string global_code { get; set; }
    }

    public class Address_Components
    {
        public string long_name { get; set; }
        public string short_name { get; set; }
        public string[] types { get; set; }
    }
}
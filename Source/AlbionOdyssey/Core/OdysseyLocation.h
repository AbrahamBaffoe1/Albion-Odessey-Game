#pragma once
#include <cmath>
#include <algorithm>

namespace Odyssey
{
// A platform adapter must supply a fresh permission-granted fix; no GPS is ever
// required by room play. Coordinates are intentionally not part of save data.
struct LocationFix
{
    double Latitude = 0;
    double Longitude = 0;
    double AccuracyMeters = 0;
    double AgeSeconds = 0;
    bool PermissionGranted = false;
};
inline bool ValidCoordinates(double Latitude, double Longitude)
{
    return std::isfinite(Latitude) && std::isfinite(Longitude) &&
           Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180;
}
inline double DistanceMeters(double Lat1, double Lon1, double Lat2, double Lon2)
{
    constexpr double Radians = 3.14159265358979323846 / 180.0;
    const double DLat = (Lat2-Lat1)*Radians;
    const double DLon = (Lon2-Lon1)*Radians;
    const double A = std::pow(std::sin(DLat/2),2) + std::cos(Lat1*Radians)*std::cos(Lat2*Radians)*std::pow(std::sin(DLon/2),2);
    return 6371000.0 * 2 * std::asin(std::sqrt(std::clamp(A,0.0,1.0)));
}
inline bool CanCollectAtLocation(const LocationFix& Fix, double TargetLat, double TargetLon, double RadiusMeters)
{
    if (!Fix.PermissionGranted || !ValidCoordinates(Fix.Latitude,Fix.Longitude) || !ValidCoordinates(TargetLat,TargetLon)) return false;
    if (!std::isfinite(Fix.AccuracyMeters) || Fix.AccuracyMeters < 0 || Fix.AccuracyMeters > 30 ||
        !std::isfinite(Fix.AgeSeconds) || Fix.AgeSeconds < 0 || Fix.AgeSeconds > 20 ||
        !std::isfinite(RadiusMeters) || RadiusMeters <= 0 || RadiusMeters > 100) return false;
    // Require the full accuracy circle to fit inside the collection radius.
    return DistanceMeters(Fix.Latitude,Fix.Longitude,TargetLat,TargetLon)+Fix.AccuracyMeters <= RadiusMeters;
}
}

namespace TFG.ARVisor.Domain.Models
{
    public class OwnshipGeoState
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public double? AltitudeMeters { get; }
        public long TimestampUtc { get; }
        public AltitudeQuality AltitudeQuality { get; }

        public OwnshipMode Mode
        {
            get
            {
                if (AltitudeQuality == AltitudeQuality.Good && AltitudeMeters.HasValue)
                {
                    return OwnshipMode.Mode3D;
                }

                return OwnshipMode.Mode2D;
            }
        }

        public OwnshipGeoState(
            double latitude,
            double longitude,
            double? altitudeMeters,
            long timestampUtc,
            AltitudeQuality altitudeQuality)
        {
            Latitude = latitude;
            Longitude = longitude;
            AltitudeMeters = altitudeMeters;
            TimestampUtc = timestampUtc;
            AltitudeQuality = altitudeQuality;
        }
    }
}
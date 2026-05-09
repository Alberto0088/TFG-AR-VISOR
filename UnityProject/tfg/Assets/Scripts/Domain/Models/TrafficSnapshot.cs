namespace TFG.ARVisor.Domain.Models
{
    public class TrafficSnapshot
    {
        public int NearbyAircraft { get; }
        public string NearestDistance { get; }
        public RiskLevel RiskLevel { get; }
        public string AlertMessage { get; }

        public TrafficSnapshot(
            int nearbyAircraft,
            string nearestDistance,
            RiskLevel riskLevel,
            string alertMessage)
        {
            NearbyAircraft = nearbyAircraft;
            NearestDistance = nearestDistance;
            RiskLevel = riskLevel;
            AlertMessage = alertMessage;
        }
    }
}
using TFG.ARVisor.Domain.Models;
using TFG.ARVisor.Presentation.HUD;
using UnityEngine;

namespace TFG.ARVisor.Infrastructure.Simulation
{
    public class SimulatedTrafficProvider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HudController hudController;

        [Header("Simulation Settings")]
        [SerializeField] private float refreshSeconds = 2f;

        private float timer;
        private int step;

        private void Start()
        {
            SendNextSnapshot();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= refreshSeconds)
            {
                timer = 0f;
                SendNextSnapshot();
            }
        }

        private void SendNextSnapshot()
        {
            if (hudController == null)
            {
                Debug.LogWarning("HudController reference is missing.");
                return;
            }

            TrafficSnapshot snapshot = CreateSnapshot(step);
            hudController.RenderTraffic(snapshot);

            step++;
        }

        private TrafficSnapshot CreateSnapshot(int index)
        {
            switch (index % 4)
            {
                case 0:
                    return new TrafficSnapshot(
                        nearbyAircraft: 0,
                        nearestDistance: "--",
                        riskLevel: RiskLevel.Low,
                        alertMessage: "NO ALERTS"
                    );

                case 1:
                    return new TrafficSnapshot(
                        nearbyAircraft: 2,
                        nearestDistance: "8.4 KM",
                        riskLevel: RiskLevel.Low,
                        alertMessage: "NO ALERTS"
                    );

                case 2:
                    return new TrafficSnapshot(
                        nearbyAircraft: 3,
                        nearestDistance: "3.1 KM",
                        riskLevel: RiskLevel.Medium,
                        alertMessage: "TRAFFIC ADVISORY"
                    );

                default:
                    return new TrafficSnapshot(
                        nearbyAircraft: 1,
                        nearestDistance: "1.2 KM",
                        riskLevel: RiskLevel.High,
                        alertMessage: "COLLISION RISK"
                    );
            }
        }
    }
}
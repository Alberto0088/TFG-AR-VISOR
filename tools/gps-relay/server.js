/*
 * server.js
 * ------------------------------------------------------------
 * Servidor relay GPS para TFG-AR-VISOR.
 *
 * El móvil envía su ubicación mediante POST /update.
 * Las Meta Quest leen la última ubicación mediante GET /latest.
 *
 * Esto permite que las gafas funcionen sin PC:
 * - móvil = GPS
 * - servidor online = puente
 * - Quest = visor AR
 */

const express = require("express");
const cors = require("cors");
const path = require("path");

const app = express();
const port = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());
app.use(express.static(path.join(__dirname, "public")));

let latestGps = null;

app.get("/", (req, res) => {
  res.sendFile(path.join(__dirname, "public", "mobile-gps.html"));
});

app.get("/health", (req, res) => {
  res.json({
    status: "ok",
    hasGps: latestGps !== null,
    timestamp: Math.floor(Date.now() / 1000)
  });
});

app.post("/update", (req, res) => {
  const { lat, lon, alt, accuracy, altitudeAccuracy } = req.body;

  if (typeof lat !== "number" || typeof lon !== "number") {
    return res.status(400).json({
      ok: false,
      error: "Invalid lat/lon"
    });
  }

  latestGps = {
    lat,
    lon,
    alt: typeof alt === "number" ? alt : 0,
    hasAltitude: typeof alt === "number",
    altitudeReliable:
      typeof altitudeAccuracy === "number" && altitudeAccuracy <= 50,
    accuracy: typeof accuracy === "number" ? accuracy : null,
    altitudeAccuracy:
      typeof altitudeAccuracy === "number" ? altitudeAccuracy : null,
    timestamp: Math.floor(Date.now() / 1000)
  };

  console.log("GPS updated:", latestGps);

  res.json({
    ok: true,
    latestGps
  });
});

app.get("/latest", (req, res) => {
  if (!latestGps) {
    return res.status(404).json({
      ok: false,
      error: "No GPS data available yet"
    });
  }

  res.json(latestGps);
});

app.listen(port, () => {
  console.log(`GPS relay running on port ${port}`);
});
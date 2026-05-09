from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import time

latest_gps = None


HTML_PAGE = """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>TFG AR Visor GPS Sender</title>
</head>
<body>
    <h2>TFG AR Visor - GPS Sender</h2>
    <p id="status">Esperando permiso de ubicación...</p>
    <pre id="data"></pre>

    <script>
        const statusEl = document.getElementById("status");
        const dataEl = document.getElementById("data");

        function sendPosition(position) {
            const coords = position.coords;

            const payload = {
                lat: coords.latitude,
                lon: coords.longitude,
                alt: coords.altitude ?? 0,
                hasAltitude: coords.altitude !== null,
                altitudeReliable: coords.altitudeAccuracy !== null ? coords.altitudeAccuracy <= 80 : false,
                timestamp: Math.floor(Date.now() / 1000)
            };

            fetch("/update", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(payload)
            })
            .then(() => {
                statusEl.innerText = "GPS enviado correctamente";
                dataEl.innerText = JSON.stringify(payload, null, 2);
            })
            .catch(error => {
                statusEl.innerText = "Error enviando GPS: " + error;
            });
        }

        function handleError(error) {
            statusEl.innerText = "Error GPS: " + error.message;
        }

        if ("geolocation" in navigator) {
            navigator.geolocation.watchPosition(
                sendPosition,
                handleError,
                {
                    enableHighAccuracy: true,
                    maximumAge: 1000,
                    timeout: 5000
                }
            );
        } else {
            statusEl.innerText = "Este navegador no soporta geolocalización.";
        }
    </script>
</body>
</html>
"""


class GpsRequestHandler(BaseHTTPRequestHandler):
    def _set_headers(self, status_code=200, content_type="application/json"):
        self.send_response(status_code)
        self.send_header("Content-Type", content_type)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()

    def do_OPTIONS(self):
        self._set_headers(200)

    def do_GET(self):
        global latest_gps

        if self.path == "/":
            self._set_headers(200, "text/html")
            self.wfile.write(HTML_PAGE.encode("utf-8"))
            return

        if self.path == "/latest":
            if latest_gps is None:
                self._set_headers(404)
                self.wfile.write(json.dumps({
                    "error": "No GPS data received yet"
                }).encode("utf-8"))
                return

            self._set_headers(200)
            self.wfile.write(json.dumps(latest_gps).encode("utf-8"))
            return

        self._set_headers(404)
        self.wfile.write(json.dumps({
            "error": "Not found"
        }).encode("utf-8"))

    def do_POST(self):
        global latest_gps

        if self.path != "/update":
            self._set_headers(404)
            self.wfile.write(json.dumps({
                "error": "Not found"
            }).encode("utf-8"))
            return

        content_length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(content_length)

        try:
            payload = json.loads(body.decode("utf-8"))

            latest_gps = {
                "lat": float(payload["lat"]),
                "lon": float(payload["lon"]),
                "alt": float(payload.get("alt", 0)),
                "hasAltitude": bool(payload.get("hasAltitude", False)),
                "altitudeReliable": bool(payload.get("altitudeReliable", False)),
                "timestamp": int(payload.get("timestamp", int(time.time())))
            }

            print("GPS actualizado:", latest_gps)

            self._set_headers(200)
            self.wfile.write(json.dumps({
                "status": "ok",
                "latest": latest_gps
            }).encode("utf-8"))

        except Exception as exception:
            self._set_headers(400)
            self.wfile.write(json.dumps({
                "error": str(exception)
            }).encode("utf-8"))


def run_server():
    host = "0.0.0.0"
    port = 5000

    server = HTTPServer((host, port), GpsRequestHandler)

    print("Servidor GPS iniciado")
    print(f"Escuchando en http://{host}:{port}")
    print("Desde este PC: http://localhost:5000")
    print("Desde el móvil/Quest: usa la IP local del PC, por ejemplo http://192.168.1.X:5000")

    server.serve_forever()


if __name__ == "__main__":
    run_server()
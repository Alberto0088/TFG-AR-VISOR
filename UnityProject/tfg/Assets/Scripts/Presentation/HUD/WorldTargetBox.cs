/*
 * WorldTargetBox.cs  —  v0.10-alpha Visual Redesign
 * ------------------------------------------------------------
 * Caja pseudo-AR de seguimiento tipo visor de combate.
 *
 * Mejoras respecto a la versión anterior:
 *   - Fade in/out suave (lerp alpha en lugar de snap).
 *   - Scan-line animada que barre la caja verticalmente en estado HIGH.
 *   - Esquinas más finas y elegantes.
 *   - Label mínimo: callsign + distancia, o callsign + IN si hay TCPA en HIGH.
 *   - Fondo translúcido levemente más visible en HIGH.
 *   - Posición estable con lerp suavizado.
 *   - Se oculta si el target sale del campo visual.
 *
 * No modifica la retícula central.
 * Es un Canvas en World Space que se coloca delante de la cámara en la dirección
 * aproximada del target según TargetViewOffsetDegrees del snapshot.
 */

using System;
using TFG.ARVisor.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TFG.ARVisor.Presentation.HUD
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class WorldTargetBox : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private Transform viewerCamera;

        [Header("Visibility")]
        [SerializeField] private bool  showInLowRisk          = false;
        [SerializeField] private float maxVisibleAngleDegrees = 70f;

        [Header("World Placement")]
        [SerializeField] private float markerDistanceMeters = 12f;
        [SerializeField] private float verticalOffsetMeters = -0.15f;
        [SerializeField] private float smoothSpeed          = 8f;

        [Header("Box Style")]
        [SerializeField] private Vector2 mediumBoxSize    = new Vector2(180f, 90f);
        [SerializeField] private Vector2 highBoxSize      = new Vector2(220f, 110f);
        [SerializeField] private float   worldScale       = 0.0035f;
        [SerializeField] private float   cornerLength     = 28f;
        [SerializeField] private float   cornerThickness  = 2.5f;
        [SerializeField] private float   backgroundAlpha  = 0.10f;
        [SerializeField] private float   fadeSpeed        = 6f;

        [Header("HIGH Pulse")]
        [SerializeField] private float highPulseSpeed  = 3.5f;
        [SerializeField] private float highPulseAmount = 0.14f;

        [Header("Scan Line")]
        [SerializeField] private bool  showScanLine       = true;
        [SerializeField] private float scanLineSpeed      = 0.8f;   // cycles per second
        [SerializeField] private float scanLineThickness  = 2f;
        [SerializeField] private float scanLineAlpha      = 0.35f;

        [Header("Label")]
        [SerializeField] private int mediumFontSize = 18;
        [SerializeField] private int highFontSize   = 22;

        // ── Runtime refs ─────────────────────────────────────────────
        private Canvas      _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _rootRect;

        private RectTransform _boxRoot;
        private RawImage      _background;

        private RawImage _tl_h, _tl_v;
        private RawImage _tr_h, _tr_v;
        private RawImage _bl_h, _bl_v;
        private RawImage _br_h, _br_v;

        private RawImage _scanLine;
        private TMP_Text _label;

        // ── State ─────────────────────────────────────────────────────
        private Vector3   _targetWorldPos;
        private Vector2   _targetBoxSize;
        private RiskLevel _currentRisk;
        private bool      _visible;
        private float     _scanPhase;

        // ── Unity ─────────────────────────────────────────────────────

        private void Awake()
        {
            _canvas      = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootRect    = GetComponent<RectTransform>();

            _canvas.renderMode   = RenderMode.WorldSpace;
            _canvas.sortingOrder = 100;

            if (viewerCamera == null && Camera.main != null)
            {
                viewerCamera       = Camera.main.transform;
                _canvas.worldCamera = Camera.main;
            }

            transform.localScale = Vector3.one * worldScale;

            BuildBox();
            SetAlpha(0f);
        }

        private void Update()
        {
            if (viewerCamera == null) return;

            // Smooth fade
            float targetAlpha = _visible ? 1f : 0f;
            float current     = _canvasGroup.alpha;
            _canvasGroup.alpha = Mathf.MoveTowards(current, targetAlpha, Time.deltaTime * fadeSpeed);

            if (!_visible && _canvasGroup.alpha <= 0.01f) return;

            // Position tracking
            transform.position = Vector3.Lerp(
                transform.position,
                _targetWorldPos,
                Time.deltaTime * smoothSpeed);

            // Always face camera
            Vector3 dir = transform.position - viewerCamera.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            // Box size
            _boxRoot.sizeDelta = Vector2.Lerp(
                _boxRoot.sizeDelta,
                _targetBoxSize,
                Time.deltaTime * smoothSpeed);

            // Pulse scale (HIGH only)
            if (_currentRisk == RiskLevel.High)
            {
                float pulse = 1f + Mathf.Sin(Time.time * highPulseSpeed) * highPulseAmount;
                _boxRoot.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                _boxRoot.localScale = Vector3.Lerp(
                    _boxRoot.localScale,
                    Vector3.one,
                    Time.deltaTime * smoothSpeed);
            }

            // Scan line (HIGH only)
            if (_scanLine != null)
            {
                bool scanActive = _currentRisk == RiskLevel.High && showScanLine && _visible;
                _scanLine.gameObject.SetActive(scanActive);

                if (scanActive)
                {
                    _scanPhase = (_scanPhase + Time.deltaTime * scanLineSpeed) % 1f;
                    float boxH = _boxRoot.sizeDelta.y;
                    float yPos = Mathf.Lerp(-boxH * 0.5f, boxH * 0.5f, _scanPhase);

                    var rt = _scanLine.rectTransform;
                    rt.anchoredPosition = new Vector2(0f, yPos);
                    rt.sizeDelta        = new Vector2(_boxRoot.sizeDelta.x, scanLineThickness);
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────

        public void RenderBox(TrafficSnapshot snapshot)
        {
            if (snapshot == null || !HasTarget(snapshot))
            {
                _visible = false;
                return;
            }

            if (snapshot.RiskLevel == RiskLevel.Low && !showInLowRisk)
            {
                _visible = false;
                return;
            }

            if (!snapshot.TargetViewOffsetDegrees.HasValue)
            {
                _visible = false;
                return;
            }

            double offset = snapshot.TargetViewOffsetDegrees.Value;
            if (Math.Abs(offset) > maxVisibleAngleDegrees)
            {
                _visible = false;
                return;
            }

            _currentRisk     = snapshot.RiskLevel;
            _targetWorldPos  = ComputeWorldPosition(offset);
            _targetBoxSize   = snapshot.RiskLevel == RiskLevel.High ? highBoxSize : mediumBoxSize;

            Color riskColor = HudVisualTheme.GetRiskColor(snapshot.RiskLevel);
            ApplyColor(riskColor, snapshot.RiskLevel);

            if (_label != null)
            {
                _label.text     = BuildLabel(snapshot);
                _label.color    = riskColor;
                _label.fontSize = snapshot.RiskLevel == RiskLevel.High ? highFontSize : mediumFontSize;
            }

            _visible = true;
        }

        // ── Internal build ────────────────────────────────────────────

        private void BuildBox()
        {
            _rootRect.sizeDelta = highBoxSize;

            // Box root
            _boxRoot                     = CreateRect("TargetLockBox", transform);
            _boxRoot.anchorMin           = new Vector2(0.5f, 0.5f);
            _boxRoot.anchorMax           = new Vector2(0.5f, 0.5f);
            _boxRoot.pivot               = new Vector2(0.5f, 0.5f);
            _boxRoot.anchoredPosition    = Vector2.zero;
            _boxRoot.sizeDelta           = mediumBoxSize;

            // Background
            _background = CreateImage("Background", _boxRoot);
            Stretch(_background.rectTransform);

            // Corners
            _tl_h = CreateImage("TL_H", _boxRoot);
            _tl_v = CreateImage("TL_V", _boxRoot);
            _tr_h = CreateImage("TR_H", _boxRoot);
            _tr_v = CreateImage("TR_V", _boxRoot);
            _bl_h = CreateImage("BL_H", _boxRoot);
            _bl_v = CreateImage("BL_V", _boxRoot);
            _br_h = CreateImage("BR_H", _boxRoot);
            _br_v = CreateImage("BR_V", _boxRoot);
            LayoutCorners();

            // Scan line (hidden by default)
            _scanLine                    = CreateImage("ScanLine", _boxRoot);
            _scanLine.color              = new Color(1f, 1f, 1f, scanLineAlpha);
            var slRect                   = _scanLine.rectTransform;
            slRect.anchorMin             = new Vector2(0.5f, 0.5f);
            slRect.anchorMax             = new Vector2(0.5f, 0.5f);
            slRect.pivot                 = new Vector2(0.5f, 0.5f);
            slRect.sizeDelta             = new Vector2(mediumBoxSize.x, scanLineThickness);
            slRect.anchoredPosition      = Vector2.zero;
            _scanLine.gameObject.SetActive(false);

            // Label
            _label = CreateLabel("TargetLabel", _boxRoot);
        }

        private void LayoutCorners()
        {
            float cl = cornerLength;
            float ct = cornerThickness;

            SetCorner(_tl_h.rectTransform, 0f, 1f,  cl, ct,  cl * 0.5f, -ct * 0.5f);
            SetCorner(_tl_v.rectTransform, 0f, 1f,  ct, cl,  ct * 0.5f, -cl * 0.5f);
            SetCorner(_tr_h.rectTransform, 1f, 1f,  cl, ct, -cl * 0.5f, -ct * 0.5f);
            SetCorner(_tr_v.rectTransform, 1f, 1f,  ct, cl, -ct * 0.5f, -cl * 0.5f);
            SetCorner(_bl_h.rectTransform, 0f, 0f,  cl, ct,  cl * 0.5f,  ct * 0.5f);
            SetCorner(_bl_v.rectTransform, 0f, 0f,  ct, cl,  ct * 0.5f,  cl * 0.5f);
            SetCorner(_br_h.rectTransform, 1f, 0f,  cl, ct, -cl * 0.5f,  ct * 0.5f);
            SetCorner(_br_v.rectTransform, 1f, 0f,  ct, cl, -ct * 0.5f,  cl * 0.5f);
        }

        private static void SetCorner(RectTransform rt, float ax, float ay,
                                      float w, float h, float px, float py)
        {
            rt.anchorMin        = new Vector2(ax, ay);
            rt.anchorMax        = new Vector2(ax, ay);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(px, py);
        }

        private Vector3 ComputeWorldPosition(double offsetDegrees)
        {
            Quaternion rot = Quaternion.AngleAxis((float)offsetDegrees, Vector3.up);
            Vector3    dir = rot * viewerCamera.forward;

            return viewerCamera.position
                 + dir.normalized * markerDistanceMeters
                 + Vector3.up * verticalOffsetMeters;
        }

        private string BuildLabel(TrafficSnapshot snapshot)
        {
            string callsign = string.IsNullOrWhiteSpace(snapshot.RelevantCallsign)
                ? "TARGET"
                : snapshot.RelevantCallsign;

            string distance = string.IsNullOrWhiteSpace(snapshot.NearestDistance)
                              || snapshot.NearestDistance == "--"
                ? ""
                : snapshot.NearestDistance;

            bool hasTcpa = !string.IsNullOrWhiteSpace(snapshot.TimeToClosestApproach)
                        && snapshot.TimeToClosestApproach != "--";

            if (snapshot.RiskLevel == RiskLevel.High && hasTcpa)
                return $"{callsign}\nIN {snapshot.TimeToClosestApproach}";

            if (!string.IsNullOrWhiteSpace(distance))
                return $"{callsign}\n{distance}";

            return callsign;
        }

        private void ApplyColor(Color color, RiskLevel risk)
        {
            Color[] corners = { color, color, color, color, color, color, color, color };
            RawImage[] images = { _tl_h, _tl_v, _tr_h, _tr_v, _bl_h, _bl_v, _br_h, _br_v };
            for (int i = 0; i < images.Length; i++)
                if (images[i] != null) images[i].color = corners[i];

            if (_background != null)
            {
                Color bg  = color;
                bg.a      = risk == RiskLevel.High ? backgroundAlpha * 2f : backgroundAlpha;
                _background.color = bg;
            }

            if (_scanLine != null)
            {
                Color sc  = color;
                sc.a      = scanLineAlpha;
                _scanLine.color = sc;
            }
        }

        private void SetAlpha(float a)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = a;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private static bool HasTarget(TrafficSnapshot s)
        {
            return s != null && !string.IsNullOrWhiteSpace(s.RelevantCallsign);
        }

        // ── UI helpers ────────────────────────────────────────────────

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RawImage CreateImage(string name, Transform parent)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<RawImage>();
            img.texture       = Texture2D.whiteTexture;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text CreateLabel(string name, Transform parent)
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rt          = go.GetComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = new Vector2(8f,  8f);
            rt.offsetMax    = new Vector2(-8f, -8f);

            var tmp         = go.GetComponent<TMP_Text>();
            tmp.alignment   = TextAlignmentOptions.Center;
            tmp.fontStyle   = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget     = false;
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
        }
    }
}

/*
 * HudCompassWidget.cs  —  v0.10-alpha
 * Brujula visual tipo franja con N/NE/E/SE/S/SW/W/NW.
 * Heading actual entre [brackets]. Target con >> en MED/HIGH.
 * Usa TextMeshPro 3D, sin Canvas.
 */
using System.Text;
using TMPro;
using UnityEngine;
using TFG.ARVisor.Domain.Models;

namespace TFG.ARVisor.Presentation.HUD
{
    public class HudCompassWidget : MonoBehaviour
    {
        private const int   STEPS = 9;
        private const int   DSTEP = 10;
        private const float TARC  = 9f;

        private double    _bearing = double.NaN;
        private RiskLevel _risk    = RiskLevel.Low;
        private bool      _hasTgt;

        private TMP_Text _strip;
        private TMP_Text _hdg;

        private void Awake()
        {
            _strip = MakeTmp("HUD_CompassStrip", Vector3.zero, 4.5f, TextAlignmentOptions.Center);
            _hdg   = MakeTmp("HUD_CompassHdg", new Vector3(0f, -0.065f, 0f), 3.5f, TextAlignmentOptions.Center);
        }

        private TMP_Text MakeTmp(string name, Vector3 pos, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one * HudVisualTheme.DefaultWorldScale;
            var t                      = go.AddComponent<TextMeshPro>();
            t.fontSize                 = size;
            t.alignment                = align;
            t.enableWordWrapping       = false;
            t.raycastTarget            = false;
            t.richText                 = true;
            t.color                    = HudVisualTheme.ColorWhite;
            t.text                     = "";
            return t;
        }

        private void Update()
        {
            if (_strip == null) return;
            float yaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
            _strip.text = BuildStrip(yaw);
            if (_hdg != null)
                _hdg.text = $"<color={HudVisualTheme.HexDim}>{(int)yaw:000}</color>";
        }

        private string BuildStrip(float cy)
        {
            int center = Mathf.RoundToInt(cy / DSTEP) * DSTEP;
            int half   = STEPS / 2;
            var sb     = new StringBuilder();
            for (int i = 0; i < STEPS; i++)
            {
                int rd  = center + (i - half) * DSTEP;
                int nd  = ((rd % 360) + 360) % 360;
                bool ic = (i == half);
                bool it = _hasTgt && Near(nd, _bearing, TARC);
                string lbl = Cardinal(nd) ?? $"{nd:000}";
                if (ic)
                    sb.Append($"<color={HudVisualTheme.HexWhite}><b>[{lbl}]</b></color>");
                else if (it)
                    sb.Append($"<color={HudVisualTheme.GetRiskHex(_risk)}>>>{lbl}</color>");
                else
                {
                    string nc = Cardinal(nd) != null ? (nd == 0 ? "#FFFFFF" : HudVisualTheme.HexIdle) : HudVisualTheme.HexDim;
                    sb.Append($"<color={nc}>{lbl}</color>");
                }
                if (i < STEPS - 1) sb.Append("  ");
            }
            return sb.ToString();
        }

        public void SetTarget(double bearing, RiskLevel risk) { _bearing = bearing; _risk = risk; _hasTgt = true; }
        public void ClearTarget() { _bearing = double.NaN; _hasTgt = false; _risk = RiskLevel.Low; }

        private static bool Near(int nd, double b, float thr)
        {
            if (double.IsNaN(b)) return false;
            return Mathf.Abs(Mathf.DeltaAngle(nd, (float)b)) <= thr;
        }

        private static string Cardinal(int d)
        {
            switch (d)
            {
                case   0: return "N";  case  45: return "NE";
                case  90: return "E";  case 135: return "SE";
                case 180: return "S";  case 225: return "SW";
                case 270: return "W";  case 315: return "NW";
                default:  return null;
            }
        }
    }
}

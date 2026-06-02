/*
 * HudThreatWidget.cs  —  v0.10-alpha
 * Widget compacto de amenaza. Reemplaza el panel derecho del HUD.
 * LOW: una linea discreta. MED/HIGH: header + datos + guia.
 */
using TMPro;
using UnityEngine;

namespace TFG.ARVisor.Presentation.HUD
{
    public class HudThreatWidget : MonoBehaviour
    {
        private TMP_Text _headerLine;
        private TMP_Text _dataLine;
        private TMP_Text _guidanceLine;

        private void Awake()
        {
            _headerLine   = MakeLine("HUD_ThreatHeader",    0.00f, 5.5f);
            _dataLine     = MakeLine("HUD_ThreatData",     -0.09f, 4.5f);
            _guidanceLine = MakeLine("HUD_ThreatGuidance", -0.17f, 4.0f);
        }

        private TMP_Text MakeLine(string name, float localY, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, localY, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one * HudVisualTheme.DefaultWorldScale;
            var tmp                    = go.AddComponent<TextMeshPro>();
            tmp.fontSize               = fontSize;
            tmp.alignment              = TextAlignmentOptions.Right;
            tmp.enableWordWrapping     = false;
            tmp.raycastTarget          = false;
            tmp.richText               = true;
            tmp.color                  = HudVisualTheme.ColorWhite;
            tmp.text                   = "";
            return tmp;
        }

        public void RenderLow(int nearbyCount, string nearestDistance)
        {
            if (_headerLine == null) return;
            string dist = HasVal(nearestDistance) ? $"  <color={HudVisualTheme.HexDim}>{nearestDistance}</color>" : "";
            _headerLine.text = $"<color={HudVisualTheme.HexLow}>CLEAR</color><color={HudVisualTheme.HexDim}>  {nearbyCount} AC{dist}</color>";
            Set(_dataLine, "");
            Set(_guidanceLine, "");
        }

        public void RenderMedium(string callsign, string distance, string cpa, string tcpa, string guidance)
        {
            if (_headerLine == null) return;
            _headerLine.text = $"<color={HudVisualTheme.HexMedium}>TRAJECTORY WATCH</color>";
            _dataLine.text   = DataLine(HudVisualTheme.HexMedium, callsign, distance, cpa, tcpa);
            SetGuide(guidance, HudVisualTheme.HexDim);
        }

        public void RenderHigh(string callsign, string distance, string cpa, string tcpa, string guidance)
        {
            if (_headerLine == null) return;
            _headerLine.text = $"<color={HudVisualTheme.HexHigh}>CONFLICT</color>";
            _dataLine.text   = DataLine(HudVisualTheme.HexHigh, callsign, distance, cpa, tcpa);
            SetGuide(guidance, HudVisualTheme.HexHighSoft);
        }

        public void RenderEmpty()
        {
            Set(_headerLine, ""); Set(_dataLine, ""); Set(_guidanceLine, "");
        }

        public TMP_Text GetHeaderLine() => _headerLine;

        private string DataLine(string cc, string cs, string d, string c, string t) =>
            $"<color={cc}>{S(cs,"TARGET")}</color><color={HudVisualTheme.HexDim}>  {S(d,"--")}  CPA {S(c,"--")}  IN {S(t,"--")}</color>";

        private void SetGuide(string text, string col)
        {
            if (_guidanceLine == null) return;
            _guidanceLine.text = HasVal(text) ? $"<color={col}>{text}</color>" : "";
        }

        private static void Set(TMP_Text t, string v) { if (t != null) t.text = v; }
        private static bool HasVal(string v) => !string.IsNullOrWhiteSpace(v) && v != "--";
        private static string S(string v, string f) => HasVal(v) ? v : f;
    }
}

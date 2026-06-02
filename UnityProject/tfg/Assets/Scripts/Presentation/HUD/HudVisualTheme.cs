using TFG.ARVisor.Domain.Models;
using UnityEngine;
namespace TFG.ARVisor.Presentation.HUD {
public static class HudVisualTheme {
    public static readonly Color ColorLow    = new Color(0.40f,0.82f,0.60f,1f);
    public static readonly Color ColorMedium = new Color(1.00f,0.78f,0.28f,1f);
    public static readonly Color ColorHigh   = new Color(1.00f,0.32f,0.26f,1f);
    public static readonly Color ColorIdle   = new Color(0.55f,0.70f,0.60f,0.80f);
    public static readonly Color ColorDim    = new Color(0.45f,0.45f,0.45f,0.70f);
    public static readonly Color ColorWhite  = new Color(0.92f,0.92f,0.92f,1f);
    public static readonly Color PanelBg     = new Color(0.04f,0.05f,0.04f,0.28f);
    public static readonly Color PanelBorder = new Color(1f,1f,1f,0.14f);
    public const string HexLow    = "#66D199";
    public const string HexMedium = "#FFC847";
    public const string HexHigh   = "#FF5242";
    public const string HexIdle   = "#8CB89B";
    public const string HexDim    = "#737373";
    public const string HexWhite  = "#EBEBEB";
    public const string HexHighSoft = "#FF9980";
    public const float DefaultWorldScale = 0.1f;
    public const float DefaultFontSize   = 5f;
    public static readonly char[] SpinnerFrames = {'|','/','-','\\'};
    public static Color GetRiskColor(RiskLevel r) {
        switch(r){ case RiskLevel.High: return ColorHigh; case RiskLevel.Medium: return ColorMedium; default: return ColorLow; }
    }
    public static string GetRiskHex(RiskLevel r) {
        switch(r){ case RiskLevel.High: return HexHigh; case RiskLevel.Medium: return HexMedium; default: return HexLow; }
    }
}}

using System; using TFG.ARVisor.Domain.Models; using TMPro; using UnityEngine;
namespace TFG.ARVisor.Presentation.HUD {
public class HudController : MonoBehaviour {
    [Header("Scene Refs")]
    [SerializeField] TMP_Text systemText;
    [SerializeField] TMP_Text trafficText;
    [SerializeField] TMP_Text alertText;
    [SerializeField] TMP_Text reticleText;
    [SerializeField] WorldTargetBox worldTargetBox;

    HudAlertBanner      _alert;
    HudCompassStrip     _compass;
    HudConflictPanel    _conflict;
    HudStatusWidget     _status;
    HudLoadingIndicator _loader;
    HudAnimator         _anim;

    void Awake(){ HideOld(); Build(); _anim=gameObject.AddComponent<HudAnimator>(); }
    void Start(){
        _status?.Render("ONLINE","WAIT","SIM","1 Hz");
        _conflict?.ShowLow(0,"--");
        _loader?.Show("SCANNING AIRSPACE");
        _alert?.Hide();
        if(alertText!=null){ alertText.richText=true; alertText.gameObject.SetActive(false); }
        if(reticleText!=null){ reticleText.richText=true; reticleText.text=$"<color={HudVisualTheme.HexDim}>+</color>"; reticleText.color=Color.white; }
    }
    void HideOld(){ Kill(systemText); Kill(trafficText); }
    static void Kill(TMP_Text t){ if(t!=null)t.color=new Color(0,0,0,0); }

    void Build(){
        Transform lp   = systemText!=null ? systemText.transform.parent : null;
        Transform root = lp!=null ? lp.parent : transform;
        _alert   = Spawn<HudAlertBanner>   ("HUD_Alert",    root, new Vector3(0f,    0.78f, 2.5f));
        _compass = Spawn<HudCompassStrip>  ("HUD_Compass",  root, new Vector3(0f,    0.56f, 2.5f));
        _conflict= Spawn<HudConflictPanel> ("HUD_Conflict", root, new Vector3( 1.082f, -0.092f, 2.5f));
        _status  = Spawn<HudStatusWidget>  ("HUD_Status",   root, new Vector3(-1.173f,  0.073f, 2.5f));
        _loader  = Spawn<HudLoadingIndicator>("HUD_Load",   root, new Vector3(0.212f, 0.44f, 2.5f));
    }
    static T Spawn<T>(string name, Transform parent, Vector3 pos) where T : MonoBehaviour {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go.AddComponent<T>();
    }

    public void RenderSystemStatus(string status, string gpsStatus, string mode, string updateRate){
        bool sim = mode!=null && mode.ToUpperInvariant().Contains("SIM");
        if(sim)               _status?.RenderSimMode(status);
        else if(gpsStatus=="WAIT") _status?.RenderWaiting();
        else                  _status?.Render(status,gpsStatus,mode,updateRate);
    }
    public void RenderGpsCoordinates(double lat, double lon, double? alt){
        _status?.SetCoordinates(lat,lon,alt);
    }
    public void RenderTraffic(TrafficSnapshot s){
        if(s==null){ NoData(); return; }
        _loader?.Hide();
        worldTargetBox?.RenderBox(s);
        UpdateReticle(s);
        switch(s.RiskLevel){
            case RiskLevel.Low:    DoLow(s);    break;
            case RiskLevel.Medium: DoMedium(s); break;
            case RiskLevel.High:   DoHigh(s);   break;
        }
    }
    void DoLow(TrafficSnapshot s){
        _anim?.StopAll();
        _alert?.Hide();
        if(alertText!=null) alertText.gameObject.SetActive(false);
        _conflict?.ShowLow(s.NearbyAircraft, s.NearestDistance, s.RelevantCallsign);
        _compass?.ClearTarget();
    }
    void DoMedium(TrafficSnapshot s){
        _alert?.ShowMedium(Msg(s,"TRAFFIC ADVISORY"));
        if(alertText!=null) alertText.gameObject.SetActive(false);
        _conflict?.ShowMedium(s.RelevantCallsign,s.NearestDistance,s.ClosestApproachDistance,s.TimeToClosestApproach,Guide(s));
        var hdr=_conflict?.GetCallsignLabel();
        if(hdr!=null)_anim?.StartPulse(hdr,HudVisualTheme.ColorMedium,HudVisualTheme.ColorWhite,1.6f);
        CompassTarget(s);
    }
    void DoHigh(TrafficSnapshot s){
        _alert?.ShowHigh(Msg(s,"CONFLICT ALERT"));
        if(alertText!=null) alertText.gameObject.SetActive(false);
        _conflict?.ShowHigh(s.RelevantCallsign,s.NearestDistance,s.ClosestApproachDistance,s.TimeToClosestApproach,Guide(s));
        var hdr=_conflict?.GetCallsignLabel();
        if(hdr!=null)_anim?.StartPulse(hdr,HudVisualTheme.ColorHigh,new Color(1f,0.7f,0.7f),2.4f);
        CompassTarget(s);
    }
    void NoData(){
        _conflict?.ShowEmpty(); _loader?.Show("NO TRAFFIC DATA");
        _compass?.ClearTarget(); _anim?.StopAll(); _alert?.Hide();
        worldTargetBox?.RenderBox(null); UpdateReticle(null);
    }
    void CompassTarget(TrafficSnapshot s){
        if(_compass==null||!s.TargetViewOffsetDegrees.HasValue||string.IsNullOrWhiteSpace(s.RelevantCallsign)){ _compass?.ClearTarget(); return; }
        float yaw = Camera.main!=null ? Camera.main.transform.eulerAngles.y : 0f;
        _compass.SetTarget(yaw+s.TargetViewOffsetDegrees.Value, s.RiskLevel);
    }
    void UpdateReticle(TrafficSnapshot s){
        if(reticleText==null) return;
        if(s==null||s.RiskLevel==RiskLevel.Low||string.IsNullOrWhiteSpace(s.RelevantCallsign)){
            reticleText.text=$"<color={HudVisualTheme.HexDim}>+</color>"; reticleText.color=Color.white; return;
        }
        reticleText.color=HudVisualTheme.GetRiskColor(s.RiskLevel);
        if(!s.TargetViewOffsetDegrees.HasValue){ reticleText.text=s.RiskLevel==RiskLevel.High?$"<color={HudVisualTheme.HexHigh}>[!]</color>":"[+]"; return; }
        double off=s.TargetViewOffsetDegrees.Value, abs=Math.Abs(off);
        string hex=HudVisualTheme.GetRiskHex(s.RiskLevel); bool hi=s.RiskLevel==RiskLevel.High;
        if(abs<=12)  reticleText.text=$"<color={hex}>{(hi?"[!]":"[+]")}</color>";
        else if(abs<=45)  reticleText.text=$"<color={hex}>{(off>0?"+  >":"<  +")}</color>";
        else if(abs<=120) reticleText.text=$"<color={hex}>{(off>0?">>":"<<")}</color>";
        else reticleText.text=$"<color={hex}>v</color>";
    }
    static string Msg(TrafficSnapshot s,string fb)=>string.IsNullOrWhiteSpace(s.AlertMessage)?fb:s.AlertMessage;
    static string Guide(TrafficSnapshot s){
        if(!s.TargetViewOffsetDegrees.HasValue)return"--";
        double off=s.TargetViewOffsetDegrees.Value,abs=Math.Abs(off);
        if(abs<=12)return"CENTERED"; if(abs<=45)return off>0?"SLIGHT RIGHT >":"< SLIGHT LEFT";
        if(abs<=120)return off>0?"LOOK RIGHT >>":"<< LOOK LEFT"; return"BEHIND";
    }
}}

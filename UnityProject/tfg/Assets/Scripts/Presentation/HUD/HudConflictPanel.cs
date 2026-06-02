using TMPro; using UnityEngine; using UnityEngine.UI;
using TFG.ARVisor.Domain.Models;
namespace TFG.ARVisor.Presentation.HUD {
[RequireComponent(typeof(Canvas))][RequireComponent(typeof(CanvasGroup))]
public class HudConflictPanel : MonoBehaviour {
    const float PW=160f,PH=195f,WS=0.003f;
    Canvas _cv; CanvasGroup _cg;
    RawImage _bg,_topLine,_divider;
    TMP_Text _trafLabel,_callsign,_riskBadge;
    TMP_Text _lDist,_vDist,_lCpa,_vCpa,_lIn,_vIn,_lDir,_vDir;

    void Awake(){
        _cv=GetComponent<Canvas>(); _cv.renderMode=RenderMode.WorldSpace; _cv.sortingOrder=98;
        if(Camera.main!=null)_cv.worldCamera=Camera.main;
        _cg=GetComponent<CanvasGroup>(); _cg.interactable=false; _cg.blocksRaycasts=false;
        var rt=GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(PW,PH);
        transform.localScale=Vector3.one*WS; Build();
    }
    void Build(){
        _bg=Img("BG",transform); Str(_bg.rectTransform); _bg.color=HudVisualTheme.PanelBg;
        _topLine=Img("TL",transform); var tl=_topLine.rectTransform;
        tl.anchorMin=new Vector2(0f,1f); tl.anchorMax=new Vector2(1f,1f);
        tl.pivot=new Vector2(0.5f,1f); tl.offsetMin=Vector2.zero; tl.offsetMax=Vector2.zero; tl.sizeDelta=new Vector2(0f,2f);
        _topLine.color=HudVisualTheme.ColorIdle;
        _divider=Img("DIV",transform); var dv=_divider.rectTransform;
        dv.anchorMin=new Vector2(0.05f,0f); dv.anchorMax=new Vector2(0.95f,0f);
        dv.pivot=new Vector2(0.5f,0f); dv.anchoredPosition=new Vector2(0f,144f); dv.sizeDelta=new Vector2(0f,0.5f);
        _divider.color=HudVisualTheme.PanelBorder;
        _trafLabel=Txt("TL",transform,8f,false); Place(_trafLabel,0f,1f,0f,1f,8f,-5f,-8f,-25f);
        _trafLabel.text=$"<color={HudVisualTheme.HexDim}>TRAFFIC</color>";
        _callsign=Txt("CS",transform,15.5f,false); Place(_callsign,0f,1f,0.72f,0.98f,8f,0f,-8f,0f);
        _riskBadge=Txt("RK",transform,9f,true); Place(_riskBadge,0.5f,1f,0.72f,0.98f,0f,0f,-8f,0f);
        float rh=0.165f;
        (_lDist,_vDist)=Row("Dist",0.565f,rh,"DISTANCE");
        (_lCpa, _vCpa) =Row("Cpa", 0.395f,rh,"CPA");
        (_lIn,  _vIn)  =Row("In",  0.225f,rh,"IN");
        (_lDir, _vDir) =Row("Dir", 0.055f,rh,"DIRECTION");
    }
    (TMP_Text,TMP_Text) Row(string id,float ancY,float h,string label){
        var L=Txt("L"+id,transform,9f,false);
        Place(L,0f,0.5f,ancY,ancY+h,10f,0f,0f,0f);
        L.text=$"<color={HudVisualTheme.HexDim}>{label}</color>";
        var V=Txt("V"+id,transform,9.5f,true);
        Place(V,0.4f,1f,ancY,ancY+h,0f,0f,-10f,0f);
        V.text=$"<color={HudVisualTheme.HexDim}>--</color>";
        return (L,V);
    }
    static void Place(TMP_Text t,float ax,float bx,float ay,float by,float ol,float ob,float or2,float ot){
        var r=t.rectTransform;
        r.anchorMin=new Vector2(ax,ay); r.anchorMax=new Vector2(bx,by);
        r.offsetMin=new Vector2(ol,ob); r.offsetMax=new Vector2(or2,ot);
    }
    public void ShowLow(int n,string dist,string cs=""){
        if(_topLine)_topLine.color=new Color(0,0,0,0);
        if(_callsign)_callsign.text=string.IsNullOrWhiteSpace(cs)?"":$"<color={HudVisualTheme.HexDim}>{cs}</color>";
        if(_riskBadge)_riskBadge.text="";
        if(_trafLabel)_trafLabel.text="";
        Set(_vDist,dist,"--",HudVisualTheme.HexDim);
        if(_lCpa!=null)_lCpa.text="";
        Set(_vCpa,"","",HudVisualTheme.HexDim);
        if(_vIn!=null)_vIn.text=$"<color={HudVisualTheme.HexDim}>{n} AC</color>";
        if(_lIn!=null)_lIn.text=$"<color={HudVisualTheme.HexDim}>NEARBY</color>";
        if(_vDir!=null)_vDir.text="";
        if(_lDir!=null)_lDir.text="";
        Reanchor(_lIn,0.395f,0.165f); Reanchor(_vIn,0.395f,0.165f);
    }
    public void ShowMedium(string cs,string dist,string cpa,string tcpa,string dir){
        if(_topLine)_topLine.color=HudVisualTheme.ColorMedium;
        if(_trafLabel)_trafLabel.text=$"<color={HudVisualTheme.HexDim}>TRAFFIC</color>";
        if(_lCpa!=null)_lCpa.text=$"<color={HudVisualTheme.HexDim}>CPA</color>";
        Reanchor(_lIn,0.225f,0.165f); Reanchor(_vIn,0.225f,0.165f);
        if(_callsign)_callsign.text=$"<color={HudVisualTheme.HexMedium}>{V(cs,"TARGET")}</color>";
        if(_riskBadge)_riskBadge.text=$"<color={HudVisualTheme.HexMedium}>MED</color>";
        Set(_vDist,dist,"--",HudVisualTheme.HexWhite);
        Set(_vCpa,cpa,"--",HudVisualTheme.HexWhite);
        Set(_vIn,tcpa,"--",HudVisualTheme.HexMedium);
        Set(_vDir,dir,"--",HudVisualTheme.HexDim);
        if(_lIn!=null)_lIn.text=$"<color={HudVisualTheme.HexDim}>IN</color>";
        if(_lDir!=null)_lDir.text=$"<color={HudVisualTheme.HexDim}>DIRECTION</color>";
    }
    public void ShowHigh(string cs,string dist,string cpa,string tcpa,string dir){
        if(_topLine)_topLine.color=HudVisualTheme.ColorHigh;
        if(_trafLabel)_trafLabel.text=$"<color={HudVisualTheme.HexDim}>TRAFFIC</color>";
        if(_lCpa!=null)_lCpa.text=$"<color={HudVisualTheme.HexDim}>CPA</color>";
        Reanchor(_lIn,0.225f,0.165f); Reanchor(_vIn,0.225f,0.165f);
        if(_callsign)_callsign.text=$"<color={HudVisualTheme.HexHigh}>{V(cs,"TARGET")}</color>";
        if(_riskBadge)_riskBadge.text=$"<color={HudVisualTheme.HexHigh}>HIGH</color>";
        Set(_vDist,dist,"--",HudVisualTheme.HexWhite);
        Set(_vCpa,cpa,"--",HudVisualTheme.HexWhite);
        Set(_vIn,tcpa,"--",HudVisualTheme.HexHigh);
        Set(_vDir,dir,"--",HudVisualTheme.HexHighSoft);
        if(_lIn!=null)_lIn.text=$"<color={HudVisualTheme.HexDim}>IN</color>";
        if(_lDir!=null)_lDir.text=$"<color={HudVisualTheme.HexDim}>DIRECTION</color>";
    }
    public void ShowEmpty(){
        if(_callsign)_callsign.text=""; if(_riskBadge)_riskBadge.text="";
        foreach(var t in new[]{_vDist,_vCpa,_vIn,_vDir}) if(t!=null)t.text="";
    }
    public TMP_Text GetCallsignLabel()=>_callsign;
    static void Set(TMP_Text t,string val,string fb,string col){if(t!=null)t.text=$"<color={col}>{(H(val)?val:fb)}</color>";}
    static bool H(string v)=>!string.IsNullOrWhiteSpace(v)&&v!="--";
    static string V(string v,string f)=>H(v)?v:f;
    static void Reanchor(TMP_Text t,float ancY,float h){ if(t==null)return; var r=t.rectTransform; r.anchorMin=new Vector2(r.anchorMin.x,ancY); r.anchorMax=new Vector2(r.anchorMax.x,ancY+h); }
    static RawImage Img(string n,Transform p){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(p,false);var i=go.GetComponent<RawImage>();i.texture=UnityEngine.Texture2D.whiteTexture;i.raycastTarget=false;return i;}
    static TMP_Text Txt(string n,Transform p,float sz,bool right){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(p,false);var t=go.GetComponent<TMP_Text>();t.fontSize=sz;t.alignment=right?TMPro.TextAlignmentOptions.Right:TMPro.TextAlignmentOptions.Left;t.enableWordWrapping=false;t.raycastTarget=false;t.richText=true;t.color=HudVisualTheme.ColorWhite;t.text="";return t;}
    static void Str(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
}}

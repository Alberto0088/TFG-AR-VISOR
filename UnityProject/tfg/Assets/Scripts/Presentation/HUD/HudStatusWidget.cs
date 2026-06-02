using System; using TMPro; using UnityEngine; using UnityEngine.UI;
namespace TFG.ARVisor.Presentation.HUD {
[RequireComponent(typeof(Canvas))][RequireComponent(typeof(CanvasGroup))]
public class HudStatusWidget : MonoBehaviour {
    const float PW=120f,PH=85f,WS=0.003f;
    Canvas _cv; CanvasGroup _cg;
    RawImage _bg,_topL;
    RawImage[] _bars=new RawImage[4];
    TMP_Text _modeLabel,_sysLabel,_coordTxt;
    string _gps="WAIT",_mode="--";
    static readonly float[] BAR_H={5f,9f,13f,17f};

    void Awake(){
        _cv=GetComponent<Canvas>(); _cv.renderMode=RenderMode.WorldSpace; _cv.sortingOrder=98;
        if(Camera.main!=null)_cv.worldCamera=Camera.main;
        _cg=GetComponent<CanvasGroup>(); _cg.interactable=false; _cg.blocksRaycasts=false;
        var rt=GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(PW,PH);
        transform.localScale=Vector3.one*WS; Build();
    }
    void Build(){
        _bg=I("BG",transform); S(_bg.rectTransform); _bg.color=HudVisualTheme.PanelBg;
        _topL=I("TL",transform); var tl=_topL.rectTransform;
        tl.anchorMin=new Vector2(0f,1f); tl.anchorMax=new Vector2(1f,1f);
        tl.pivot=new Vector2(0.5f,1f); tl.offsetMin=Vector2.zero; tl.offsetMax=Vector2.zero; tl.sizeDelta=new Vector2(0f,2f);
        _topL.color=HudVisualTheme.ColorIdle;
        for(int i=0;i<4;i++){
            _bars[i]=I("B"+i,transform); var br=_bars[i].rectTransform;
            br.anchorMin=new Vector2(0f,0f); br.anchorMax=new Vector2(0f,0f);
            br.pivot=new Vector2(0f,0f);
            br.anchoredPosition=new Vector2(10f+i*7f,38f);
            br.sizeDelta=new Vector2(4f,BAR_H[i]);
            _bars[i].color=HudVisualTheme.ColorDim;
        }
        _modeLabel=T("ML",transform,9.5f,false); var mr=_modeLabel.rectTransform;
        mr.anchorMin=new Vector2(0f,0.5f); mr.anchorMax=new Vector2(1f,1f);
        mr.offsetMin=new Vector2(44f,2f); mr.offsetMax=new Vector2(-6f,-2f);
        _sysLabel=T("SL",transform,8f,false); var sr=_sysLabel.rectTransform;
        sr.anchorMin=new Vector2(0f,0.25f); sr.anchorMax=new Vector2(1f,0.52f);
        sr.offsetMin=new Vector2(10f,0f); sr.offsetMax=new Vector2(-6f,0f);
        _coordTxt=T("CT",transform,8f,false); var cr=_coordTxt.rectTransform;
        cr.anchorMin=new Vector2(0f,0f); cr.anchorMax=new Vector2(1f,0.27f);
        cr.offsetMin=new Vector2(10f,2f); cr.offsetMax=new Vector2(-6f,0f);
        _coordTxt.text=$"<color={HudVisualTheme.HexDim}>--  --</color>";
    }
    public void Render(string sys,string gps,string mode,string rate){_gps=gps;_mode=mode;Ref();}
    public void RenderWaiting(){_gps="WAIT";_mode="--";Ref();if(_coordTxt)_coordTxt.text=$"<color={HudVisualTheme.HexDim}>--  --</color>";}
    public void RenderSimMode(string sys){_gps="SIM";_mode="SIM";Ref();if(_coordTxt)_coordTxt.text=$"<color={HudVisualTheme.HexDim}>SIM MODE</color>";}
    public void SetCoordinates(double? lat,double? lon,double? alt){
        if(_coordTxt==null)return;
        if(!lat.HasValue||!lon.HasValue){_coordTxt.text=$"<color={HudVisualTheme.HexDim}>--  --</color>";return;}
        string ls=$"{Math.Abs(lat.Value):0.000}{(lat.Value>=0?"N":"S")}";
        string lo=$"{Math.Abs(lon.Value):0.000}{(lon.Value>=0?"E":"W")}";
        _coordTxt.text=$"<color={HudVisualTheme.HexDim}>{ls}  {lo}</color>";
    }
    void Ref(){
        bool ok=_gps=="REAL",sim=_gps=="SIM",d3=_mode=="3D";
        int lit=ok?4:sim?0:1;
        Color bc=ok?HudVisualTheme.ColorLow:sim?HudVisualTheme.ColorDim:HudVisualTheme.ColorMedium;
        for(int i=0;i<4;i++) if(_bars[i]) _bars[i].color=i<lit?bc:new Color(0.25f,0.25f,0.25f,0.5f);
        if(_topL)_topL.color=ok?HudVisualTheme.ColorLow:sim?HudVisualTheme.ColorDim:HudVisualTheme.ColorMedium;
        if(_modeLabel==null)return;
        string gc=ok?HudVisualTheme.HexLow:sim?HudVisualTheme.HexDim:HudVisualTheme.HexMedium;
        string mc=d3?HudVisualTheme.HexLow:HudVisualTheme.HexDim;
        string gl=ok?"GPS OK":sim?"GPS SIM":"GPS WAIT";
        _modeLabel.text=$"<color={gc}>{gl}</color>  <color={mc}>{_mode}</color>";
        if(_sysLabel)_sysLabel.text=$"<color={HudVisualTheme.HexDim}>SYS ONLINE</color>";
    }
    static RawImage I(string n,Transform p){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(p,false);var i=go.GetComponent<RawImage>();i.texture=UnityEngine.Texture2D.whiteTexture;i.raycastTarget=false;return i;}
    static TMP_Text T(string n,Transform p,float sz,bool r){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(p,false);var t=go.GetComponent<TMP_Text>();t.fontSize=sz;t.alignment=r?TMPro.TextAlignmentOptions.Right:TMPro.TextAlignmentOptions.Left;t.enableWordWrapping=false;t.raycastTarget=false;t.richText=true;t.color=HudVisualTheme.ColorWhite;t.text="";return t;}
    static void S(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;}
}}

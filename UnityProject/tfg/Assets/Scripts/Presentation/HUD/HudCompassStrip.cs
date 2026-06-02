using TMPro; using UnityEngine; using UnityEngine.UI;
using TFG.ARVisor.Domain.Models;
namespace TFG.ARVisor.Presentation.HUD {
[RequireComponent(typeof(Canvas))][RequireComponent(typeof(CanvasGroup))]
public class HudCompassStrip : MonoBehaviour {
    const float PW=640f,PH=54f,WS=0.003f;
    const int STEPS=9,DS=10;
    Canvas _cv; CanvasGroup _cg;
    RawImage _bg,_topL,_botL,_cMark;
    RawImage[] _ticks = new RawImage[STEPS];
    TMP_Text[] _nums  = new TMP_Text[STEPS];
    TMP_Text _hdg;
    double _tBear=double.NaN; RiskLevel _risk=RiskLevel.Low; bool _hasTgt;

    void Awake(){
        _cv=GetComponent<Canvas>(); _cv.renderMode=RenderMode.WorldSpace; _cv.sortingOrder=98;
        if(Camera.main!=null)_cv.worldCamera=Camera.main;
        _cg=GetComponent<CanvasGroup>(); _cg.interactable=false; _cg.blocksRaycasts=false;
        var rt=GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(PW,PH);
        transform.localScale=Vector3.one*WS; Build();
    }
    void Build(){
        _bg=Img("BG",transform); Str(_bg.rectTransform); _bg.color=new Color(0f,0f,0f,0f);
        _topL=HLine("TL",transform,1f,true); _topL.color=new Color(1f,1f,1f,0.18f);
        _botL=HLine("BL",transform,1f,false); _botL.color=new Color(0f,0f,0f,0f);
        _cMark=Img("CM",transform);
        var cm=_cMark.rectTransform;
        cm.anchorMin=new Vector2(0.5f,1f); cm.anchorMax=new Vector2(0.5f,1f);
        cm.pivot=new Vector2(0.5f,1f); cm.anchoredPosition=Vector2.zero; cm.sizeDelta=new Vector2(2f,7f);
        _cMark.color=Color.white;
        float slotW=66f, startX=-(STEPS/2)*slotW;
        for(int i=0;i<STEPS;i++){
            float xp=startX+i*slotW;
            _ticks[i]=Img("TK"+i,transform);
            var tr=_ticks[i].rectTransform;
            tr.anchorMin=new Vector2(0.5f,0.52f); tr.anchorMax=new Vector2(0.5f,0.52f);
            tr.pivot=new Vector2(0.5f,0f);
            tr.anchoredPosition=new Vector2(xp,0f); tr.sizeDelta=new Vector2(1.5f,9f);
            _ticks[i].color=new Color(1f,1f,1f,0.30f);
            _nums[i]=Lbl("NM"+i,transform,9.5f);
            var nr=_nums[i].rectTransform;
            nr.anchorMin=new Vector2(0.5f,0f); nr.anchorMax=new Vector2(0.5f,0.52f);
            nr.offsetMin=new Vector2(xp-32f,2f); nr.offsetMax=new Vector2(xp+32f,0f);
        }
        _hdg=Lbl("HDG",transform,8.5f);
        var hr=_hdg.rectTransform;
        hr.anchorMin=new Vector2(0.5f,0f); hr.anchorMax=new Vector2(0.5f,0.52f);
        hr.offsetMin=new Vector2(-20f,-1f); hr.offsetMax=new Vector2(20f,0f);
    }
    void Update(){
        float yaw=Camera.main!=null?Camera.main.transform.eulerAngles.y:0f;
        int center=UnityEngine.Mathf.RoundToInt(yaw/DS)*DS;
        for(int i=0;i<STEPS;i++){
            int raw=center+(i-STEPS/2)*DS;
            int nd=((raw%360)+360)%360;
            bool isc=(i==STEPS/2), ist=_hasTgt&&Near(nd,_tBear,9f);
            bool isCard=Card(nd)!=null;
            string lbl=Card(nd)??(nd==0?"N":$"{nd:000}");
            string col=isc?"#FFFFFF":ist?HudVisualTheme.GetRiskHex(_risk):isCard?HudVisualTheme.HexIdle:HudVisualTheme.HexDim;
            if(_nums[i]!=null)_nums[i].text=$"<color={col}>{lbl}</color>";
            if(_ticks[i]!=null){
                float h=isc?14f:isCard?11f:7f;
                _ticks[i].rectTransform.sizeDelta=new Vector2(1.5f,h);
                _ticks[i].color=isc?Color.white:ist?HudVisualTheme.GetRiskColor(_risk):new Color(1f,1f,1f,isCard?0.45f:0.20f);
            }
        }
        if(_hdg!=null)_hdg.text=$"<color={HudVisualTheme.HexDim}>{(int)yaw:000}</color>";
        Color lc=_hasTgt?HudVisualTheme.GetRiskColor(_risk):new Color(1f,1f,1f,0.18f);
        if(_topL)_topL.color=lc;
    }
    public void SetTarget(double b,RiskLevel r){_tBear=b;_risk=r;_hasTgt=true;}
    public void ClearTarget(){_tBear=double.NaN;_hasTgt=false;_risk=RiskLevel.Low;}
    static bool Near(int nd,double b,float t){if(double.IsNaN(b))return false;return UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(nd,(float)b))<=t;}
    static string Card(int d){switch(d){case 0:return"N";case 45:return"NE";case 90:return"E";case 135:return"SE";case 180:return"S";case 225:return"SW";case 270:return"W";case 315:return"NW";default:return null;}}
    static RawImage Img(string n,Transform p){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(p,false);var i=go.GetComponent<RawImage>();i.texture=UnityEngine.Texture2D.whiteTexture;i.raycastTarget=false;return i;}
    static TMP_Text Lbl(string n,Transform p,float sz){var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(p,false);var t=go.GetComponent<TMP_Text>();t.fontSize=sz;t.alignment=TMPro.TextAlignmentOptions.Center;t.enableWordWrapping=false;t.raycastTarget=false;t.richText=true;t.color=HudVisualTheme.ColorWhite;t.text="";return t;}
    static RawImage HLine(string n,Transform p,float h,bool top){var i=Img(n,p);var r=i.rectTransform;r.anchorMin=new Vector2(0f,top?1f:0f);r.anchorMax=new Vector2(1f,top?1f:0f);r.pivot=new Vector2(0.5f,top?1f:0f);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;r.sizeDelta=new Vector2(0f,h);return i;}
    static void Str(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
}}

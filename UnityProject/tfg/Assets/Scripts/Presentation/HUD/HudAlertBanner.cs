using TMPro; using UnityEngine; using UnityEngine.UI;
namespace TFG.ARVisor.Presentation.HUD {
[RequireComponent(typeof(Canvas))][RequireComponent(typeof(CanvasGroup))]
public class HudAlertBanner : MonoBehaviour {
    const float PW=380f,PH=34f,WS=0.003f;
    Canvas _cv; CanvasGroup _cg;
    RawImage _bg,_lBar,_rBar; TMP_Text _txt;
    void Awake(){
        _cv=GetComponent<Canvas>(); _cv.renderMode=RenderMode.WorldSpace; _cv.sortingOrder=99;
        if(Camera.main!=null)_cv.worldCamera=Camera.main;
        _cg=GetComponent<CanvasGroup>(); _cg.interactable=false; _cg.blocksRaycasts=false;
        var rt=GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(PW,PH);
        transform.localScale=Vector3.one*WS; Build(); gameObject.SetActive(false);
    }
    void Build(){
        _bg=I("BG",transform); S(_bg.rectTransform); _bg.color=HudVisualTheme.PanelBg;
        _lBar=I("L",transform); var lr=_lBar.rectTransform;
        lr.anchorMin=new Vector2(0f,0.15f); lr.anchorMax=new Vector2(0f,0.85f);
        lr.pivot=new Vector2(0f,0.5f); lr.anchoredPosition=Vector2.zero; lr.sizeDelta=new Vector2(2f,0f);
        _lBar.color=HudVisualTheme.ColorIdle;
        _rBar=I("R",transform); var rr=_rBar.rectTransform;
        rr.anchorMin=new Vector2(1f,0.15f); rr.anchorMax=new Vector2(1f,0.85f);
        rr.pivot=new Vector2(1f,0.5f); rr.anchoredPosition=Vector2.zero; rr.sizeDelta=new Vector2(2f,0f);
        _rBar.color=HudVisualTheme.ColorIdle;
        _txt=T("Txt",transform,11.5f); var tr=_txt.rectTransform;
        tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one;
        tr.offsetMin=new Vector2(8f,0f); tr.offsetMax=new Vector2(-8f,0f);
        _txt.alignment=TMPro.TextAlignmentOptions.Center;
    }
    public void ShowMedium(string msg){
        gameObject.SetActive(true);
        if(_lBar)_lBar.color=HudVisualTheme.ColorMedium;
        if(_rBar)_rBar.color=HudVisualTheme.ColorMedium;
        if(_txt)_txt.text=$"<color={HudVisualTheme.HexMedium}>{msg}</color>";
    }
    public void ShowHigh(string msg){
        gameObject.SetActive(true);
        if(_lBar)_lBar.color=HudVisualTheme.ColorHigh;
        if(_rBar)_rBar.color=HudVisualTheme.ColorHigh;
        if(_txt)_txt.text=$"<color={HudVisualTheme.HexHigh}>{msg}</color>";
    }
    public void Hide(){ gameObject.SetActive(false); }
    static RawImage I(string n,Transform p){ var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)); go.transform.SetParent(p,false); var i=go.GetComponent<RawImage>(); i.texture=UnityEngine.Texture2D.whiteTexture; i.raycastTarget=false; return i; }
    static TMP_Text T(string n,Transform p,float sz){ var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI)); go.transform.SetParent(p,false); var t=go.GetComponent<TMP_Text>(); t.fontSize=sz; t.enableWordWrapping=false; t.raycastTarget=false; t.richText=true; t.color=HudVisualTheme.ColorWhite; t.text=""; return t; }
    static void S(RectTransform rt){ rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero; }
}}

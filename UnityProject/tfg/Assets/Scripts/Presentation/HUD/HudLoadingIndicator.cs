using TMPro; using UnityEngine; using UnityEngine.UI;
namespace TFG.ARVisor.Presentation.HUD {
[RequireComponent(typeof(Canvas))][RequireComponent(typeof(CanvasGroup))]
public class HudLoadingIndicator : MonoBehaviour {
    const float PW=300f,PH=28f,WS=0.003f;
    const float SI=0.15f;
    Canvas _cv; CanvasGroup _cg;
    TMP_Text _spin,_msg;
    float _t; int _f;
    void Awake(){
        _cv=GetComponent<Canvas>(); _cv.renderMode=RenderMode.WorldSpace; _cv.sortingOrder=97;
        if(Camera.main!=null)_cv.worldCamera=Camera.main;
        _cg=GetComponent<CanvasGroup>(); _cg.interactable=false; _cg.blocksRaycasts=false;
        var rt=GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(PW,PH);
        transform.localScale=Vector3.one*WS; Build(); gameObject.SetActive(false);
    }
    void Build(){
        _spin=T("S",transform,8.5f,TMPro.TextAlignmentOptions.Center);
        var sr=_spin.rectTransform;
        sr.anchorMin=new Vector2(0f,0f); sr.anchorMax=new Vector2(0.12f,1f);
        sr.offsetMin=new Vector2(0f,0f); sr.offsetMax=Vector2.zero;
        _spin.color=HudVisualTheme.ColorDim; _spin.text="|";
        _msg=T("M",transform,8.5f,TMPro.TextAlignmentOptions.Left);
        var mr=_msg.rectTransform;
        mr.anchorMin=new Vector2(0.12f,0f); mr.anchorMax=new Vector2(1f,1f);
        mr.offsetMin=new Vector2(4f,0f); mr.offsetMax=new Vector2(-4f,0f);
        _msg.color=HudVisualTheme.ColorDim; _msg.text="SCANNING AIRSPACE";
    }
    void Update(){
        _t+=Time.deltaTime; if(_t<SI)return; _t=0f;
        _f=(_f+1)%HudVisualTheme.SpinnerFrames.Length;
        if(_spin!=null)_spin.text=HudVisualTheme.SpinnerFrames[_f].ToString();
    }
    public void Show(string msg){ gameObject.SetActive(true); if(_msg!=null)_msg.text=msg; }
    public void Hide(){ gameObject.SetActive(false); }
    static TMP_Text T(string n,Transform p,float sz,TMPro.TextAlignmentOptions a){
        var go=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI));
        go.transform.SetParent(p,false);
        var t=go.GetComponent<TMP_Text>();
        t.fontSize=sz; t.alignment=a; t.enableWordWrapping=false; t.raycastTarget=false;
        t.richText=true; t.color=HudVisualTheme.ColorDim; t.text=""; return t;
    }
}}

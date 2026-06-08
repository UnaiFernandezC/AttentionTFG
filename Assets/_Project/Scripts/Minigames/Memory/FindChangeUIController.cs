using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FindChangeUIController : MonoBehaviour
{

    TextMeshProUGUI _timerLbl;
    TextMeshProUGUI _phaseLbl;
    Image           _bgPanel;
    Image           _overlayFlash;

    GameObject      _resultPanel;
    TextMeshProUGUI _resultTitle;
    TextMeshProUGUI _resultSub;

    static Color C(float r,float g,float b,float a=1f) => new Color(r,g,b,a);
    static readonly Color BG      = C(0.10f, 0.12f, 0.18f);
    static readonly Color HDR     = C(0.07f, 0.09f, 0.16f);
    static readonly Color PANEL   = C(0.09f, 0.12f, 0.22f);
    static readonly Color ACCENT  = C(0.40f, 0.70f, 1.00f);
    static readonly Color DIM     = C(0.48f, 0.60f, 0.78f);
    static readonly Color DIM2    = C(0.32f, 0.44f, 0.62f);
    static readonly Color CGREEN  = C(0.28f, 0.88f, 0.52f);
    static readonly Color CRED    = C(0.90f, 0.28f, 0.32f);
    static Vector2 V(float x,float y) => new Vector2(x,y);

    public RectTransform BuildUI(Action onRestart, Action onMenu)
    {
        var cGO = new GameObject("Canvas_FindChange");
        cGO.transform.SetParent(transform, false);
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();
        var R = cGO.GetComponent<RectTransform>();

        _bgPanel = MkImg(R,"BG",BG,V(0,0),V(1,1),V(0,0),V(0,0)).img;

        BuildGridDecor(R);

        var hdr = MkImg(R,"Hdr",HDR,V(0,1),V(1,1),V(0,-44),V(0,88)).rt;
        MkImg(hdr,"Line",ACCENT,V(0,0),V(1,0),V(0,1.5f),V(0,3f));
        MkImg(hdr,"AccL",ACCENT,V(0,0.18f),V(0,0.82f),V(3,0),V(6,0));
        var ttl = MkTxt(hdr,"Title","CAMBIOS SUTILES",Color.white,35,V(0.03f,0.12f),V(0.58f,0.88f));
        ttl.fontStyle = FontStyles.Bold; ttl.alignment = TextAlignmentOptions.MidlineLeft; ttl.characterSpacing = 2f;
        MkTxt(hdr,"Cat","MEMORIA",DIM2,16,V(0.60f,0.12f),V(0.97f,0.88f))
            .alignment = TextAlignmentOptions.MidlineRight;

        _phaseLbl = MkTxt(R,"Phase","",ACCENT,30,V(0.05f,0.84f),V(0.60f,0.92f));
        _phaseLbl.fontStyle = FontStyles.Bold;
        _phaseLbl.alignment = TextAlignmentOptions.MidlineLeft;
        _phaseLbl.characterSpacing = 3f;

        _timerLbl = MkTxt(R,"Timer","",Color.white,42,V(0.72f,0.84f),V(0.97f,0.94f));
        _timerLbl.fontStyle = FontStyles.Bold;
        _timerLbl.alignment = TextAlignmentOptions.MidlineRight;

        var gameArea = MkImg(R,"GameArea",C(0,0,0,0),V(0.05f,0.10f),V(0.95f,0.84f),V(0,0),V(0,0)).rt;
        gameArea.GetComponent<Image>().raycastTarget = false;

        _overlayFlash = MkImg(R,"Flash",C(0,0,0,0),V(0,0),V(1,1),V(0,0),V(0,0)).img;
        _overlayFlash.raycastTarget = false;

        var bot = MkImg(R,"Bot",HDR,V(0,0),V(1,0),V(0,40),V(0,80)).rt;
        MkImg(bot,"BotLine",ACCENT,V(0,1),V(1,1),V(0,-1.5f),V(0,3));
        MkTxt(bot,"Instr","Observa la escena y haz clic en el elemento que haya cambiado.",
            C(ACCENT.r+0.1f,ACCENT.g+0.1f,ACCENT.b+0.1f,1f),
            18,V(0.01f,0),V(0.78f,1)).alignment = TextAlignmentOptions.MidlineLeft;
        MkImg(bot,"Sep",C(1,1,1,0.10f),V(0.78f,0.1f),V(0.782f,0.9f),V(0,0),V(0,0));

        BuildResultPanel(R, onRestart, onMenu);

        return gameArea;
    }

    void BuildGridDecor(RectTransform R)
    {

        for (int i = 1; i < 5; i++)
        {
            float x = i * 0.2f;
            MkImg(R,"Gv"+i,C(1,1,1,0.015f),V(x-0.001f,0.09f),V(x+0.001f,0.85f),V(0,0),V(0,0));
        }
        for (int j = 1; j < 4; j++)
        {
            float y = 0.09f + j * ((0.85f - 0.09f) / 4f);
            MkImg(R,"Gh"+j,C(1,1,1,0.015f),V(0.05f,y-0.001f),V(0.95f,y+0.001f),V(0,0),V(0,0));
        }
    }

    void BuildResultPanel(RectTransform R, Action onRestart, Action onMenu)
    {
        _resultPanel = new GameObject("ResultPanel");
        _resultPanel.transform.SetParent(R, false);
        var er = _resultPanel.AddComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.sizeDelta = Vector2.zero; er.anchoredPosition = Vector2.zero;
        _resultPanel.AddComponent<Image>().color = C(0,0,0,0.82f);

        var card = MkImg(er,"Card",PANEL,V(0.5f,0.5f),V(0.5f,0.5f),V(0,0),V(780f,460f)).rt;
        MkImg(card,"Shine",C(1,1,1,0.03f),V(0,0.5f),V(1,1),V(0,0),V(0,0));
        MkImg(card,"LineTop",ACCENT,V(0,1),V(1,1),V(0,-4),V(0,8));
        MkImg(card,"AccL",ACCENT,V(0,0.08f),V(0,0.92f),V(4,0),V(8,0));

        _resultTitle = MkTxt(card,"RT","",Color.white,58,V(0.05f,0.72f),V(0.95f,0.97f));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultSub   = MkTxt(card,"RS","",DIM,24,V(0.05f,0.52f),V(0.95f,0.72f));
        _resultSub.overflowMode = TextOverflowModes.Overflow;

        MkBtn(card,"Jugar de nuevo",ACCENT,V(0.05f,0.05f),V(0.95f,0.18f),onRestart);
        _resultPanel.SetActive(false);
    }

    public void SetPhase(string label, Color col)
    {
        if (_phaseLbl) { _phaseLbl.text = label; _phaseLbl.color = col; }
    }

    public void SetTimer(float t, float max)
    {
        if (_timerLbl == null) return;
        int secs = Mathf.CeilToInt(t);
        _timerLbl.text  = secs.ToString();
        _timerLbl.color = t < max * 0.35f ? CRED : Color.white;
    }

    public void HideTimer() { if (_timerLbl) _timerLbl.text = ""; }

    public void SetFlash(float alpha)
    {
        if (_overlayFlash) _overlayFlash.color = C(0,0,0,alpha);
    }

    public void HighlightCorrect(ElementData e)
    {
        if (e?.Go == null) return;

        var hGO = new GameObject("Highlight");
        hGO.transform.SetParent(e.RT, false);
        var hRT = hGO.AddComponent<RectTransform>();
        hRT.anchorMin = Vector2.zero; hRT.anchorMax = Vector2.one;
        hRT.sizeDelta = new Vector2(12f, 12f);
        hRT.anchoredPosition = Vector2.zero;
        hGO.AddComponent<Image>().color = CGREEN;

        hGO.transform.SetAsFirstSibling();
    }

    public void HighlightWrong(ElementData wrong, ElementData correct)
    {

        if (wrong?.Img  != null) wrong.Img.color  = new Color(0.85f, 0.22f, 0.22f);

        HighlightCorrect(correct);
    }

    public void ShowResult(bool correct, string sub)
    {
        _resultTitle.text  = correct ? "¡Correcto!" : "Fallaste";
        _resultTitle.color = correct ? CGREEN : CRED;
        _resultSub.text    = sub;
        _resultPanel.SetActive(true);
    }

    struct UIR { public RectTransform rt; public Image img; }
    UIR MkImg(RectTransform p,string n,Color col,Vector2 amin,Vector2 amax,Vector2 pos,Vector2 sd)
    {
        var go=new GameObject(n); go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=amin; rt.anchorMax=amax; rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=pos; rt.sizeDelta=sd;
        var img=go.AddComponent<Image>(); img.color=col;
        return new UIR{rt=rt,img=img};
    }
    TextMeshProUGUI MkTxt(RectTransform p,string n,string txt,Color col,float sz,Vector2 amin,Vector2 amax)
    {
        var go=new GameObject(n); go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=amin; rt.anchorMax=amax; rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=Vector2.zero; rt.sizeDelta=Vector2.zero;
        var t=go.AddComponent<TextMeshProUGUI>();
        t.text=txt; t.color=col; t.fontSize=sz;
        t.alignment=TextAlignmentOptions.Center; t.overflowMode=TextOverflowModes.Overflow;
        return t;
    }
    void MkBtn(RectTransform p,string lbl,Color bg,Vector2 amin,Vector2 amax,Action click)
    {
        var r=MkImg(p,"Btn_"+lbl,bg,amin,amax,V(0,0),V(0,0));
        MkImg(r.rt,"Sh",C(1,1,1,.09f),V(0,.5f),V(1,1),V(0,0),V(0,0));
        var b=r.rt.gameObject.AddComponent<Button>(); b.targetGraphic=r.img;
        var cb=b.colors; cb.normalColor=Color.white;
        cb.highlightedColor=new Color(1,1,1,.82f); cb.pressedColor=new Color(.72f,.72f,.72f);
        b.colors=cb; b.onClick.AddListener(()=>click?.Invoke());
        var t=MkTxt(r.rt,"T",lbl,Color.white,24,V(0,0),V(1,1));
        t.fontStyle=FontStyles.Bold;
    }
}

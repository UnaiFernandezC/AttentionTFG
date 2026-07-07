// @made by Unai Fernandez Cobos - @unaifdezcobos@gmail.com
using UnityEngine;

public class ChangeManager : MonoBehaviour
{
    [Header("0=obvio 1=sutil 2=muy sutil")]
    public int changeSubtlety = 0;

    [Header("0=solo color, 1=color+size, 2=size+swap")]
    public int changeTypeMask = 0;

    static readonly Color[] PALETTE =
    {
        new Color(0.92f, 0.25f, 0.28f), new Color(0.22f, 0.52f, 0.92f),
        new Color(0.18f, 0.78f, 0.38f), new Color(0.96f, 0.78f, 0.12f),
        new Color(0.68f, 0.22f, 0.90f), new Color(0.96f, 0.52f, 0.12f),
        new Color(0.12f, 0.82f, 0.88f), new Color(0.92f, 0.28f, 0.68f),
        new Color(0.42f, 0.82f, 0.22f), new Color(0.28f, 0.68f, 0.88f),
        new Color(0.88f, 0.42f, 0.22f), new Color(0.58f, 0.92f, 0.42f),
    };

    public int  ChangedElementId { get; private set; } = -1;
    public string ChangeDescription { get; private set; } = "";

    public void ApplyChange(ElementData[] elements)
    {
        if (elements == null || elements.Length == 0) return;

        int targetIdx = Random.Range(0, elements.Length);
        var e = elements[targetIdx];
        e.IsChanged = true;
        ChangedElementId = e.Id;

        int type = PickChangeType();

        switch (type)
        {
            case 0: ApplyColorChange(e);    break;
            case 1: ApplySizeChange(e);     break;
            case 2: ApplyPositionSwap(e, elements); break;
        }

        SceneGenerator.ApplyState(e);
    }

    void ApplyColorChange(ElementData e)
    {

        Color orig = e.CurColor;
        Color next = orig;
        int   tries = 0;
        while (ColorDistance(next, orig) < (changeSubtlety == 0 ? 0.6f : 0.35f) && tries < 30)
        {
            next  = PALETTE[Random.Range(0, PALETTE.Length)];
            tries++;
        }

        if (changeSubtlety == 2)
            next = Color.Lerp(orig, next, 0.40f);

        e.CurColor = next;
        ChangeDescription = "color";
    }

    void ApplySizeChange(ElementData e)
    {
        float factor = changeSubtlety == 0 ? 0.30f
                     : changeSubtlety == 1 ? 0.20f : 0.12f;
        bool  grow   = Random.value > 0.5f;
        e.CurSize    = e.OrigSize * (grow ? 1f + factor : 1f - factor);
        ChangeDescription = grow ? "tamaño (mayor)" : "tamaño (menor)";
    }

    void ApplyPositionSwap(ElementData e, ElementData[] all)
    {

        int other = Random.Range(0, all.Length);
        if (other == e.Id) other = (other + 1) % all.Length;
        var b = all[other];

        Vector2 tmp  = e.CurPos;
        e.CurPos     = b.CurPos;
        b.CurPos     = tmp;
        SceneGenerator.ApplyState(b);
        ChangeDescription = "posición";
    }

    int PickChangeType()
    {
        if (changeTypeMask == 0) return 0;
        if (changeTypeMask == 1) return Random.value > 0.5f ? 0:1;
        return Random.value > 0.5f ? 1 : 2;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        return Mathf.Sqrt(dr*dr + dg*dg + db*db);
    }
}

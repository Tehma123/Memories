using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PixelBorder : MonoBehaviour
{
    [SerializeField] private int thickness = 3;
    [SerializeField] private Color borderColor = new Color(0.86f, 0.86f, 0.86f, 1f);

    void Start()
    {
        // Tạo 4 cạnh bằng child Image
        CreateEdge("Top",    new Vector2(0,1),   new Vector2(1,1),   thickness, 0);
        CreateEdge("Bottom", new Vector2(0,0),   new Vector2(1,0),   thickness, 0);
        CreateEdge("Left",   new Vector2(0,0),   new Vector2(0,1),   0, thickness);
        CreateEdge("Right",  new Vector2(1,0),   new Vector2(1,1),   0, thickness);
    }

    void CreateEdge(string edgeName, Vector2 anchorMin, Vector2 anchorMax, int h, int v)
    {
        var go  = new GameObject(edgeName);
        go.transform.SetParent(transform, false);

        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = Vector2.zero;
        rt.offsetMax   = Vector2.zero;
        rt.sizeDelta   = new Vector2(v > 0 ? thickness : 0, h > 0 ? thickness : 0);

        var img        = go.AddComponent<Image>();
        img.color      = borderColor;
    }
}
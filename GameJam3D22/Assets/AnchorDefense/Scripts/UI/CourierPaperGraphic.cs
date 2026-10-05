using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    // An editable, resolution-independent torn-paper silhouette, not a rectangular panel.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierPaperGraphic : MaskableGraphic
    {
        public Color ink = new Color(0.25f, 0.17f, 0.29f, 1f);
        [Min(0f)] public float border = 2f;
        public bool stationeryDetails;
        public bool pawStamp;
        public Color tapeColor = new Color(.73f,.52f,.56f,.75f);
        public Color accentColor = new Color(.63f,.43f,.24f,.65f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2[] points = { new Vector2(.025f,.13f), new Vector2(0,.43f), new Vector2(.016f,.84f),
                new Vector2(.055f,.97f), new Vector2(.28f,1), new Vector2(.53f,.975f),
                new Vector2(.79f,1), new Vector2(.965f,.94f), new Vector2(1,.72f),
                new Vector2(.981f,.38f), new Vector2(1,.12f), new Vector2(.92f,.025f),
                new Vector2(.68f,.05f), new Vector2(.44f,0), new Vector2(.20f,.035f) };
            Fill(vh, r, points, ink);
            r.xMin += border; r.xMax -= border; r.yMin += border; r.yMax -= border;
            Fill(vh, r, points, color);
            if (stationeryDetails)
            {
                // Small washi tape and a folded corner sit inside the existing
                // padding, so decoration never consumes text or pointer space.
                Quad(vh, new Vector2(r.xMin+27,r.yMax-8), new Vector2(r.xMin+73,r.yMax-5),
                    new Vector2(r.xMin+74,r.yMax-12), new Vector2(r.xMin+28,r.yMax-15), tapeColor);
                Quad(vh, new Vector2(r.xMax-22,r.yMax-7), new Vector2(r.xMax-10,r.yMax-12),
                    new Vector2(r.xMax-14,r.yMax-25), new Vector2(r.xMax-22,r.yMax-7), accentColor);
                if (pawStamp)
                {
                    Vector2 p = new Vector2(r.xMax-31,r.yMin+12);
                    Ellipse(vh,p,new Vector2(5,3.5f),accentColor);
                    for (int i=0;i<4;i++) Ellipse(vh,p+new Vector2(-6+i*4,5+Mathf.Sin(i*Mathf.PI/3)*2),new Vector2(1.8f,2.2f),accentColor);
                }
            }
        }
        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int n=vh.currentVertCount;
            vh.AddVert(a,tint,Vector2.zero); vh.AddVert(b,tint,Vector2.zero);
            vh.AddVert(c,tint,Vector2.zero); vh.AddVert(d,tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
        private static void Ellipse(VertexHelper vh, Vector2 center, Vector2 size, Color tint)
        {
            int n=vh.currentVertCount; vh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<12;i++)
            {
                float a=-i*Mathf.PI/6;
                vh.AddVert(center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y),tint,Vector2.zero);
            }
            for(int i=0;i<12;i++) vh.AddTriangle(n,n+1+i,n+1+(i+1)%12);
        }
        private static void Fill(VertexHelper vh, Rect r, Vector2[] points, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(r.center, tint, Vector2.zero);
            foreach (Vector2 p in points) vh.AddVert(new Vector2(r.x + p.x * r.width, r.y + p.y * r.height), tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
        }
    }
}

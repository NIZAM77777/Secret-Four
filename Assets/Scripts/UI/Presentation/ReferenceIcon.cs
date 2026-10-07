using UnityEngine;
using UnityEngine.UI;

namespace SecretFour.Presentation
{
    // Vector artwork: all dimensions, colors and transforms remain editable in the Inspector.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Secret Four/UI/Reference Icon")]
    public class ReferenceIcon : MaskableGraphic
    {
        public enum Shape { Crown, Back, Up, Down, Left, Right, Pause, People, Robot, Book, Bag, Plus, Enter, Check, Gear }
        public Shape shape;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(shape)
            {
                case Shape.Crown:
                    Poly(vh,new[]{new Vector2(.13f,.24f),new Vector2(.05f,.69f),new Vector2(.29f,.56f),new Vector2(.5f,.94f),new Vector2(.71f,.56f),new Vector2(.95f,.69f),new Vector2(.87f,.24f)});
                    Line(vh,.16f,.15f,.84f,.15f,.05f); break;
                case Shape.Back: Line(vh,.64f,.82f,.31f,.5f,.105f); Line(vh,.31f,.5f,.64f,.18f,.105f); break;
                case Shape.Pause: Box(vh,.24f,.2f,.18f,.6f);Box(vh,.58f,.2f,.18f,.6f);break;
                case Shape.Check:Line(vh,.18f,.48f,.4f,.25f,.1f);Line(vh,.4f,.25f,.84f,.77f,.1f);break;
                case Shape.Up: case Shape.Down: case Shape.Left: case Shape.Right:
                    Poly(vh,new[]{new Vector2(.34f,.12f),new Vector2(.66f,.12f),new Vector2(.66f,.52f),new Vector2(.9f,.52f),new Vector2(.5f,.94f),new Vector2(.1f,.52f),new Vector2(.34f,.52f)},shape==Shape.Down?180:shape==Shape.Left?90:shape==Shape.Right?-90:0);break;
                case Shape.People:
                    Disc(vh,.34f,.72f,.15f);Disc(vh,.7f,.72f,.15f);Box(vh,.13f,.13f,.4f,.38f);Box(vh,.56f,.13f,.31f,.38f);break;
                case Shape.Robot:
                    Outline(vh,.12f,.18f,.76f,.57f,.055f);Line(vh,.5f,.75f,.5f,.93f,.05f);Disc(vh,.5f,.93f,.055f);Disc(vh,.35f,.47f,.055f);Disc(vh,.66f,.47f,.055f);break;
                case Shape.Book:
                    Outline(vh,.08f,.15f,.4f,.67f,.05f);Outline(vh,.52f,.15f,.4f,.67f,.05f);break;
                case Shape.Bag:
                    Outline(vh,.17f,.1f,.66f,.62f,.06f);Outline(vh,.34f,.7f,.32f,.2f,.045f);break;
                case Shape.Plus:
                    Outline(vh,.08f,.08f,.84f,.84f,.065f);Line(vh,.5f,.25f,.5f,.75f,.065f);Line(vh,.25f,.5f,.75f,.5f,.065f);break;
                case Shape.Enter:
                    Line(vh,.5f,.85f,.85f,.85f,.07f);Line(vh,.85f,.85f,.85f,.15f,.07f);Line(vh,.85f,.15f,.5f,.15f,.07f);Line(vh,.06f,.5f,.61f,.5f,.08f);Line(vh,.43f,.7f,.64f,.5f,.08f);Line(vh,.43f,.3f,.64f,.5f,.08f);break;
                case Shape.Gear:
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,.5f+Mathf.Cos(a)*.24f,.5f+Mathf.Sin(a)*.24f,.5f+Mathf.Cos(a)*.43f,.5f+Mathf.Sin(a)*.43f,.12f);}
                    Ring(vh,.5f,.5f,.29f,.12f);break;
            }
        }
        Vector2 Point(Vector2 p){var r=rectTransform.rect;return new Vector2(r.x+p.x*r.width,r.y+p.y*r.height);}
        void Poly(VertexHelper vh,Vector2[] ps,float degrees=0)
        {
            int start=vh.currentVertCount;
            foreach(var p in ps){var q=p;if(degrees!=0)q=(Vector2)(Quaternion.Euler(0,0,degrees)*(Vector3)(p-Vector2.one*.5f))+Vector2.one*.5f;
                var tint=Color.Lerp(color*.65f,color,Mathf.Clamp01(q.y));tint.a=color.a;vh.AddVert(Point(q),tint,Vector2.zero);}
            // Ear clipping supports the concave crown and arrow silhouettes.
            var indices=new System.Collections.Generic.List<int>();for(int i=0;i<ps.Length;i++)indices.Add(i);
            float area=0;for(int i=0;i<ps.Length;i++){var a=ps[i];var b=ps[(i+1)%ps.Length];area+=a.x*b.y-b.x*a.y;}float sign=Mathf.Sign(area);
            int guard=0;while(indices.Count>2 && guard++<100){bool found=false;for(int j=0;j<indices.Count;j++){
                int a=indices[(j+indices.Count-1)%indices.Count],b=indices[j],c=indices[(j+1)%indices.Count];if(Cross(ps[b]-ps[a],ps[c]-ps[b])*sign<=0)continue;
                bool inside=false;foreach(int k in indices){if(k==a||k==b||k==c)continue; if(Cross(ps[b]-ps[a],ps[k]-ps[a])*sign>=0&&Cross(ps[c]-ps[b],ps[k]-ps[b])*sign>=0&&Cross(ps[a]-ps[c],ps[k]-ps[c])*sign>=0){inside=true;break;}}
                if(inside)continue;vh.AddTriangle(start+a,start+b,start+c);indices.RemoveAt(j);found=true;break;}if(!found)break;}
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        void Box(VertexHelper v,float x,float y,float w,float h)=>Poly(v,new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)});
        void Outline(VertexHelper v,float x,float y,float w,float h,float t){Box(v,x,y,w,t);Box(v,x,y+h-t,w,t);Box(v,x,y,t,h);Box(v,x+w-t,y,t,h);}
        void Line(VertexHelper v,float x,float y,float xx,float yy,float t){var a=new Vector2(x,y);var b=new Vector2(xx,yy);var n=new Vector2(-(b-a).y,(b-a).x).normalized*t*.5f;Poly(v,new[]{a+n,b+n,b-n,a-n});}
        void Disc(VertexHelper v,float x,float y,float radius){Vector2[] p=new Vector2[32];for(int i=0;i<p.Length;i++){float a=i*2*Mathf.PI/p.Length;p[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius);}Poly(v,p);}
        void Ring(VertexHelper v,float x,float y,float r,float thickness){for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Line(v,x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r,thickness);}}
    }
}


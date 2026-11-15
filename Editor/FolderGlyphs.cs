using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blumgames.FolderIcon
{
    // Small original glyphs drawn from geometry. No fonts or third-party icon assets.
    internal static class FolderGlyphs
    {
        static readonly float[] Points0 = new[]{.5f,.93f,.91f,.72f,.91f,.27f,.5f,.06f,.09f,.27f,.09f,.72f,.5f,.93f};
        static readonly float[] Points1 = new[]{.09f,.72f,.5f,.5f,.91f,.72f};
        static readonly float[] Points2 = new[]{.12f,.25f,.37f,.55f,.57f,.35f,.7f,.47f,.88f,.25f};
        static readonly float[] Points3 = new[]{.1f,.12f,.43f,.88f,.76f,.12f};
        static readonly float[] Points4 = new[]{.3f,.55f,.5f,.34f,.7f,.55f};
        static readonly float[] Points5 = new[]{.5f,.4f,.5f,.16f,.74f,.16f};
        static readonly float[] Points6 = new[]{.1f,.48f,.37f,.2f,.9f,.8f};
        static readonly Dictionary<FolderSymbol, Texture2D> Cache = new Dictionary<FolderSymbol, Texture2D>();
        public static Texture2D Get(FolderSymbol symbol)
        {
            if (symbol == FolderSymbol.None || symbol == FolderSymbol.Custom) return null;
            if (Cache.TryGetValue(symbol, out var cached) && cached) return cached;
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int coverage = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            if (Inside(symbol, (x + (sx + .5f) / 2) / size, (y + (sy + .5f) / 2) / size)) coverage++;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(coverage * 255 / 4));
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "BlumFolderIcon " + symbol, hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Cache[symbol] = texture;
            return texture;
        }

        public static void Dispose()
        {
            foreach (var texture in Cache.Values) if (texture) UnityEngine.Object.DestroyImmediate(texture);
            Cache.Clear();
        }

        static bool Inside(FolderSymbol symbol, float x, float y)
        {
            switch (symbol)
            {
                case FolderSymbol.Code:
                    return Line(x,y,.25f,.23f,.04f,.5f,.12f) || Line(x,y,.04f,.5f,.25f,.77f,.12f)
                        || Line(x,y,.75f,.23f,.96f,.5f,.12f) || Line(x,y,.96f,.5f,.75f,.77f,.12f)
                        || Line(x,y,.43f,.15f,.57f,.85f,.12f);
                case FolderSymbol.Audio:
                    return Box(x,y,.04f,.39f,.14f,.61f) || Box(x,y,.24f,.24f,.36f,.76f)
                        || Box(x,y,.44f,.08f,.56f,.92f) || Box(x,y,.64f,.24f,.76f,.76f) || Box(x,y,.86f,.39f,.96f,.61f);
                case FolderSymbol.Cube:
                case FolderSymbol.Model:
                    return Path(x,y,.09f,Points0)
                        || Path(x,y,.09f,Points1) || Line(x,y,.5f,.5f,.5f,.06f,.09f);
                case FolderSymbol.Scene:
                    return Circle(x,y,.76f,.74f,.13f) || Triangle(x,y,.03f,.13f,.38f,.72f,.7f,.13f)
                        || Triangle(x,y,.43f,.13f,.72f,.5f,.97f,.13f);
                case FolderSymbol.Image:
                    return Outline(x,y,.05f,.1f,.95f,.9f,.08f) || Circle(x,y,.72f,.69f,.1f)
                        || Path(x,y,.08f,Points2);
                case FolderSymbol.Material:
                    return Circle(x,y,.5f,.5f,.42f) && (!Circle(x,y,.5f,.5f,.31f) || x < .43f);
                case FolderSymbol.Animation:
                case FolderSymbol.Video:
                    return Outline(x,y,.07f,.12f,.93f,.68f,.09f) || Line(x,y,.07f,.8f,.88f,.94f,.14f)
                        || Triangle(x,y,.4f,.25f,.4f,.55f,.68f,.4f);
                case FolderSymbol.Tools:
                    return Line(x,y,.18f,.13f,.73f,.68f,.15f)
                        || (Circle(x,y,.72f,.72f,.25f) && !(x > .69f && y > .69f)) || Circle(x,y,.18f,.13f,.13f);
                case FolderSymbol.UI:
                    return Outline(x,y,.06f,.12f,.94f,.88f,.09f) || Line(x,y,.06f,.65f,.94f,.65f,.08f)
                        || Line(x,y,.36f,.12f,.36f,.65f,.08f);
                case FolderSymbol.Font:
                    return Path(x,y,.11f,Points3) || Line(x,y,.24f,.4f,.63f,.4f,.1f)
                        || Line(x,y,.79f,.66f,.97f,.66f,.08f) || Line(x,y,.88f,.12f,.88f,.67f,.08f);
                case FolderSymbol.Shader:
                    return Triangle(x,y,.53f,.96f,.12f,.44f,.52f,.44f) || Triangle(x,y,.48f,.56f,.88f,.56f,.41f,.03f);
                case FolderSymbol.Resources:
                    return Outline(x,y,.1f,.1f,.9f,.58f,.1f) || Line(x,y,.5f,.91f,.5f,.37f,.1f)
                        || Path(x,y,.1f,Points4);
                case FolderSymbol.Plugins:
                    return Box(x,y,.27f,.39f,.73f,.72f) || Line(x,y,.36f,.72f,.36f,.93f,.1f)
                        || Line(x,y,.64f,.72f,.64f,.93f,.1f) || Path(x,y,.11f,Points5);
                case FolderSymbol.Settings:
                    float dx=x-.5f, dy=y-.5f, radius=Mathf.Sqrt(dx*dx+dy*dy);
                    float edge=.34f + (Mathf.Cos(Mathf.Atan2(dy,dx)*8) > 0 ? .1f : 0);
                    return radius < edge && radius > .15f;
                case FolderSymbol.Tests:
                    return Path(x,y,.13f,Points6);
                case FolderSymbol.Documentation:
                    return Outline(x,y,.17f,.06f,.83f,.94f,.09f) || Line(x,y,.32f,.7f,.68f,.7f,.07f)
                        || Line(x,y,.32f,.5f,.68f,.5f,.07f) || Line(x,y,.32f,.3f,.56f,.3f,.07f);
                case FolderSymbol.Star:
                    return Star(x,y,.5f,.5f,.46f,.21f,5);
                case FolderSymbol.Heart:
                    return Circle(x,y,.3f,.65f,.23f) || Circle(x,y,.7f,.65f,.23f)
                        || Triangle(x,y,.08f,.59f,.92f,.59f,.5f,.05f);
                case FolderSymbol.Gamepad:
                    return Outline(x,y,.05f,.2f,.95f,.76f,.1f) || Line(x,y,.2f,.48f,.43f,.48f,.08f)
                        || Line(x,y,.315f,.36f,.315f,.6f,.08f) || Circle(x,y,.69f,.42f,.065f) || Circle(x,y,.81f,.57f,.065f);
                case FolderSymbol.Particles:
                    return Star(x,y,.39f,.53f,.34f,.08f,4) || Star(x,y,.8f,.8f,.18f,.05f,4) || Star(x,y,.8f,.2f,.12f,.03f,4);
                default: return false;
            }
        }

        static bool Box(float x,float y,float x0,float y0,float x1,float y1) => x>=x0 && x<=x1 && y>=y0 && y<=y1;
        static bool Outline(float x,float y,float x0,float y0,float x1,float y1,float w) => Box(x,y,x0,y0,x1,y1) && !Box(x,y,x0+w,y0+w,x1-w,y1-w);
        static bool Circle(float x,float y,float cx,float cy,float r) => (x-cx)*(x-cx)+(y-cy)*(y-cy) <= r*r;
        static bool Line(float x,float y,float ax,float ay,float bx,float by,float width)
        {
            float vx=bx-ax, vy=by-ay;
            float t=Mathf.Clamp01(((x-ax)*vx+(y-ay)*vy)/(vx*vx+vy*vy));
            return Circle(x,y,ax+t*vx,ay+t*vy,width*.5f);
        }
        static bool Path(float x,float y,float width,float[] points)
        {
            for(int i=2;i<points.Length;i+=2) if(Line(x,y,points[i-2],points[i-1],points[i],points[i+1],width)) return true;
            return false;
        }
        static bool Triangle(float x,float y,float ax,float ay,float bx,float by,float cx,float cy)
        {
            float a=(x-bx)*(ay-by)-(ax-bx)*(y-by), b=(x-cx)*(by-cy)-(bx-cx)*(y-cy), c=(x-ax)*(cy-ay)-(cx-ax)*(y-ay);
            return !((a<0 || b<0 || c<0) && (a>0 || b>0 || c>0));
        }
        static bool Star(float x,float y,float cx,float cy,float outer,float inner,int tips)
        {
            for(int i=0;i<tips*2;i++)
            {
                float a=(i*Mathf.PI/tips)+Mathf.PI*.5f, b=((i+1)*Mathf.PI/tips)+Mathf.PI*.5f;
                float ra=i%2==0?outer:inner, rb=i%2==0?inner:outer;
                if(Triangle(x,y,cx,cy,cx+Mathf.Cos(a)*ra,cy+Mathf.Sin(a)*ra,cx+Mathf.Cos(b)*rb,cy+Mathf.Sin(b)*rb)) return true;
            }
            return false;
        }
    }
}

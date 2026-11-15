using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Blumgames.FolderIcon
{
    [InitializeOnLoad]
    public static class FolderIconRenderer
    {
        sealed class Entry { public bool isFolder; public FolderStyle style; public Texture2D source; public Texture2D texture; }
        static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<int, Color32[]> Pixels = new Dictionary<int, Color32[]>();
        static bool dark;
        public static event Action Refreshed;

        static FolderIconRenderer()
        {
            EditorApplication.projectWindowItemOnGUI += Draw;
            EditorApplication.projectChanged += Refresh;
            FolderIconSettings.Changed += Refresh;
            Undo.undoRedoPerformed += UndoChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
            EditorApplication.delayCall += () => FolderIconSettings.instance.InitializeUndoState();
        }

        static void UndoChanged() => FolderIconSettings.instance.AfterUndoRedo();

        public static void Refresh()
        {
            Entries.Clear();
            Pixels.Clear();
            foreach (var texture in Textures.Values) if (texture) UnityEngine.Object.DestroyImmediate(texture);
            Textures.Clear();
            FolderIconSettings.instance.InvalidateLookup();
            dark = EditorGUIUtility.isProSkin;
            Refreshed?.Invoke();
            EditorApplication.RepaintProjectWindow();
        }

        static void Dispose()
        {
            foreach (var texture in Textures.Values) if (texture) UnityEngine.Object.DestroyImmediate(texture);
            Textures.Clear(); Pixels.Clear(); Entries.Clear(); FolderGlyphs.Dispose();
        }

        static void Draw(string guid, Rect rect)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(guid) || !FolderIconSettings.instance.Enabled) return;
            if (dark != EditorGUIUtility.isProSkin) Refresh();
            if (!Entries.TryGetValue(guid, out var entry))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                entry = new Entry { isFolder = AssetDatabase.IsValidFolder(path) };
                if (entry.isFolder)
                {
                    entry.style = FolderIconSettings.instance.Resolve(guid, Path.GetFileName(path));
                    entry.source = AssetDatabase.GetCachedIcon(path) as Texture2D;
                }
                Entries[guid] = entry;
            }
            if (!entry.isFolder || entry.style == null || (!entry.style.colorize && entry.style.symbol == FolderSymbol.None)) return;
            var iconRect = IconRect(rect);
            if (!entry.texture) entry.texture = Preview(entry.style, entry.source, true);
            GUI.DrawTexture(iconRect, entry.texture, ScaleMode.ScaleToFit, true);
        }

        public static Rect IconRect(Rect rect)
        {
            if (rect.height > 20f)
                return new Rect(rect.x + (rect.width - Mathf.Min(rect.width, rect.height - 14f)) * .5f, rect.y,
                    Mathf.Min(rect.width, rect.height - 14f), Mathf.Min(rect.width, rect.height - 14f));
            return new Rect(rect.x, rect.y, 16f, 16f);
        }

        public static Texture2D Preview(FolderStyle style, Texture2D source = null, bool matchNativeSize = false)
        {
            if (!source) source = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            if (!source) return Texture2D.grayTexture;
            style = style ?? FolderStyle.Native();
            string key = source.GetInstanceID() + ":" + style.colorize + ":" + ColorUtility.ToHtmlStringRGBA(style.color)
                + ":" + (int)style.symbol + ":" + style.customIconGuid + ":" + matchNativeSize;
            if (Textures.TryGetValue(key, out var cached) && cached) return cached;
            // Bound generated texture memory without creating a continuous repaint loop.
            if (Textures.Count > 256) { foreach(var t in Textures.Values) if(t) UnityEngine.Object.DestroyImmediate(t); Textures.Clear(); }
            var artwork = style.colorize ? FolderGlyphs.Load("FolderLight") : source;
            var shade = style.colorize ? FolderGlyphs.Load("FolderDark") : null;
            if (!artwork || (style.colorize && !shade)) return source;
            var input = ReadPixels(artwork);
            var shaded = shade ? ReadPixels(shade) : null;
            int w = artwork.width, h = artwork.height;
            var output = new Color32[input.Length];
            int x0=w, y0=h, x1=0, y1=0;
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                int i=y*w+x;
                Color original=input[i];
                if(original.a>.2f) { x0=Mathf.Min(x0,x); y0=Mathf.Min(y0,y); x1=Mathf.Max(x1,x); y1=Mathf.Max(y1,y); }
                if(style.colorize)
                {
                    Color low=shaded[i];
                    Color tinted=new Color(low.r+(original.r-low.r)*style.color.r,
                        low.g+(original.g-low.g)*style.color.g,low.b+(original.b-low.b)*style.color.b);
                    tinted.a=original.a;
                    output[i]=tinted;
                }
                else output[i]=input[i];
            }
            Texture2D glyph = style.symbol == FolderSymbol.Custom
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(style.customIconGuid ?? ""))
                : FolderGlyphs.Get(style.symbol);
            if(glyph && x1>x0 && y1>y0)
            {
                var gp=ReadPixels(glyph);
                bool approvedPlacement=style.colorize && style.symbol!=FolderSymbol.Custom;
                float width=approvedPlacement ? w*.5625f : (x1-x0+1)*.55f;
                float height=approvedPlacement ? h*.5625f : (y1-y0+1)*.53f;
                float fit=Mathf.Min(width/glyph.width,height/glyph.height);
                width=glyph.width*fit; height=glyph.height*fit;
                float left=approvedPlacement ? w*.21875f : (x0+x1+1-width)*.5f;
                float bottom=approvedPlacement ? h*.125f : y0+(y1-y0+1)*.42f-height*.5f;
                for(int y=Mathf.Max(0,Mathf.FloorToInt(bottom));y<Mathf.Min(h,Mathf.CeilToInt(bottom+height));y++)
                    for(int x=Mathf.Max(0,Mathf.FloorToInt(left));x<Mathf.Min(w,Mathf.CeilToInt(left+width));x++)
                    {
                        float u=(x+.5f-left)/width, v=(y+.5f-bottom)/height;
                        Color fg=Sample(gp,glyph.width,glyph.height,u,v);
                        Color bg=output[y*w+x];
                        output[y*w+x]=Color.Lerp(bg,new Color(fg.r,fg.g,fg.b,bg.a),fg.a*bg.a);
                    }
            }
            if (matchNativeSize && style.colorize)
            {
                output = FitNative(output, w, h, source, style.color);
                w = source.width; h = source.height;
            }
            var texture = new Texture2D(w,h,TextureFormat.RGBA32,false)
            { name="BlumFolderIcon preview",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear };
            texture.SetPixels32(output); texture.Apply(false,false); Textures[key]=texture;
            return texture;
        }

        // Native textures have their own padding. Match their visible bounds,
        // rather than treating the custom PNG's canvas as the visible folder size.
        static Color32[] FitNative(Color32[] artwork, int width, int height, Texture2D native, Color tint)
        {
            var mask = ReadPixels(native);
            var from = AlphaBounds(artwork, width, height);
            var to = AlphaBounds(mask, native.width, native.height);
            var result = new Color32[mask.Length];
            if (from.width == 0 || to.width == 0) return result;
            for (int y = 0; y < native.height; y++)
                for (int x = 0; x < native.width; x++)
                {
                    int i = y * native.width + x;
                    bool inside = to.Contains(new Vector2Int(x, y));
                    if (!inside && mask[i].a == 0) continue;
                    float u = (from.x + (x + .5f - to.x) / to.width * from.width) / width;
                    float v = (from.y + (y + .5f - to.y) / to.height * from.height) / height;
                    Color art = inside ? Sample(artwork, width, height, u, v) : Color.clear;
                    // Fill small differences between the two rounded silhouettes, so Unity's
                    // gray folder cannot peek through around the custom artwork.
                    Color color = Color.Lerp(tint, art, art.a);
                    color.a = Mathf.Max(art.a, mask[i].a / 255f);
                    if (color.a > 0f) result[i] = color;
                }
            return result;
        }

        static RectInt AlphaBounds(Color32[] pixels, int width, int height)
        {
            int left = width, bottom = height, right = -1, top = -1;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                if (pixels[y * width + x].a > 51)
                {
                    left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y);
                    right = Mathf.Max(right, x); top = Mathf.Max(top, y);
                }
            return right < left ? new RectInt() : new RectInt(left, bottom, right - left + 1, top - bottom + 1);
        }

        static Color Sample(Color32[] data,int w,int h,float u,float v)
        {
            float x=Mathf.Clamp(u*w-.5f,0,w-1), y=Mathf.Clamp(v*h-.5f,0,h-1);
            int x0=(int)x,y0=(int)y,x1=Mathf.Min(w-1,x0+1),y1=Mathf.Min(h-1,y0+1);
            return Color.Lerp(Color.Lerp(data[y0*w+x0],data[y0*w+x1],x-x0),Color.Lerp(data[y1*w+x0],data[y1*w+x1],x-x0),y-y0);
        }

        static Color32[] ReadPixels(Texture2D source)
        {
            if(Pixels.TryGetValue(source.GetInstanceID(),out var cached)) return cached;
            Color32[] result;
            if(source.isReadable) result=source.GetPixels32();
            else
            {
                var old=RenderTexture.active;
                var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                Texture2D copy=null;
                try
                {
                    Graphics.Blit(source,rt); RenderTexture.active=rt;
                    copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
                    copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0); copy.Apply();
                    result=copy.GetPixels32();
                }
                finally { RenderTexture.active=old; RenderTexture.ReleaseTemporary(rt); if(copy) UnityEngine.Object.DestroyImmediate(copy); }
            }
            Pixels[source.GetInstanceID()]=result;
            return result;
        }
    }
}

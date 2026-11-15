using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Blumgames.FolderIcon
{
    // Approved original PNG artwork; the folder GUID works for Assets and UPM installs.
    internal static class FolderGlyphs
    {
        const string ArtworkGuid = "b58fbb98c9ae5f9694ebc433dfb6e007";
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(FolderSymbol symbol) =>
            symbol == FolderSymbol.None || symbol == FolderSymbol.Custom ? null : Load(symbol.ToString());

        internal static Texture2D Load(string name)
        {
            if (Cache.TryGetValue(name, out var texture) && texture) return texture;
            string folder = AssetDatabase.GUIDToAssetPath(ArtworkGuid);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + ".png");
            if (texture) Cache[name] = texture;
            return texture;
        }

        // Imported textures belong to AssetDatabase and must never be destroyed here.
        public static void Dispose() => Cache.Clear();
    }
}

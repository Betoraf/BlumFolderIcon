using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blumgames.FolderIcon
{
    public enum FolderSymbol
    {
        None, Code, Scene, Cube, Material, Image, Audio, Animation, Tools,
        UI, Font, Shader, Model, Resources, Plugins, Settings, Tests,
        Documentation, Star, Heart, Gamepad, Particles, Video, Custom
    }

    [Serializable]
    public sealed class FolderStyle
    {
        public bool colorize = true;
        public Color color = FolderPresets.Colors[0];
        public FolderSymbol symbol = FolderSymbol.Code;
        public string customIconGuid = "";

        public FolderStyle Copy() => new FolderStyle
        { colorize = colorize, color = color, symbol = symbol, customIconGuid = customIconGuid };

        public static FolderStyle Native() => new FolderStyle { colorize = false, symbol = FolderSymbol.None };
    }

    [Serializable]
    public sealed class FolderRule
    {
        public bool enabled = true;
        public string names = "";
        public FolderStyle style = new FolderStyle();
    }

    [Serializable]
    public sealed class FolderOverride
    {
        public string guid;
        public FolderStyle style;
    }

    public static class FolderPresets
    {
        public static readonly string[] ColorNames =
        { "Blue", "Coral", "Sage", "Gold", "Lavender", "Teal", "Rose", "Slate" };
        public static readonly Color[] Colors =
        {
            Hex("4699EE"), Hex("F27C68"), Hex("79BE71"), Hex("F2B943"),
            Hex("AD86EB"), Hex("3DBBAE"), Hex("E67DA8"), Hex("86A3C4")
        };

        // Upgrade exact v1.0 preset colors only. User-picked colors stay intact.
        static readonly string[] LegacyColors =
        { "598DCA", "CE786B", "8BAD7D", "DCAD55", "A18EC8", "5CA9A3", "BB7F9B", "89939F" };

        internal static bool UpgradeColor(FolderStyle style)
        {
            if (style == null) return false;
            string rgb = ColorUtility.ToHtmlStringRGB(style.color);
            for (int i = 0; i < LegacyColors.Length; i++)
                if (rgb == LegacyColors[i])
                {
                    float alpha = style.color.a;
                    style.color = Colors[i]; style.color.a = alpha;
                    return true;
                }
            return false;
        }

        public static List<FolderRule> CreateRules() => new List<FolderRule>
        {
            Rule("Scripts, Script, Code", FolderSymbol.Code, 0),
            Rule("Scenes, Scene, Levels, Level", FolderSymbol.Scene, 4),
            Rule("Prefabs, Prefab", FolderSymbol.Cube, 2),
            Rule("Materials, Material", FolderSymbol.Material, 6),
            Rule("Textures, Texture, Sprites, Sprite, Images, Image", FolderSymbol.Image, 3),
            Rule("Audio, Sounds, Sound, Music, SFX", FolderSymbol.Audio, 1),
            Rule("Animations, Animation, Animators, Animator", FolderSymbol.Animation, 4),
            Rule("Editor, Editors, Tools", FolderSymbol.Tools, 7),
            Rule("UI, GUI, Interface", FolderSymbol.UI, 5),
            Rule("Fonts, Font", FolderSymbol.Font, 3),
            Rule("Shaders, Shader, ShaderGraphs", FolderSymbol.Shader, 6),
            Rule("Models, Model, Meshes, Mesh", FolderSymbol.Model, 2),
            Rule("Resources, Resource, Addressables", FolderSymbol.Resources, 3),
            Rule("Plugins, Plugin, ThirdParty", FolderSymbol.Plugins, 7),
            Rule("Settings, Config, Configuration", FolderSymbol.Settings, 7),
            Rule("Tests, Test", FolderSymbol.Tests, 2),
            Rule("Documentation, Documents, Docs", FolderSymbol.Documentation, 0),
            Rule("VFX, FX, Effects, Particles", FolderSymbol.Particles, 1),
            Rule("Video, Videos, Movies", FolderSymbol.Video, 4)
        };

        static FolderRule Rule(string names, FolderSymbol symbol, int color) => new FolderRule
        { names = names, style = new FolderStyle { color = Colors[color], symbol = symbol } };

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var result);
            return result;
        }
    }
}

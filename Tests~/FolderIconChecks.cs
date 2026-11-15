using System;
using System.IO;
using System.Linq;
using Blumgames.FolderIcon;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public static class FolderIconChecks
{
    static int checks;
    static void Check(bool value,string message) { if(!value) throw new Exception(message); checks++; }

    public static void Run()
    {
        var settings=FolderIconSettings.instance;
        settings.SetOptions(true,true);
        settings.RestoreRules();
        Check(settings.Rules.Count==21,"default rules");
        Check(settings.Resolve("", "sCrIpTs").symbol==FolderSymbol.Code,"case-insensitive aliases");
        Check(settings.Resolve("", "Script").symbol==FolderSymbol.Code,"singular alias");
        Check(settings.Resolve("", "Music").symbol==FolderSymbol.Audio,"audio aliases");
        Check(settings.Resolve("", "Unknown") == null,"unknown folder stays native");
        Check(settings.Resolve("", "MyScripts") == null,"no partial matching");
        if(AssetDatabase.IsValidFolder("Assets/Checks")) AssetDatabase.DeleteAsset("Assets/Checks");
        Directory.CreateDirectory("Assets/Checks/A/Scripts");
        Directory.CreateDirectory("Assets/Checks/B/Scripts");
        foreach(var name in new[]{"Scripts","Audio","Scenes","Prefabs","Materials","Textures","Sprites","ThirdParty","Native","Editor","UI","Shaders","Models","Resources","Fonts","Animations","Plugins","Settings","Tests","Docs","VFX","Video"})
            Directory.CreateDirectory("Assets/Demo/"+name);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string first=AssetDatabase.AssetPathToGUID("Assets/Checks/A/Scripts"),second=AssetDatabase.AssetPathToGUID("Assets/Checks/B/Scripts");
        var style=new FolderStyle { colorize=true,color=Color.red,symbol=FolderSymbol.Heart };
        settings.SetOverrides(new[]{first},style);
        Check(settings.Resolve(first,"Scripts").symbol==FolderSymbol.Heart,"manual priority");
        Check(settings.Resolve(second,"Scripts").symbol==FolderSymbol.Code,"same-name folder independent");
        Check(AssetDatabase.RenameAsset("Assets/Checks/A/Scripts","Renamed")=="","rename");
        Check(settings.Resolve(first,"Renamed").symbol==FolderSymbol.Heart,"rename preserves appearance");
        Check(AssetDatabase.MoveAsset("Assets/Checks/A/Renamed","Assets/Checks/B/Moved")=="","move");
        Check(AssetDatabase.AssetPathToGUID("Assets/Checks/B/Moved")==first,"move preserves GUID");
        Check(settings.Resolve(first,"Moved").symbol==FolderSymbol.Heart,"move preserves appearance");
        Undo.IncrementCurrentGroup();
        settings.SetOverrides(new[]{first,second},new FolderStyle { colorize=false,symbol=FolderSymbol.Audio });
        Check(settings.Resolve(first,"Moved").symbol==FolderSymbol.Audio && settings.Resolve(second,"Scripts").symbol==FolderSymbol.Audio,"bulk edit");
        Undo.PerformUndo();
        Check(settings.Resolve(first,"Moved").symbol==FolderSymbol.Heart && settings.Resolve(second,"Scripts").symbol==FolderSymbol.Code,"undo bulk");
        Undo.PerformRedo();
        Check(settings.Resolve(first,"Moved").symbol==FolderSymbol.Audio && settings.Resolve(second,"Scripts").symbol==FolderSymbol.Audio,"redo bulk");
        settings.SetOverrides(new[]{second},FolderStyle.Native());
        Check(!settings.Resolve(second,"Scripts").colorize && settings.Resolve(second,"Scripts").symbol==FolderSymbol.None,"explicit native beats rule");
        settings.ResetOverrides(new[]{second});
        Check(settings.Resolve(second,"Scripts").symbol==FolderSymbol.Code,"reset to auto");
        settings.SetOptions(true,false);
        Check(settings.Resolve(second,"Scripts")==null && settings.Resolve(first,"Moved").symbol==FolderSymbol.Audio,"disable auto preserves manual");
        settings.SetOptions(false,true);
        Check(settings.Resolve(first,"Moved")==null,"disable all");
        settings.SetOptions(true,true);
        settings.AddRule();
        settings.SetRule(settings.Rules.Count-1,new FolderRule { names="Scripts, My Special",style=style.Copy() });
        Check(settings.Resolve("","Scripts").symbol==FolderSymbol.Code,"first rule wins duplicates");
        Check(settings.Resolve("","my special").symbol==FolderSymbol.Heart,"new rule");
        settings.RemoveRule(settings.Rules.Count-1);
        Check(settings.Resolve("","My Special")==null,"remove rule");
        settings.SetRule(0,new FolderRule { enabled=false,names="Scripts",style=style.Copy() });
        Check(settings.Resolve("","Scripts")==null,"disabled rule");
        settings.RestoreRules();

        var native=FolderIconRenderer.Preview(FolderStyle.Native());
        Check(native && native.width>0,"native texture");
        Directory.CreateDirectory("Previews");
        foreach(FolderSymbol symbol in Enum.GetValues(typeof(FolderSymbol)))
        {
            if(symbol==FolderSymbol.Custom) continue;
            var icon=FolderIconRenderer.Preview(new FolderStyle { color=FolderPresets.Colors[(int)symbol%8],symbol=symbol });
            Check(icon && icon.GetPixels32().Any(p=>p.a>0),"render "+symbol);
            Check(icon.GetPixels32().Any(p=>p.a==0),"transparency "+symbol);
            File.WriteAllBytes("Previews/"+symbol+".png",icon.EncodeToPNG());
        }
        var custom=new Texture2D(32,32);
        custom.SetPixels(Enumerable.Repeat(Color.magenta,32*32).ToArray());custom.Apply();
        File.WriteAllBytes("Assets/Checks/custom.png",custom.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(custom);
        AssetDatabase.ImportAsset("Assets/Checks/custom.png",ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Checks/custom.png");
        importer.isReadable=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var customStyle=new FolderStyle { color=Color.blue,symbol=FolderSymbol.Custom,customIconGuid=AssetDatabase.AssetPathToGUID("Assets/Checks/custom.png") };
        var customPreview=FolderIconRenderer.Preview(customStyle);
        Check(customPreview.GetPixels32().Any(p=>p.r>220 && p.b>220 && p.g<30),"non-readable custom PNG keeps colors");
        Check(!((TextureImporter)AssetImporter.GetAtPath("Assets/Checks/custom.png")).isReadable,"custom importer unchanged");
        Check(FolderIconRenderer.Preview(customStyle)==customPreview,"preview cache reused");
        settings.SetOverrides(new[]{first},customStyle);
        Check(File.Exists("ProjectSettings/BlumFolderIcon.asset"),"settings saved outside package");
        File.WriteAllText("expected-guid.txt",first);
        Check(!CompilationPipeline.GetAssemblies(AssembliesType.Player).Any(a=>a.name.StartsWith("Blumgames.FolderIcon")),"no runtime assembly");
        Check(FolderIconRenderer.IconRect(new Rect(20,10,140,16)).width==16,"list icon width");
        var grid=FolderIconRenderer.IconRect(new Rect(10,20,64,78));
        Check(grid.width==64 && grid.x==10 && grid.y==20,"grid icon geometry");
        Artwork();
        NativeSizing();
        CategoryColors();
        File.WriteAllText("CHECKS-PASS.txt",checks+" checks passed on Unity "+Application.unityVersion+"\n");
        Debug.Log("BLUMFOLDERICON CHECKS PASS: "+checks);
    }

    public static void Persistence()
    {
        string guid=File.ReadAllText("expected-guid.txt");
        var style=FolderIconSettings.instance.Resolve(guid,"Moved");
        Check(style!=null && style.symbol==FolderSymbol.Custom,"customization survives editor restart");
        Check(!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(style.customIconGuid)),"custom PNG GUID persisted");
        Check(FolderIconSettings.instance.Resolve("","Scripts").symbol==FolderSymbol.Code,"rules survived restart");
        File.WriteAllText("PERSISTENCE-PASS.txt","Settings and custom PNG persisted across Editor restart on "+Application.unityVersion);
        Debug.Log("BLUMFOLDERICON PERSISTENCE PASS");
    }

    public static void Export()
    {
        AssetDatabase.ExportPackage("Assets/Blumgames/FolderIcon","../release-1.1.1/BlumFolderIcon-1.1.1.unitypackage",ExportPackageOptions.Recurse);
        Debug.Log("BLUMFOLDERICON EXPORTED");
    }

    static void Artwork()
    {
        var settings=FolderIconSettings.instance;
        string original=EditorJsonUtility.ToJson(settings);
        ColorUtility.TryParseHtmlString("#598DCA",out var oldBlue);
        var customColor=new Color(.137f,.421f,.739f);
        settings.SetRule(0,new FolderRule { names="Scripts",style=new FolderStyle { color=oldBlue,symbol=FolderSymbol.Code } });
        settings.SetRule(1,new FolderRule { names="Scenes",style=new FolderStyle { color=customColor,symbol=FolderSymbol.Scene } });
        settings.SetOverrides(new[]{"legacy-palette-check"},new FolderStyle { color=oldBlue,symbol=FolderSymbol.Star });
        var serialized=new SerializedObject(settings);
        serialized.FindProperty("paletteVersion").intValue=0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        FolderIconRenderer.Refresh();
        Check(ColorUtility.ToHtmlStringRGB(settings.Resolve("","Scripts").color)=="4699EE","legacy rule palette upgraded");
        Check(ColorUtility.ToHtmlStringRGB(settings.Resolve("legacy-palette-check","X").color)=="4699EE","legacy manual palette upgraded");
        Check(settings.Resolve("","Scenes").color==customColor,"custom-picked color preserved");
        EditorJsonUtility.FromJsonOverwrite(original,settings);
        FolderIconRenderer.Refresh();
        settings.SetOptions(true,true);

        for(int i=0;i<FolderPresets.Colors.Length;i++)
        {
            var icon=FolderIconRenderer.Preview(new FolderStyle { color=FolderPresets.Colors[i],symbol=FolderSymbol.None });
            Check(icon.width==256 && icon.height==256,"approved artwork size "+i);
            Color top=icon.GetPixel(184,195),front=icon.GetPixel(184,145);
            Check(top.grayscale<front.grayscale*.9f,"rear layer darker than front "+i);
        }
    }

    static void NativeSizing()
    {
        foreach (string name in new[]{"Folder Icon","FolderEmpty Icon","d_Folder Icon","d_FolderEmpty Icon"})
        {
            var source=EditorGUIUtility.IconContent(name).image as Texture2D;
            Check(source!=null,"built-in folder "+name);
            var native=FolderIconRenderer.Preview(FolderStyle.Native(),source);
            var fitted=FolderIconRenderer.Preview(new FolderStyle(),source,true);
            Check(fitted.width==native.width && fitted.height==native.height,"native canvas "+name);
            var a=fitted.GetPixels32(); var b=native.GetPixels32();
            Check(Bounds(a,fitted.width).Equals(Bounds(b,native.width)),"native visible bounds "+name);
            Check(a.Zip(b,(p,q)=>p.a>=q.a).All(v=>v),"underlying native folder fully covered "+name);
            Check(FolderIconRenderer.Preview(new FolderStyle(),source,true)==fitted,"fitted texture cache "+name);
            File.WriteAllBytes("Previews/Native-"+name.Replace(' ','-')+".png",native.EncodeToPNG());
            File.WriteAllBytes("Previews/Fitted-"+name.Replace(' ','-')+".png",fitted.EncodeToPNG());
        }
    }

    static RectInt Bounds(Color32[] pixels,int width)
    {
        var solid=Enumerable.Range(0,pixels.Length).Where(i=>pixels[i].a>51).ToArray();
        int left=solid.Min(i=>i%width),bottom=solid.Min(i=>i/width);
        return new RectInt(left,bottom,solid.Max(i=>i%width)-left+1,solid.Max(i=>i/width)-bottom+1);
    }

    static void CategoryColors()
    {
        var settings=FolderIconSettings.instance;
        string saved=EditorJsonUtility.ToJson(settings);
        var rules=FolderPresets.CreateRules();
        Check(rules.Select(r=>ColorUtility.ToHtmlStringRGB(r.style.color)).Distinct().Count()==21,"21 distinct automatic category colors");
        EditorJsonUtility.FromJsonOverwrite(File.ReadAllText("Settings-1.1.json"),settings);
        FolderIconRenderer.Refresh();
        Check(settings.Rules.Count==21,"upgrade splits sprites and thirdparty");
        Check(settings.Rules.Select(r=>ColorUtility.ToHtmlStringRGB(r.style.color)).Distinct().Count()==21,"all unchanged defaults migrate");
        Check(settings.Resolve("","Sprites").color!=settings.Resolve("","Textures").color,"sprites distinct from textures");
        Check(settings.Resolve("","Thirdparty").color!=settings.Resolve("","Plugins").color,"thirdparty distinct from plugins");
        string once=EditorJsonUtility.ToJson(settings);
        FolderIconRenderer.Refresh();
        Check(once==EditorJsonUtility.ToJson(settings),"migration idempotent");

        EditorJsonUtility.FromJsonOverwrite(File.ReadAllText("Settings-1.1.json"),settings);
        // Edit serialized fields before migration to represent user-customized 1.1 settings.
        var so=new SerializedObject(settings);
        so.FindProperty("rules").GetArrayElementAtIndex(4).FindPropertyRelative("style.color").colorValue=Color.magenta;
        so.FindProperty("rules").GetArrayElementAtIndex(13).FindPropertyRelative("enabled").boolValue=false;
        var overrides=so.FindProperty("overrides");overrides.arraySize=1;
        var item=overrides.GetArrayElementAtIndex(0);
        item.FindPropertyRelative("guid").stringValue="manual-check";
        item.FindPropertyRelative("style.colorize").boolValue=true;
        item.FindPropertyRelative("style.color").colorValue=FolderPresets.Colors[7];
        item.FindPropertyRelative("style.symbol").enumValueIndex=(int)FolderSymbol.Plugins;
        so.ApplyModifiedPropertiesWithoutUndo();
        FolderIconRenderer.Refresh();
        Check(settings.Resolve("","Sprites").color==Color.magenta,"customized alias group preserved");
        Check(settings.Resolve("","Plugins")==null && settings.Resolve("","ThirdParty")==null,"split disabled rule remains disabled");
        Check(settings.Resolve("manual-check","Plugins").color==FolderPresets.Colors[7],"manual preset color preserved");
        Check(settings.Resolve("manual-check","Plugins").symbol==FolderSymbol.Plugins,"manual symbol preserved");
        EditorJsonUtility.FromJsonOverwrite(saved,settings);
        FolderIconRenderer.Refresh();
        settings.SetOptions(true,true);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Blumgames.FolderIcon
{
    public sealed class FolderIconWindow : EditorWindow
    {
        [SerializeField] string[] targets = Array.Empty<string>();
        [SerializeField] FolderStyle draft = new FolderStyle();
        [SerializeField] bool showRules;
        VisualElement editor;
        VisualElement preview;
        Label targetLabel;
        Label status;
        ColorField colorField;
        Toggle colorToggle;
        ObjectField customIcon;
        readonly List<Button> glyphButtons = new List<Button>();

        [MenuItem("Tools/Blumgames/BlumFolderIcon")]
        public static void Open()
        {
            var window = GetWindow<FolderIconWindow>();
            window.titleContent = new GUIContent("BlumFolderIcon");
            window.minSize = new Vector2(440, 610);
            window.Show();
            window.ReadSelection();
        }

        [MenuItem("Assets/BlumFolderIcon/Customize", false, 2000)]
        static void Customize() => Open();
        [MenuItem("Assets/BlumFolderIcon/Customize", true)]
        static bool CanCustomize() => SelectedFolders().Length > 0;

        [MenuItem("Assets/BlumFolderIcon/Reset to Automatic", false, 2001)]
        static void ResetSelection() => FolderIconSettings.instance.ResetOverrides(SelectedFolders());
        [MenuItem("Assets/BlumFolderIcon/Reset to Automatic", true)]
        static bool CanReset() => CanCustomize();

        [MenuItem("Assets/BlumFolderIcon/Use Unity Default", false, 2002)]
        static void Native() => FolderIconSettings.instance.SetOverrides(SelectedFolders(), FolderStyle.Native());
        [MenuItem("Assets/BlumFolderIcon/Use Unity Default", true)]
        static bool CanNative() => CanCustomize();

        static string[] SelectedFolders() => Selection.assetGUIDs.Where(g => AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(g))).ToArray();

        void OnEnable()
        {
            Selection.selectionChanged += ReadSelection;
            FolderIconRenderer.Refreshed += RefreshPreview;
            Undo.undoRedoPerformed += ReadSelection;
        }
        void OnDisable()
        {
            Selection.selectionChanged -= ReadSelection;
            FolderIconRenderer.Refreshed -= RefreshPreview;
            Undo.undoRedoPerformed -= ReadSelection;
        }

        void ReadSelection()
        {
            targets = SelectedFolders();
            if(targets.Length > 0)
            {
                string path=AssetDatabase.GUIDToAssetPath(targets[0]);
                draft=(FolderIconSettings.instance.Resolve(targets[0],Path.GetFileName(path)) ?? FolderStyle.Native()).Copy();
            }
            if(rootVisualElement.childCount>0) CreateGUI();
        }

        public void CreateGUI()
        {
            var root=rootVisualElement;
            root.Clear();
            root.AddToClassList("blum-root");
            root.EnableInClassList("light", !EditorGUIUtility.isProSkin);
            string scriptPath=AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var sheet=AssetDatabase.LoadAssetAtPath<StyleSheet>(Path.GetDirectoryName(scriptPath)+"/BlumFolderIcon.uss");
            if(sheet && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);

            var heading=Row(root,"header");
            var title=new Label("BlumFolderIcon"); title.AddToClassList("title"); heading.Add(title);
            var brand=new Label("by blumgames"); brand.AddToClassList("muted"); heading.Add(brand);
            var settings=FolderIconSettings.instance;
            var options=Row(root,"options");
            var enable=new Toggle("Enabled") { value=settings.Enabled };
            var auto=new Toggle("Automatic icons") { value=settings.Automatic };
            enable.RegisterValueChangedCallback(e=>settings.SetOptions(e.newValue,settings.Automatic));
            auto.RegisterValueChangedCallback(e=>settings.SetOptions(settings.Enabled,e.newValue));
            options.Add(enable); options.Add(auto);
            var rulesButton=new Button(()=> { showRules=!showRules; CreateGUI(); }) { text=showRules?"Back to folders":"Automatic rules" };
            options.Add(rulesButton);
            if(showRules) { BuildRules(root); return; }

            var body=new ScrollView(); body.AddToClassList("body"); root.Add(body);
            targetLabel=new Label(targets.Length==0?"Select a folder in the Project window":targets.Length==1
                ?Path.GetFileName(AssetDatabase.GUIDToAssetPath(targets[0])):targets.Length+" selected folders");
            targetLabel.AddToClassList("selection-title"); body.Add(targetLabel);
            preview=Row(body,"preview");
            preview.RegisterCallback<AttachToPanelEvent>(e=>preview.schedule.Execute(RefreshPreview));
            editor=new VisualElement(); body.Add(editor);
            editor.SetEnabled(targets.Length>0);
            LabelText(editor,"Folder color","section-title");
            var colorRow=Row(editor,"field-row");
            colorToggle=new Toggle("Use color") { value=draft.colorize };
            colorToggle.RegisterValueChangedCallback(e=> { draft.colorize=e.newValue; RefreshPreview(); });
            colorRow.Add(colorToggle);
            colorField=new ColorField { value=draft.color, showAlpha=false, hdr=false };
            colorField.AddToClassList("color-field");
            colorField.RegisterValueChangedCallback(e=> { draft.color=e.newValue; draft.colorize=true; colorToggle.SetValueWithoutNotify(true); RefreshPreview(); });
            colorRow.Add(colorField);
            var palette=Row(editor,"palette");
            for(int i=0;i<FolderPresets.Colors.Length;i++)
            {
                int index=i;
                var swatch=new Button(()=> { draft.color=FolderPresets.Colors[index]; draft.colorize=true;
                    colorField.SetValueWithoutNotify(draft.color); colorToggle.SetValueWithoutNotify(true); RefreshPreview(); })
                    { tooltip=FolderPresets.ColorNames[i] };
                swatch.AddToClassList("swatch"); swatch.style.backgroundColor=FolderPresets.Colors[i]; palette.Add(swatch);
            }
            LabelText(editor,"Symbol","section-title");
            var grid=Row(editor,"glyph-grid"); glyphButtons.Clear();
            foreach(FolderSymbol symbol in Enum.GetValues(typeof(FolderSymbol)))
            {
                if(symbol==FolderSymbol.Custom) continue;
                var button=new Button(()=> { draft.symbol=symbol; RefreshPreview(); }) { tooltip=symbol.ToString() };
                button.AddToClassList("glyph-button"); button.userData=symbol;
                if(symbol==FolderSymbol.None) button.text="None";
                else { var img=new Image { image=FolderGlyphs.Get(symbol), scaleMode=ScaleMode.ScaleToFit }; img.AddToClassList("glyph"); button.Add(img); }
                grid.Add(button); glyphButtons.Add(button);
            }
            customIcon=new ObjectField("Custom PNG") { objectType=typeof(Texture2D), allowSceneObjects=false,
                value=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(draft.customIconGuid??"")) };
            customIcon.RegisterValueChangedCallback(e=>
            {
                draft.customIconGuid=e.newValue?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.newValue)):"";
                draft.symbol=e.newValue?FolderSymbol.Custom:FolderSymbol.None; RefreshPreview();
            });
            editor.Add(customIcon);
            LabelText(editor,"A transparent image works best. Its colors are preserved.","hint");
            status=LabelText(body,"", "hint");
            var footer=Row(root,"footer");
            var reset=new Button(()=> { settings.ResetOverrides(targets); ReadSelection(); }) { text="Reset to Automatic" };
            var native=new Button(()=> { settings.SetOverrides(targets,FolderStyle.Native()); ReadSelection(); }) { text="Unity Default" };
            var apply=new Button(()=> { settings.SetOverrides(targets,draft); RefreshPreview(); status.text="Saved. Renaming or moving these folders keeps their style."; }) { text=targets.Length>1?"Apply to "+targets.Length+" folders":"Apply" };
            apply.AddToClassList("primary");
            footer.Add(reset); footer.Add(native); footer.Add(apply); footer.SetEnabled(targets.Length>0);
            RefreshPreview();
        }

        void RefreshPreview()
        {
            if(showRules || preview==null || preview.panel==null) return;
            preview.Clear();
            var texture=FolderIconRenderer.Preview(draft);
            foreach(int size in new[]{80,32,16})
            {
                var slot=new VisualElement(); slot.AddToClassList("preview-slot");
                var image=new Image { image=texture, scaleMode=ScaleMode.ScaleToFit };
                image.style.width=size; image.style.height=size;
                slot.Add(image); slot.Add(new Label(size+" px")); preview.Add(slot);
            }
            foreach(var button in glyphButtons) button.EnableInClassList("chosen",(FolderSymbol)button.userData==draft.symbol);
            if(status!=null) status.text=targets.Length==0?"Right-click a folder > BlumFolderIcon > Customize."
                :"Color and symbol are independent. Apply changes to the selected folders.";
        }

        void BuildRules(VisualElement root)
        {
            LabelText(root,"Match folder names automatically", "selection-title");
            LabelText(root,"Separate aliases with commas. Matching ignores letter case. First matching rule wins; manual folder styles take priority.","hint");
            var scroll=new ScrollView(); scroll.AddToClassList("body"); root.Add(scroll);
            var settings=FolderIconSettings.instance;
            for(int i=0;i<settings.Rules.Count;i++)
            {
                int index=i; var current=settings.Rules[i];
                var card=new VisualElement(); card.AddToClassList("rule"); scroll.Add(card);
                var top=Row(card,"field-row");
                var enabled=new Toggle { value=current.enabled }; top.Add(enabled);
                var names=new TextField { value=current.names, isDelayed=true }; names.style.flexGrow=1; top.Add(names);
                var remove=new Button(()=> { settings.RemoveRule(index); CreateGUI(); }) { text="Remove" }; top.Add(remove);
                var fields=Row(card,"field-row");
                var tint=new Toggle("Color") { value=current.style.colorize }; fields.Add(tint);
                var color=new ColorField { value=current.style.color, showAlpha=false }; color.style.width=70; fields.Add(color);
                var symbol=new EnumField(current.style.symbol); symbol.style.flexGrow=1; fields.Add(symbol);
                var custom=new ObjectField("Custom PNG") { objectType=typeof(Texture2D),allowSceneObjects=false,
                    value=AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(current.style.customIconGuid??"")) };
                custom.style.display=current.style.symbol==FolderSymbol.Custom?DisplayStyle.Flex:DisplayStyle.None; card.Add(custom);
                Action save=()=>settings.SetRule(index,new FolderRule { enabled=enabled.value,names=names.value,style=new FolderStyle
                { colorize=tint.value,color=color.value,symbol=(FolderSymbol)symbol.value,
                    customIconGuid=custom.value?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(custom.value)):"" } });
                enabled.RegisterValueChangedCallback(e=>save()); names.RegisterValueChangedCallback(e=>save()); tint.RegisterValueChangedCallback(e=>save());
                color.RegisterValueChangedCallback(e=>save()); custom.RegisterValueChangedCallback(e=>save());
                symbol.RegisterValueChangedCallback(e=> { custom.style.display=(FolderSymbol)e.newValue==FolderSymbol.Custom?DisplayStyle.Flex:DisplayStyle.None; save(); });
            }
            var footer=Row(root,"footer");
            footer.Add(new Button(()=> { settings.AddRule(); CreateGUI(); }) { text="Add rule" });
            footer.Add(new Button(()=>
            {
                if(EditorUtility.DisplayDialog("Restore automatic rules?","Your custom name rules will be replaced by the defaults. Per-folder styles will be kept.","Restore","Cancel"))
                { settings.RestoreRules(); CreateGUI(); }
            }) { text="Restore default rules" });
            LabelText(root,"Shared settings: ProjectSettings/BlumFolderIcon.asset", "hint");
        }

        static VisualElement Row(VisualElement parent,string css)
        {
            var row=new VisualElement(); row.AddToClassList(css); parent.Add(row); return row;
        }
        static Label LabelText(VisualElement parent,string text,string css)
        {
            var label=new Label(text); label.AddToClassList(css); parent.Add(label); return label;
        }
    }
}

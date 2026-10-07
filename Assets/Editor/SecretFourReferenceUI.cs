#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;
using SecretFour.Localization;
using SecretFour.Presentation;
using Object=UnityEngine.Object;

public static class SecretFourReferenceUI
{
    const string Art="Assets/Art/ReferenceUI/";
    static Color gold=new Color(1,.81f,.43f),cream=new Color(1,.96f,.85f),muted=new Color(.72f,.76f,.8f),ink=new Color(.015f,.045f,.065f);
    static Sprite dark,primary,cell,outline,marble,xCoin,yCoin,circle,disc,crown,ring;
    static GameTextCatalog catalog;
    static Transform canvas;
    [MenuItem("Tools/Secret Four/Apply Reference UI")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play mode first.");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Prepare();
        foreach(string scene in new[]{"Mainmenu","Game"})
        {
            var s=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
            canvas=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).First().transform;
            if(canvas.Find("Reference UI applied")!=null)throw new InvalidOperationException("Reference UI is already applied. Edit its objects directly.");
            foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {text.color=cream;text.fontStyle=FontStyles.Normal;text.enableAutoSizing=true;text.fontSizeMin=24;text.fontSizeMax=Mathf.Max(36,text.fontSize);text.raycastTarget=false;}
            foreach(var b in canvas.GetComponentsInChildren<Button>(true)) StyleButton(b.transform,false);
            foreach(var input in canvas.GetComponentsInChildren<TMP_InputField>(true))
            {if(input.image){input.image.sprite=dark;input.image.type=Image.Type.Sliced;input.image.color=Color.white;} foreach(var t in input.GetComponentsInChildren<TMP_Text>(true)) {t.fontSize=42;t.fontSizeMax=42;t.fontSizeMin=28;}}
            if(scene=="Mainmenu") Menu(); else Game();
            new GameObject("Reference UI applied",typeof(RectTransform)).transform.SetParent(canvas,false);
            EditorSceneManager.SaveScene(s);
        }
        StyleStore();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Mainmenu.unity");
        Debug.Log("REFERENCE_UI_APPLIED: scenes and editable presentation assets saved.");
    }
    public static void RunBatch(){try{Apply();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Prepare()
    {
        Directory.CreateDirectory(Art);
        dark=MakeFrame("NavyFrame",false,false);primary=MakeFrame("GoldButton",true,false);outline=MakeFrame("BoardOutline",false,true);cell=MakeFrame("BoardCell",false,false,12,256,256,256);circle=MakeCircle();disc=MakeCircle(true);ring=MakeRing();
        Import(Art+"Marble.png",100,Vector4.zero);marble=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Marble.png");
        Import(Art+"Crown.png",100,Vector4.zero);crown=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Crown.png");
        var coins=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Coins/Coin.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();xCoin=coins[0];yCoin=coins[1];
        catalog=AssetDatabase.LoadAssetAtPath<GameTextCatalog>("Assets/Resources/Localization/GameTextCatalog.asset");
        AddText("ui.placeToChoose","Place a coin to choose your move","Coloca una ficha para elegir tu movimiento","Placez un pion pour choisir votre déplacement","ضع قطعة لاختيار حركتك");
        AddText("ui.brand","SECRET FOUR","SECRET FOUR","SECRET FOUR","سيكريت فور");
        AddText("ui.placeHint","Tap any empty cell","Toca una casilla vacía","Touchez une case vide","اضغط على أي خانة فارغة");
        AddText("ui.chooseHint","Choose a secret direction","Elige una dirección secreta","Choisissez une direction secrète","اختر اتجاهاً سرياً");
        AddText("ui.revealHint","Both moves are revealed together","Ambos movimientos se revelan juntos","Les deux coups sont révélés ensemble","تُكشف الحركتان معاً");
        AddText("ui.place","PLACE","COLOCAR","PLACER","ضع");AddText("ui.choose","CHOOSE","ELEGIR","CHOISIR","اختر");AddText("ui.reveal","REVEAL","REVELAR","RÉVÉLER","اكشف");
        AddText("ui.victoryHint","Four in a row. Well played.","Cuatro en línea. Bien jugado.","Quatre à la suite. Bien joué.","أربع قطع متصلة. أحسنت اللعب.");
        AddText("ui.drawHint","Evenly matched. Ready for another round?","Empate. ¿Otra ronda?","À égalité. Une autre partie ?","تعادل. هل أنت مستعد لجولة أخرى؟");
        AddText("ui.ready","Your room is ready","Tu sala está lista","Votre salon est prêt","غرفتك جاهزة");
        AddText("ui.roomInfo","The match begins when your friend joins","La partida comienza cuando se una tu amigo","La partie commence quand votre ami arrive","تبدأ المباراة عندما ينضم صديقك");
        AddText("ui.loadingHint","Getting everything ready","Preparando todo","Préparation en cours","جارٍ تجهيز كل شيء");
        AddText("ui.codeLength","6 characters","6 caracteres","6 caractères","٦ أحرف");
        AddText("ui.invite","Invite a friend with a room code","Invita con un código de sala","Invitez un ami avec un code","ادعُ صديقاً باستخدام رمز الغرفة");
        AddText("ui.joinHint","Enter a code to play together","Introduce un código para jugar","Saisissez un code pour jouer ensemble","أدخل رمزاً للعب معاً");
        AddText("ui.player","PLAYER","JUGADOR","JOUEUR","اللاعب");
        EditorUtility.SetDirty(catalog);
    }
    static void AddText(string key,string en,string es,string fr,string ar){if(catalog.entries.Any(e=>e.key==key))return;var l=catalog.entries.ToList();l.Add(new GameTextCatalog.Entry{key=key,english=en,spanish=es,french=fr,arabic=ar});catalog.entries=l.ToArray();}
    static void Import(string path,float ppu,Vector4 borders)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=ppu;importer.spriteBorder=borders;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
    }
    static Sprite MakeFrame(string name,bool filled,bool hollow,int corner=20,int width=256,int height=128,int ppu=100)
    {
        var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);
        var pixels=new Color[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            float dx=Mathf.Min(x,width-1-x),dy=Mathf.Min(y,height-1-y);
            float d=Mathf.Min(Mathf.Min(dx,dy),(dx+dy-corner)/1.4142f);
            float t=(float)y/(height-1);float noise=(Mathf.PerlinNoise(x*.51f,y*.51f)-.5f)*.028f;
            Color c=filled?Color.Lerp(new Color(.73f,.45f,.15f),new Color(1,.89f,.61f),t):Color.Lerp(new Color(.006f,.025f,.04f),new Color(.026f,.078f,.11f),t);
            c+=new Color(noise,noise,noise,0);
            if(d<2.2f)c=Color.Lerp(new Color(.64f,.36f,.09f),new Color(1,.91f,.65f),t);
            else if(d<4)c=ink;
            else if(d<5.3f)c=filled?new Color(1,.87f,.59f):new Color(.5f,.39f,.23f);
            if(hollow&&d>5.3f)c=Color.clear;
            c.a*=Mathf.Clamp01(d+1);pixels[y*width+x]=c;
        }
        tex.SetPixels(pixels);tex.Apply();string path=Art+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);Import(path,ppu,new Vector4(28,28,28,28));return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite MakeRing()
    {
        int size=768;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*(size/2));Color c=Color.Lerp(new Color(.75f,.43f,.12f),cream,(float)y/size);c.a=Mathf.Clamp01(7-Mathf.Abs(r-375));pixels[y*size+x]=c;}
        t.SetPixels(pixels);t.Apply();string path=Art+"LoadingRing.png";File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);Import(path,100,Vector4.zero);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite MakeCircle(bool solid=false)
    {
        const int size=128;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);var ps=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*64);Color c=solid?Color.white:r>60?gold:ink;c.a=Mathf.Clamp01(64-r);ps[y*size+x]=c;}
        t.SetPixels(ps);t.Apply();string path=Art+(solid?"Disc.png":"Circle.png");File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);Import(path,100,Vector4.zero);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Transform At(Transform root,string name){var t=root.Find(name);if(t==null)throw new Exception("Missing UI: "+root.name+"/"+name);return t;}
    static RectTransform Rect(Transform t)=>t as RectTransform;
    static void Place(Transform t,float x,float y,float w,float h){var r=Rect(t);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    static void Stretch(Transform t,float inset=0){var r=Rect(t);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;r.localScale=Vector3.one;}
    static Transform New(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.transform;}
    static Image Pic(Transform p,string name,Sprite sprite,float x,float y,float w,float h){var t=New(p,name);Place(t,x,y,w,h);var im=t.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;im.preserveAspect=true;return im;}
    static Image Panel(Transform p,string name,float x,float y,float w,float h,Sprite sprite=null){var im=Pic(p,name,sprite??dark,x,y,w,h);im.preserveAspect=false;im.type=Image.Type.Sliced;return im;}
    static TMP_Text Label(Transform p,string name,string key,float x,float y,float w,float h,float size,Color? tint=null)
    {
        var t=New(p,name);Place(t,x,y,w,h);var text=t.gameObject.AddComponent<RTLTextMeshPro>();text.font=catalog.latinFont;text.fontSize=size;text.fontSizeMax=size;text.fontSizeMin=Mathf.Min(26,size);text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;text.color=tint??cream;text.raycastTarget=false;
        var local=t.gameObject.AddComponent<LocalizedText>();local.key=key;local.Refresh();return text;
    }
    static void TextStyle(Transform t,float x,float y,float w,float h,float size,bool bold=false,Color? tint=null){Place(t,x,y,w,h);var text=t.GetComponent<TMP_Text>();text.fontSize=text.fontSizeMax=size;text.fontSizeMin=Mathf.Min(26,size);text.enableAutoSizing=true;text.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;text.alignment=TextAlignmentOptions.Center;text.color=tint??cream;}
    static ReferenceIcon Icon(Transform p,string name,ReferenceIcon.Shape shape,float x,float y,float size,Color? tint=null){var t=New(p,name);Place(t,x,y,size,size);var icon=t.gameObject.AddComponent<ReferenceIcon>();icon.shape=shape;icon.color=tint??gold;icon.raycastTarget=false;return icon;}
    static Image Line(Transform p,string name,float x,float y,float w,Color? tint=null){var im=Pic(p,name,null,x,y,w,2);im.color=tint??gold;return im;}
    static void Crown(Transform p,float y,float size=105){Pic(p,"Crown",crown,0,y,size*1.2f,size*1.2f);Line(p,"Crown left rule",-size/2-100,y-14,160);Line(p,"Crown right rule",size/2+100,y-14,160);}
    static void Pair(Transform p,float y,float size=260,float spread=140){var x=Pic(p,"Decorative X coin",xCoin,-spread,y,size,size);x.rectTransform.localEulerAngles=new Vector3(0,0,12);var z=Pic(p,"Decorative Y coin",yCoin,spread,y-15,size,size);z.rectTransform.localEulerAngles=new Vector3(0,0,-12);}
    static void StyleButton(Transform t,bool isPrimary)
    {
        var b=t.GetComponent<Button>();if(b==null)return;var image=t.GetComponent<Image>();if(image==null)image=t.gameObject.AddComponent<Image>();image.sprite=isPrimary?primary:dark;image.type=Image.Type.Sliced;image.preserveAspect=false;image.color=Color.white;b.targetGraphic=image;b.transition=Selectable.Transition.ColorTint;
        var colors=ColorBlock.defaultColorBlock;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.93f,.78f);colors.pressedColor=new Color(.72f,.66f,.52f);colors.selectedColor=Color.white;colors.disabledColor=new Color(.38f,.43f,.48f,.7f);b.colors=colors;
        foreach(var text in t.GetComponentsInChildren<TMP_Text>(true)){text.color=isPrimary?ink:cream;text.fontStyle=isPrimary?FontStyles.Bold:FontStyles.Normal;text.fontSizeMax=48;text.fontSize=48;text.fontSizeMin=25;}
    }
    static void ButtonAt(Transform root,string name,float x,float y,float w,float h,bool primaryButton=false){var t=At(root,name);Place(t,x,y,w,h);StyleButton(t,primaryButton);var label=t.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();if(label!=null){Stretch(label.transform,14);}}
    static void ButtonIcon(Transform t,ReferenceIcon.Shape icon,bool primaryButton=false){var r=Rect(t);Icon(t,"Icon",icon,-r.sizeDelta.x*.36f,0,r.sizeDelta.y*.48f,primaryButton?ink:gold);var label=t.GetComponentsInChildren<TMP_Text>(true).First();Place(label.transform,r.sizeDelta.x*.085f,0,r.sizeDelta.x*.71f,r.sizeDelta.y-22);}
    static Transform Content(string panel)
    {
        var p=At(canvas,panel);var content=At(p,"Safe Area/Content");Place(content,0,0,1080,1920);if(content.GetComponent<ReferenceLayout>()==null)content.gameObject.AddComponent<ReferenceLayout>();
        foreach(var im in p.GetComponentsInChildren<Image>(true).Where(i=>i.transform.parent==p&&i.GetComponent<Button>()==null)) {im.sprite=marble;im.type=Image.Type.Simple;im.color=new Color(.65f,.75f,.85f);im.preserveAspect=false;Stretch(im.transform);}
        return content;
    }
    static void StandardHeading(Transform p,float titleY=690,float subtitleY=580,float size=76)
    {
        Crown(p,840,110);TextStyle(At(p,"Heading"),0,titleY,940,150,size,true);var sub=p.Find("Subtitle");if(sub)TextStyle(sub,0,subtitleY,920,90,38,false,muted);var divider=p.Find("Heading divider");if(divider)Place(divider,0,subtitleY+40,180,2);
    }
    static void TopBack(Transform p,Transform source)
    {
        var clone=Object.Instantiate(source.gameObject,p);clone.name="Top back";foreach(var child in clone.GetComponentsInChildren<TMP_Text>(true))Object.DestroyImmediate(child.gameObject);Place(clone.transform,-450,855,90,90);clone.GetComponent<Image>().sprite=circle;clone.GetComponent<Image>().type=Image.Type.Simple;Icon(clone.transform,"Back arrow",ReferenceIcon.Shape.Back,0,0,50);
    }
    static void Menu()
    {
        var c=Content("MainmenuPanel");var home=At(canvas,"MainmenuPanel").GetComponent<Image>();home.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Panels/MenuBackdrop.png");home.color=Color.white;home.preserveAspect=false;
        TextStyle(At(c,"Game title"),0,615,850,310,142,false,gold);TextStyle(At(c,"Tagline"),0,395,900,75,40);TextStyle(At(c,"Footer"),0,-865,900,55,25,false,muted);At(c,"Studio").gameObject.SetActive(false);
        ButtonAt(c,"WithHumanButton",0,-270,940,160,true);ButtonIcon(At(c,"WithHumanButton"),ReferenceIcon.Shape.People,true);At(c,"WithHumanButton").GetComponentInChildren<TMP_Text>().fontSizeMax=64;
        ButtonAt(c,"WithBotButton",-242,-455,456,132);ButtonIcon(At(c,"WithBotButton"),ReferenceIcon.Shape.Robot);
        ButtonAt(c,"InRoomButton",242,-455,456,132);ButtonIcon(At(c,"InRoomButton"),ReferenceIcon.Shape.People);
        ButtonAt(c,"StoreButton",-242,-612,456,132);ButtonIcon(At(c,"StoreButton"),ReferenceIcon.Shape.Bag);
        ButtonAt(c,"RulesButton",242,-612,456,132);ButtonIcon(At(c,"RulesButton"),ReferenceIcon.Shape.Book);
        ButtonAt(c,"ExitButton",0,-768,440,110);IconButton(At(c,"Settings"),ReferenceIcon.Shape.Gear,450,865,95);
        c=Content("BotSelectionPanel");StandardHeading(c,515,300,90);Icon(c,"AI icon",ReferenceIcon.Shape.Robot,0,690,116);Place(At(c,"Heading"),0,515,900,230);Place(At(c,"Heading divider"),0,365,240,2);
        string[] difficulties={"EasyButton","MediumButton","HardButton"};for(int i=0;i<3;i++)
        {
            var t=At(c,difficulties[i]);Place(t,0,65-i*295,945,265);TextStyle(At(t,"Text (TMP)"),160,63,480,85,60,true);TextStyle(At(t,"Description"),160,-67,510,84,34,false,muted);
            Pic(t,"X coin",xCoin,-322,0,157,157);Pic(t,"Y coin",yCoin,-183,-8,157,157);
            for(int j=0;j<3;j++){var bar=Panel(t,"Difficulty mark "+(j+1),35+j*70,-6,54,26,j<=i?primary:dark);bar.color=j<=i?Color.white:Color.gray;}
        }
        ButtonAt(c,"BackButton",0,-795,620,118);TopBack(c,At(c,"BackButton"));
        c=Content("HumanSetupPanel");StandardHeading(c,700,605,84);Pair(c,380,240,125);
        for(int i=0;i<2;i++)
        {
            float y=60-i*325;var card=Panel(c,"Player "+(i+1)+" card",0,y,970,285);card.transform.SetAsFirstSibling();
            Pic(c,"Player "+(i+1)+" coin",i==0?xCoin:yCoin,-355,y+22,140,140);
            TextStyle(At(c,"Player "+i),90,y+78,680,75,51,true);var inputBg=At(c,i==0?"Bg":"Bg2");Place(inputBg,95,y-54,665,110);var im=inputBg.GetComponent<Image>();im.sprite=dark;im.type=Image.Type.Sliced;
        }
        ButtonAt(c,"StartButton",0,-570,965,156,true);ButtonAt(c,"BackToMainmenu",0,-760,965,134);TopBack(c,At(c,"BackToMainmenu"));
        c=Content("PlayInRoomPanel");StandardHeading(c,675,535,73);Pair(c,200,305,190);
        ButtonAt(c,"createroom",0,-235,940,220,true);ButtonAt(c,"joinroom",0,-495,940,210);
        foreach(var n in new[]{"createroom","joinroom"}){var b=At(c,n);bool create=n=="createroom";Icon(b,"Action icon",create?ReferenceIcon.Shape.Plus:ReferenceIcon.Shape.Enter,-325,0,102,create?ink:gold);TextStyle(At(b,"Text (TMP)"),95,35,640,90,60,true,create?ink:cream);Label(b,"Description",create?"ui.invite":"ui.joinHint",95,-49,640,76,34,create?ink:muted);}
        ButtonAt(c,"BackToMainmenu",0,-760,460,110);TopBack(c,At(c,"BackToMainmenu"));
        c=Content("JoinRoomPanel");StandardHeading(c,660,545,108);Pair(c,252,285,155);
        var codePanel=Panel(c,"Room code card",0,-160,960,350);codePanel.transform.SetAsFirstSibling();
        TextStyle(At(c,"Code label"),0,-57,840,85,52,false,gold);Place(At(c,"RoomCodeInput"),0,-172,840,128);Label(c,"Code length","ui.codeLength",0,-290,800,85,32,muted);
        TextStyle(At(c,"JoinRoomMessage"),0,-397,890,110,31,false,gold);ButtonAt(c,"JoinButton",0,-550,950,150,true);ButtonAt(c,"CloseButton",0,-760,950,135);TopBack(c,At(c,"CloseButton"));
        c=Content("Createroompanel");Crown(c,835);var h=At(c,"Heading");h.GetComponent<LocalizedText>().key="ui.ready";h.GetComponent<LocalizedText>().Refresh();TextStyle(h,0,610,950,160,66,true);
        Label(c,"Brand","ui.brand",0,750,940,105,70,gold).GetComponent<LocalizedText>().useTitleFont=true;TextStyle(At(c,"Share code"),0,485,910,90,40,false,muted);At(c,"Heading divider").gameObject.SetActive(false);
        var codeCard=Panel(c,"Room code card",0,195,925,360);codeCard.transform.SetAsFirstSibling();Label(c,"Room code label","room.code",0,295,840,65,33,muted);TextStyle(At(c,"RoomcodeText"),0,150,845,155,122,true);
        var waitingCard=Panel(c,"Waiting card",0,-235,925,355);waitingCard.transform.SetAsFirstSibling();Pic(c,"Host coin",xCoin,-320,-145,150,150);Pic(c,"Friend coin",yCoin,-320,-315,150,150);Label(c,"Host label","player.one",90,-140,570,70,42);TextStyle(At(c,"Waiting For Player Text"),85,-308,580,110,37);Line(c,"Waiting separator",0,-230,830,muted*.65f);
        Label(c,"Room info","ui.roomInfo",0,-505,940,95,32,muted);ButtonAt(c,"CancelButton",0,-745,750,120);
        c=Content("CreatingRoomPanel");Crown(c,805);var title=Label(c,"Brand","game.title",0,605,880,275,122,gold);title.GetComponent<LocalizedText>().useTitleFont=true;
        Pair(c,75,295,157);var orbit=Pic(c,"Loading orbit",ring,0,85,700,700);orbit.color=new Color(1,.8f,.44f,.75f);orbit.type=Image.Type.Filled;orbit.fillMethod=Image.FillMethod.Radial360;orbit.fillAmount=.74f;orbit.gameObject.AddComponent<LoadingOrbit>();orbit.transform.SetAsFirstSibling();
        TextStyle(At(c,"Loading message"),0,-340,990,175,63,true);Label(c,"Loading hint","ui.loadingHint",0,-455,900,90,41,muted);
        // Reuses the existing cancel handler; room creation logic is unchanged.
        var cancel=Object.Instantiate(At(Content("Createroompanel"),"CancelButton").gameObject,c);cancel.name="Cancel creation";Place(cancel.transform,0,-740,520,120);
        Rules();Settings(Content("SettingsPanel"));
        c=Content("StorePanel");Crown(c,850);TextStyle(At(c,"Header/StoreTitle"),0,720,900,120,88,true);TextStyle(At(c,"Subtitle"),0,610,910,85,38,false,muted);ButtonAt(At(c,"Header"),"CloseButton",0,-830,800,115);
    }
    static void IconButton(Transform t,ReferenceIcon.Shape shape,float x,float y,float size){Place(t,x,y,size,size);var im=t.GetComponent<Image>();im.sprite=circle;im.type=Image.Type.Simple;im.color=Color.white;Icon(t,"Icon",shape,0,0,size*.55f);}
    static void Rules()
    {
        var c=Content("RulesPanel");Crown(c,850,85);var title=Label(c,"Brand","ui.brand",0,735,930,100,78,gold);title.GetComponent<LocalizedText>().useTitleFont=true;
        var card=Panel(c,"Rules card",0,-45,990,1410);card.transform.SetAsFirstSibling();TextStyle(At(c,"Heading"),0,565,900,130,80,true);Place(At(c,"Heading divider"),0,470,180,2);
        var scroll=At(c,"Rules Scroll");Place(scroll,0,-100,840,1060);var rules=At(scroll,"Viewport/RulesInstructions").GetComponent<TMP_Text>();rules.fontSize=36;rules.fontSizeMax=36;rules.enableAutoSizing=false;rules.lineSpacing=5;
        TextStyle(At(c,"Scroll hint"),0,-670,830,65,29,false,muted);ButtonAt(c,"BackToMainmenu",0,-845,800,115,true);TopBack(c,At(c,"BackToMainmenu"));
    }
    static void Settings(Transform c)
    {
        StandardHeading(c,730,625,100);At(c,"Heading divider").gameObject.SetActive(false);Section(c,"Player section","ui.player",525);
        TextStyle(At(c,"Profile label"),0,440,880,66,42);Place(At(c,"Player name"),0,351,890,98);
        TextStyle(At(c,"Language heading"),0,230,880,60,40,false,gold);SectionLines(c,"Language",230);
        for(int i=0;i<4;i++){ButtonAt(c,"Language "+i,i%2==0?-230:230,i<2?127:7,436,104);}
        TextStyle(At(c,"Sound heading"),0,-130,880,68,40,false,gold);SectionLines(c,"Audio",-130);
        string[] volumes={"Master","Music","Effects"};for(int i=0;i<3;i++)
        {
            var row=At(c,volumes[i]);Place(row,0,-250-i*145,890,140);TextStyle(At(row,"Label"),-130,32,630,65,42);row.Find("Label").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;TextStyle(At(row,"Value"),370,32,140,65,40,false,gold);
            Place(At(row,"Slider"),0,-33,900,62);Place(At(row,"Slider/Track"),0,0,866,16);At(row,"Slider/Track").GetComponent<Image>().color=new Color(.08f,.17f,.23f);
        }
        ButtonAt(c,"Mute",0,-674,890,105);TextStyle(At(c,"Mute/Label"),-110,0,590,78,41);At(c,"Mute/Label").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
        var track=Panel(At(c,"Mute"),"Mute switch",330,0,118,57);track.sprite=circle;track.type=Image.Type.Simple;var knob=Pic(track.transform,"Knob",disc,-28,0,45,45);knob.color=cream;
        ButtonAt(c,"Back",0,-827,890,117);TextStyle(At(c,"Saved"),0,-913,970,82,28,false,muted);TopBack(c,At(c,"Back"));
        var selection=c.gameObject.AddComponent<SettingsSelectionVisuals>();selection.languages=Enumerable.Range(0,4).Select(i=>At(c,"Language "+i).GetComponent<Button>()).ToArray();selection.selectedSprite=primary;selection.normalSprite=dark;selection.muteKnob=knob.rectTransform;selection.muteTrack=track;selection.checks=selection.languages.Select(b=>Icon(b.transform,"Selected check",ReferenceIcon.Shape.Check,175,0,28,ink).gameObject).ToArray();
    }
    static void Section(Transform c,string name,string key,float y){Label(c,name,key,0,y,380,62,40,gold);SectionLines(c,name,y);}
    static void SectionLines(Transform c,string name,float y){Line(c,name+" left",-305,y,280);Line(c,name+" right",305,y,280);}
    static void Game()
    {
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.matchWidthOrHeight=0;Camera.main.gameObject.AddComponent<PortraitBoardCamera>();
        var bg=GameObject.Find("GameBackground").GetComponent<SpriteRenderer>();bg.sprite=marble;bg.color=Color.white;
        var board=Object.FindFirstObjectByType<BoardView>();var so=new SerializedObject(board);so.FindProperty("boardScale").floatValue=.76f;so.FindProperty("boardVerticalOffset").floatValue=-.15f;so.ApplyModifiedPropertiesWithoutUndo();
        var hud=At(canvas,"TopHUD");Place(hud,0,0,1080,1920);var edge=hud.GetComponent<SafeAreaEdge>();if(edge)Object.DestroyImmediate(edge);hud.GetComponent<Image>().enabled=false;
        Crown(hud,915,52);var title=Label(hud,"Brand","ui.brand",0,856,750,78,66,gold);title.GetComponent<LocalizedText>().useTitleFont=true;
        var playerCard=Panel(hud,"Player card",0,735,995,138);playerCard.transform.SetAsFirstSibling();Line(hud,"Player divider",0,735,1).rectTransform.sizeDelta=new Vector2(1,94);
        Place(At(hud,"PlayerX"),-245,735,470,120);Place(At(hud,"PlayerY"),245,735,470,120);
        Place(At(hud,"PlayerX/IconX"),-120,0,118,118);Place(At(hud,"PlayerY/IconY"),120,0,118,118);
        TextStyle(At(hud,"PlayerX/NameX"),72,9,260,80,43,true);TextStyle(At(hud,"PlayerY/NameY"),-72,9,260,80,43,true);TextStyle(At(hud,"BotDifficultyText"),212,687,255,46,27,false,muted);
        IconButton(At(hud,"SettingsButton"),ReferenceIcon.Shape.Gear,-450,873,95);IconButton(At(hud,"PauseButton"),ReferenceIcon.Shape.Pause,450,873,95);
        TextStyle(At(hud,"Turn player"),0,618,900,65,33,false,muted);TextStyle(At(hud,"TurnText"),0,537,960,82,68,true,gold);
        var hudLogic=Object.FindFirstObjectByType<GameHUD>();var hso=new SerializedObject(hudLogic);hso.FindProperty("useLegacyTurnLayout").boolValue=false;hso.ApplyModifiedPropertiesWithoutUndo();
        var visual=hud.gameObject.AddComponent<MatchPresentation>();visual.turnManager=Object.FindFirstObjectByType<TurnManager>();visual.guide=Label(hud,"Move guidance","ui.placeHint",0,461,920,60,31,muted);visual.xUnderline=Line(hud,"X active underline",-210,676,198);visual.yUnderline=Line(hud,"Y active underline",210,676,198);visual.xUnderline.rectTransform.sizeDelta=visual.yUnderline.rectTransform.sizeDelta=new Vector2(198,5);
        Panel(hud,"Board frame",0,-24,756,885,outline);
        var directions=At(canvas,"DirectionPanel");var safe=directions.GetComponent<SafeAreaEdge>();if(safe)Object.DestroyImmediate(safe);Place(directions,0,-700,780,380);
        ButtonAt(directions,"UpButton",0,113,192,98);ButtonAt(directions,"DownButton",0,-113,192,98);ButtonAt(directions,"LeftButton",-205,0,184,104);ButtonAt(directions,"RightButton",205,0,184,104);ButtonAt(directions,"StayButton",0,0,192,104);
        foreach(var n in new[]{"Up","Down","Left","Right"})Icon(At(directions,n+"Button"),"Direction icon",(ReferenceIcon.Shape)Enum.Parse(typeof(ReferenceIcon.Shape),n),0,0,66);
        Label(At(directions,"StayButton"),"Stay label","direction.stay",0,0,165,78,40);
        foreach(var button in directions.GetComponentsInChildren<Button>(true)){var decoration=button.gameObject.AddComponent<SelectableDecoration>();decoration.control=button;decoration.decorations=button.GetComponentsInChildren<Graphic>(true).Where(g=>g.gameObject!=button.gameObject).ToArray();}
        visual.phases=new TMP_Text[3];visual.phaseUnderlines=new Image[3];for(int i=0;i<3;i++){visual.phases[i]=Label(hud,"Phase "+i,new[]{"ui.place","ui.choose","ui.reveal"}[i],(i-1)*210,-505,200,75,27);visual.phaseUnderlines[i]=Line(hud,"Phase underline "+i,(i-1)*210,-539,128);visual.phaseUnderlines[i].rectTransform.sizeDelta=new Vector2(128,5);}
        visual.directionGuide=Label(hud,"Direction guidance","ui.placeToChoose",0,-912,970,76,31,muted);
        Overlay("Pausepanel","bg",805,970);var p=At(canvas,"Pausepanel");Crown(p,395,90);TextStyle(At(p,"Paused"),0,270,750,100,93,true);TextStyle(At(p,"Subtitle"),0,165,695,95,34,false,muted);
        ButtonAt(p,"ResumeButton",0,25,690,128,true);ButtonAt(p,"RestartButton",0,-120,690,128);ButtonAt(p,"MainMenuButton",0,-265,690,128);ButtonAt(p,"Settings",0,-410,690,105);
        Overlay("GameOverPanel","BG",790,1030);p=At(canvas,"GameOverPanel");Crown(p,432,90);TextStyle(At(p,"Result Title"),0,310,745,142,107,true,gold);TextStyle(At(p,"Winner name"),0,200,690,90,54,true);Place(At(p,"WinnerImage"),0,-35,305,305);
        ButtonAt(p,"PlayAgain",0,-315,680,124,true);ButtonAt(p,"MainMenu",0,-455,680,112);
        var result=p.gameObject.AddComponent<ResultPresentation>();result.winnerImage=At(p,"WinnerImage").GetComponent<Image>();result.background=Rect(At(p,"BG"));result.title=Rect(At(p,"Result Title"));result.playAgain=Rect(At(p,"PlayAgain"));result.mainMenu=Rect(At(p,"MainMenu"));result.caption=Label(p,"Result caption","ui.victoryHint",0,-225,725,75,33,muted);var draw=New(p,"Draw emblem");Place(draw,0,240,650,180);Pic(draw,"X coin",xCoin,-195,0,150,150);Pic(draw,"Y coin",yCoin,195,0,150,150);Pic(draw,"Crown",crown,0,0,90,90);result.drawEmblem=draw.gameObject;draw.gameObject.SetActive(false);
        // Crown moves with the modal's top edge for both result layouts.
        var resultCrown=At(p,"Crown");result.victoryCrown=resultCrown.gameObject;resultCrown.SetParent(result.background,false);Place(resultCrown,0,0,90,90);Rect(resultCrown).anchorMin=Rect(resultCrown).anchorMax=new Vector2(.5f,1);Rect(resultCrown).anchoredPosition=new Vector2(0,-85);At(p,"Crown left rule").gameObject.SetActive(false);At(p,"Crown right rule").gameObject.SetActive(false);
        Overlay("DisconnectPanel","BG",830,780);p=At(canvas,"DisconnectPanel");Crown(p,270,95);TextStyle(At(p,"Title"),0,115,730,130,71,true);TextStyle(At(p,"MessageText"),0,-50,720,160,40,false,muted);ButtonAt(p,"MainMenuButton",0,-250,690,125,true);
        Settings(Content("SettingsPanel"));
        // Only the default cosmetic changes. Purchased/selected alternative skins retain their own artwork.
        var item=AssetDatabase.LoadAssetAtPath<MN7.Store.MN7ItemData>("Assets/Resources/Boards/DefaultBoarditem.asset");item.icon=cell;EditorUtility.SetDirty(item);
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Cell.prefab");prefab.GetComponent<SpriteRenderer>().sprite=cell;
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"BoardCell.mat");if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(mat,Art+"BoardCell.mat");}prefab.GetComponent<SpriteRenderer>().sharedMaterial=mat;prefab.transform.localScale=new Vector3(.96f,.96f,1);PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Cell.prefab");PrefabUtility.UnloadPrefabContents(prefab);
    }
    static void Overlay(string name,string bgName,float w,float h)
    {
        var p=At(canvas,name);var bg=At(p,bgName).GetComponent<Image>();bg.sprite=dark;bg.type=Image.Type.Sliced;bg.color=Color.white;bg.preserveAspect=false;Place(bg.transform,0,0,w,h);
        var screen=p.GetComponent<Image>();if(screen){screen.sprite=null;screen.color=new Color(0,0,0,.68f);screen.raycastTarget=true;}
    }
    static void StyleStore()
    {
        foreach(var path in new[]{"Assets/Prefabs/MN7StoreItem.prefab","Assets/Prefabs/MN7StoreCategoryButton.prefab","Assets/Prefabs/MN7StoreItemActions.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            foreach(var b in root.GetComponentsInChildren<Button>(true))StyleButton(b.transform,false);
            foreach(var im in root.GetComponentsInChildren<Image>(true))if(im.name=="Bg"){im.sprite=dark;im.type=Image.Type.Sliced;im.color=Color.white;}
            foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))t.color=cream;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif





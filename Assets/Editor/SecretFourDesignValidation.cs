#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SecretFour.Localization;
using TMPro;
using RTLTMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class SecretFourDesignValidation
{
    static SecretFourDesignValidation()
    {
        EditorApplication.playModeStateChanged += state => {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DesignValidation",false))
            {
                SessionState.SetBool("DesignValidation",false);
                new GameObject("Design validation").AddComponent<SecretFourDesignProbe>();
            }
        };
    }
    public static void RunBatch()
    {
        typeof(SecretFourFlowValidation).GetMethod("SetPortraitGameView",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        EditorSceneManager.OpenScene("Assets/Scenes/Mainmenu.unity");
        SessionState.SetBool("DesignValidation",true);
        EditorApplication.EnterPlaymode();
    }
}
public class SecretFourDesignProbe : MonoBehaviour
{
    private readonly List<string> report=new List<string>();
    private readonly List<string> failures=new List<string>();
    private GameLanguage previousLanguage;
    private float master,music,effects;
    private bool muted;
    private float deadline;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        previousLanguage=GameLocalization.Language;
        master=AudioPreferences.Master;music=AudioPreferences.Music;effects=AudioPreferences.Effects;muted=AudioPreferences.Muted;
        deadline=Time.realtimeSinceStartup+180;
    }
    private void Update(){if(Time.realtimeSinceStartup>deadline)Finish("Design check timed out.");}
    private void Check(bool condition,string message){if(!condition)failures.Add(message);}
    private IEnumerator Settled(){yield return null;yield return null;yield return null;Canvas.ForceUpdateCanvases();}
    private IEnumerator Start()
    {
        yield return Settled();
        var catalog=GameLocalization.Catalog;
        Check(catalog!=null && catalog.latinFont!=null && catalog.arabicFont!=null && catalog.titleFont!=null,"Missing catalog/fonts");
        foreach(var entry in catalog.entries)
        {
            string placeholders=string.Join(",",Regex.Matches(entry.english,@"[{][0-9]+[}]").Cast<Match>().Select(m=>m.Value).OrderBy(x=>x));
            foreach(GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
            {
                string value=entry.Get(language);
                Check(!string.IsNullOrWhiteSpace(value),"Missing "+language+": "+entry.key);
                Check(placeholders==string.Join(",",Regex.Matches(value,@"{[0-9]+}").Cast<Match>().Select(m=>m.Value).OrderBy(x=>x)),"Placeholder mismatch: "+entry.key);
            }
        }
        report.Add("Catalog checked in all four languages, including formatting placeholders.");
        var menu=Object.FindFirstObjectByType<MainMenuManager>();
        foreach(GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
        {
            GameLocalization.SetLanguage(language);yield return Settled();
            Audit("Menu "+language);Capture("design-menu-"+language);
            menu.OpenSettings();yield return Settled();
            var settings=Object.FindFirstObjectByType<GameSettingsUI>();
            Check(settings!=null,"Settings missing");
            Audit("Settings "+language);Capture("design-settings-"+language);
            settings.Close();menu.CloseSettings();
            menu.OpenBotPanel();yield return Settled();Audit("Bot "+language);Capture("design-bot-"+language);menu.BackFromBotPanel();
            menu.OpenHumanSetup();yield return Settled();Audit("Setup "+language);Capture("design-setup-"+language);menu.BackToMainMenu();
            menu.OpenHowToPlay();yield return Settled();Audit("Rules "+language);Capture("design-rules-"+language);menu.CloseHowToPlay();
            menu.OpenStore();yield return Settled();Check(Object.FindObjectsByType<MN7.Store.MN7StoreItemUI>(FindObjectsSortMode.None).Length>0,"Store did not load items");Audit("Store "+language);Capture("design-store-"+language);
            foreach(var category in Object.FindObjectsByType<MN7.Store.MN7StoreCategoryButtonUI>(FindObjectsSortMode.None))
            {
                category.Button.onClick.Invoke();yield return Settled();
                var visible=Object.FindObjectsByType<MN7.Store.MN7StoreItemUI>(FindObjectsSortMode.None);
                Check(visible.Length>0,"Empty category "+category.CategoryId);
                if(category.CategoryId!=MN7.Store.MN7StoreConfig.AllCategoryId) Check(visible.All(card=>card.ItemData.categoryIds.Contains(category.CategoryId)),"Category filter mismatch "+category.CategoryId);
            }
            menu.CloseStore();
            menu.PlayInRoom();yield return Settled();Audit("Room "+language);Capture("design-room-"+language);
            var room=Object.FindFirstObjectByType<RoomUIController>();room.OpenJoinRoom();yield return Settled();Audit("Join "+language);Capture("design-join-"+language);
            // No external room is created during visual validation.
            var join=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0].GetComponentsInChildren<Transform>(true).First(x=>x.name=="JoinRoomPanel");
            join.gameObject.SetActive(false);menu.BackFromPlayInRoom();
        }
        menu.OpenSettings();yield return Settled();
        var ui=Object.FindFirstObjectByType<GameSettingsUI>();
        ui.master.value=.31f;ui.music.value=.22f;ui.effects.value=.63f;ui.muteButton.onClick.Invoke();
        Check(Mathf.Abs(AudioPreferences.Master-.31f)<.001f && Mathf.Abs(AudioPreferences.Music-.22f)<.001f && Mathf.Abs(AudioPreferences.Effects-.63f)<.001f,"Volume controls not applied");
        bool toggled=AudioPreferences.Muted;ui.Close();menu.CloseSettings();menu.OpenSettings();yield return Settled();
        Check(Mathf.Abs(ui.master.value-.31f)<.001f && Mathf.Abs(ui.music.value-.22f)<.001f && Mathf.Abs(ui.effects.value-.63f)<.001f && AudioPreferences.Muted==toggled,"Settings not retained after reopening");
        AudioPreferences.SetMuted(true);yield return null;
        var audio=Object.FindFirstObjectByType<GameAudio>();Check(audio!=null && audio.GetComponents<AudioSource>().All(s=>s.volume==0),"Mute did not silence all channels");
        AudioPreferences.SetMuted(false);yield return null;
        Check(audio.GetComponents<AudioSource>().Any(s=>Mathf.Abs(s.volume-.31f*.22f)<.001f),"Music mix is incorrect");
        Check(audio.GetComponents<AudioSource>().Any(s=>Mathf.Abs(s.volume-.31f*.63f)<.001f),"Effects mix is incorrect");
        report.Add("Master/music/effects sliders, mute, reopening and audio mixing passed.");
        ui.Close();menu.CloseSettings();
        GameSession.Instance.StartLocalHumanGame("أحمد","María");SceneManager.LoadScene("Game");yield return Settled();
        foreach(GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
        {
            GameLocalization.SetLanguage(language);yield return Settled();Audit("Game "+language);Capture("design-game-"+language);
            var settings=Object.FindObjectsByType<GameSettingsUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
            settings.Open();yield return Settled();Check(Time.timeScale==0,"Local settings did not pause game");settings.Close();Check(Time.timeScale==1,"Local settings did not restore play");
            var pause=Object.FindFirstObjectByType<PauseManager>();pause.PauseGame();settings.Open();yield return Settled();settings.Close();Check(Time.timeScale==0,"Settings unpaused the paused match");Capture("design-pause-"+language);pause.ResumeGame();
            var result=Object.FindFirstObjectByType<GameOverUI>();result.ShowWinner(PlayerType.X);yield return Settled();Audit("Result "+language);Capture("design-result-"+language);result.Hide();
        }
        GameSession.Instance.StartOnlineRoomGame();
        var onlineSettings=Object.FindObjectsByType<GameSettingsUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
        onlineSettings.Open();yield return Settled();Check(Time.timeScale==1,"Online settings paused shared match");onlineSettings.Close();
        report.Add("In-game language updates, Arabic player names, local pause preservation and online settings passed.");
        Object.FindFirstObjectByType<DisconnectUIManager>().ShowDisconnect("disconnect.lost");
        foreach(GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
        { GameLocalization.SetLanguage(language);yield return Settled();Audit("Disconnect "+language);Capture("design-disconnect-"+language); }
        Finish(null);
    }
    private void Audit(string screen)
    {
        foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if(!text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text))continue;
            text.ForceMeshUpdate();
            Check(text is RTLTextMeshPro,screen+": text lacks Arabic support "+text.name);
            if(text.GetComponentInParent<TMP_InputField>()!=null)continue;
            if(text.GetComponent<ContentSizeFitter>()!=null)continue;
            Check(!text.isTextOverflowing,screen+": text overflow "+text.name+" ("+text.text+")");
            var local=text.GetComponent<LocalizedText>();
            Check(local!=null,screen+": text is not editable/localized "+text.name);
            if(local!=null && !string.IsNullOrEmpty(local.key)) Check(GameLocalization.Text(local.key)!=local.key,screen+": unknown key "+local.key);
        }
    }
    private void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        int width = Screen.width, height = Screen.height;
        var target = new RenderTexture(width, height, 24);
        var oldTarget = camera.targetTexture;
        float oldAspect = camera.aspect;
        RenderMode mode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        camera.targetTexture = target;
        camera.aspect = (float)width / height;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
            new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        Directory.CreateDirectory("ValidationResults");
        File.WriteAllBytes("ValidationResults/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = oldTarget;
        camera.aspect = oldAspect;
        canvas.renderMode = mode;
        canvas.worldCamera = oldCamera;
        Destroy(texture); Destroy(target);
        Canvas.ForceUpdateCanvases();
    }


    private void Finish(string error)
    {
        enabled=false;
        if(error!=null)failures.Add(error);
        GameLocalization.SetLanguage(previousLanguage);
        AudioPreferences.SetMaster(master);AudioPreferences.SetMusic(music);AudioPreferences.SetEffects(effects);AudioPreferences.SetMuted(muted);AudioPreferences.Save();
        Time.timeScale=1;
        Directory.CreateDirectory("ValidationResults");
        report.AddRange(failures.Select(x=>"FAIL: "+x));
        report.Add(failures.Count==0?"PASS: all design checks.":"FAILURES: "+failures.Count);
        File.WriteAllLines("ValidationResults/design.txt",report);
        EditorApplication.Exit(failures.Count==0?0:1);
    }
}
#endif

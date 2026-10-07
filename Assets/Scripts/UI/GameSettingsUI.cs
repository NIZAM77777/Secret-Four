using SecretFour.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    public TMP_InputField playerName;
    public Slider master, music, effects;
    public TMP_Text masterValue, musicValue, effectsValue, muteLabel, savedLabel;
    public Button[] languageButtons;
    public Button muteButton;
    public bool pauseLocalGame;
    private float previousTimeScale;
    private bool pausedHere;
    private void OnEnable()
    {
        if (pauseLocalGame && (GameSession.Instance == null || GameSession.Instance.CurrentGameMode != GameMode.OnlineRoom))
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            pausedHere = true;
        }
        if (playerName != null)
        {
            playerName.characterLimit = 12;
            playerName.SetTextWithoutNotify(PlayerNameManager.GetPlayerName());
            playerName.onEndEdit.AddListener(SaveName);
        }
        master.SetValueWithoutNotify(AudioPreferences.Master);
        music.SetValueWithoutNotify(AudioPreferences.Music);
        effects.SetValueWithoutNotify(AudioPreferences.Effects);
        master.onValueChanged.AddListener(ChangeMaster);
        music.onValueChanged.AddListener(ChangeMusic);
        effects.onValueChanged.AddListener(ChangeEffects);
        muteButton.onClick.AddListener(ToggleMute);
        GameLocalization.Changed += Refresh;
        AudioPreferences.Changed += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (playerName != null) playerName.onEndEdit.RemoveListener(SaveName);
        master.onValueChanged.RemoveListener(ChangeMaster);
        music.onValueChanged.RemoveListener(ChangeMusic);
        effects.onValueChanged.RemoveListener(ChangeEffects);
        muteButton.onClick.RemoveListener(ToggleMute);
        GameLocalization.Changed -= Refresh;
        AudioPreferences.Changed -= Refresh;
        AudioPreferences.Save();
        if (pausedHere) { Time.timeScale = previousTimeScale; pausedHere = false; }
    }
    public void SelectEnglish() => GameLocalization.SetLanguage(GameLanguage.English);
    public void SelectSpanish() => GameLocalization.SetLanguage(GameLanguage.Spanish);
    public void SelectFrench() => GameLocalization.SetLanguage(GameLanguage.French);
    public void SelectArabic() => GameLocalization.SetLanguage(GameLanguage.Arabic);
    private void ChangeMaster(float value) => AudioPreferences.SetMaster(value);
    private void ChangeMusic(float value) => AudioPreferences.SetMusic(value);
    private void ChangeEffects(float value) => AudioPreferences.SetEffects(value);
    private void ToggleMute() => AudioPreferences.SetMuted(!AudioPreferences.Muted);
    private void SaveName(string value)
    {
        PlayerNameManager.SavePlayerName(value);
        playerName.SetTextWithoutNotify(PlayerNameManager.GetPlayerName());
    }
    private void Refresh()
    {
        masterValue.text = Mathf.RoundToInt(AudioPreferences.Master * 100) + "%";
        musicValue.text = Mathf.RoundToInt(AudioPreferences.Music * 100) + "%";
        effectsValue.text = Mathf.RoundToInt(AudioPreferences.Effects * 100) + "%";
        muteLabel.text = GameLocalization.Text(AudioPreferences.Muted ? "settings.unmute" : "settings.mute");
        if (savedLabel != null) savedLabel.text = GameLocalization.Text("settings.saved");
        for (int i = 0; i < languageButtons.Length; i++)
        {
            bool selected = i == (int)GameLocalization.Language;
            var colors = languageButtons[i].colors;
            colors.normalColor = selected ? new Color(1f, .81f, .43f) : Color.white;
            colors.selectedColor = colors.normalColor;
            languageButtons[i].colors = colors;
        }
    }
    public void Open() { transform.SetAsLastSibling(); gameObject.SetActive(true); }
    public void Close() { if (playerName != null) SaveName(playerName.text); gameObject.SetActive(false); }
}

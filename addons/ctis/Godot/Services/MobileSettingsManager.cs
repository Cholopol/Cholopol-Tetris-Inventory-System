using Godot;

namespace Ctis.Presentation;

/// <summary>
/// Manages mobile mode settings, persistence, and state change notifications.
/// </summary>
public sealed class MobileSettingsManager
{
    private const string ConfigPath = "user://ctis_settings.cfg";
    private const string SectionName = "controls";
    private const string KeyMobileMode = "mobile_mode";

    private bool _isMobileMode;

    public bool IsMobileMode
    {
        get => _isMobileMode;
        set => SetMobileMode(value);
    }

    public event Action<bool>? MobileModeChanged;

    public MobileSettingsManager()
    {
        LoadSettings();
    }

    public void SetMobileMode(bool enable, bool persist = true)
    {
        if (_isMobileMode == enable) return;
        _isMobileMode = enable;
        if (persist)
        {
            SaveSettings();
        }
        MobileModeChanged?.Invoke(_isMobileMode);
    }

    public void LoadSettings()
    {
        var config = new ConfigFile();
        var err = config.Load(ConfigPath);
        if (err == Error.Ok)
        {
            _isMobileMode = (bool)config.GetValue(SectionName, KeyMobileMode, DefaultMobileMode());
        }
        else
        {
            _isMobileMode = DefaultMobileMode();
        }
    }

    public void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue(SectionName, KeyMobileMode, _isMobileMode);
        config.Save(ConfigPath);
    }

    private static bool DefaultMobileMode()
    {
        var osName = OS.GetName();
        return osName is "Android" or "iOS";
    }
}

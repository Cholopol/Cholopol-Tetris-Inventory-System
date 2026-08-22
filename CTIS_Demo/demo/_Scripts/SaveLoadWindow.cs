using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

[DotPudicaView(typeof(SaveSlotListVM), Pooled = true)]
public partial class SaveLoadWindow : GodotWindow
{
    private VBoxContainer _list = null!;
    private Label _hint = null!;
    private CheckButton _mobileToggle = null!;
    private OptionButton _languageSelector = null!;
    private readonly List<SaveSlotRowView> _rows = new();

    [Inject] private MobileSettingsManager _settings = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();
        base._ExitTree();
    }

    public SaveLoadWindow()
    {
        WindowType = WindowType.Full;
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }

    partial void OnViewReady()
    {
        _settings ??= AppContext.Current.Services.GetRequiredService<MobileSettingsManager>();

        _list = GetNode<VBoxContainer>("Root/Panel/List");
        _hint = GetNode<Label>("Root/Hint");
        _mobileToggle = GetNode<CheckButton>("Root/Header/LocaleHost/ControlsHost/MobileToggle");
        _languageSelector = GetNode<OptionButton>("Root/Header/LocaleHost/ControlsHost/LanguageSelector");

        _mobileToggle.ButtonPressed = _settings.IsMobileMode;
        _mobileToggle.Toggled += OnMobileToggled;

        _languageSelector.Select(CtisLocale.IsChinese ? 1 : 0);
        _languageSelector.ItemSelected += OnLanguageSelected;

        UpdateHint();
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;
        CtisLocale.Changed += OnLocaleChanged;
        if (_settings != null)
        {
            _settings.MobileModeChanged += OnMobileModeChanged;
        }

        ViewModel.Refresh();
        var rowScene = GD.Load<PackedScene>("res://CTIS_Demo/demo/SaveSlotRow.tscn");
        while (_rows.Count < ViewModel.Slots.Count)
        {
            var row = rowScene.Instantiate<SaveSlotRowView>();
            _list.AddChild(row);
            _rows.Add(row);
        }
        for (int i = 0; i < ViewModel.Slots.Count; i++)
            _rows[i].Bind(ViewModel.Slots[i]);

        if (_mobileToggle != null && _settings != null)
        {
            _mobileToggle.ButtonPressed = _settings.IsMobileMode;
        }
        UpdateHint();
    }

    partial void OnViewDisposing()
    {
        if (_mobileToggle != null && GodotObject.IsInstanceValid(_mobileToggle))
            _mobileToggle.Toggled -= OnMobileToggled;

        if (_languageSelector != null && GodotObject.IsInstanceValid(_languageSelector))
            _languageSelector.ItemSelected -= OnLanguageSelected;

        CtisLocale.Changed -= OnLocaleChanged;
        if (_settings != null)
        {
            _settings.MobileModeChanged -= OnMobileModeChanged;
        }
    }

    private void OnLanguageSelected(long index)
    {
        CtisLocale.SetLocale(index == 1 ? "zh" : "en");
    }

    private void OnMobileToggled(bool isToggled)
    {
        _settings?.SetMobileMode(isToggled);
        UpdateHint();
    }

    private void OnMobileModeChanged(bool isMobile)
    {
        if (_mobileToggle != null && _mobileToggle.ButtonPressed != isMobile)
        {
            _mobileToggle.ButtonPressed = isMobile;
        }
        UpdateHint();
    }

    private void OnLocaleChanged()
    {
        _languageSelector?.Select(CtisLocale.IsChinese ? 1 : 0);
        ViewModel?.Refresh();
        UpdateHint();
    }

    private void UpdateHint()
    {
        if (_hint == null) return;
        _hint.Text = _settings is { IsMobileMode: true } ? "CTIS_MOBILE_HINT" : "CTIS_PRESS_B";
    }
}


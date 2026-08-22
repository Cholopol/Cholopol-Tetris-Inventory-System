using System.ComponentModel;
using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

[DotPudicaView(typeof(ItemInformationVM), Pooled = true)]
public partial class ItemInformationWindow : GodotWindow
{
    private const float PanelWidth = 150f;
    private const float HeaderHeight = 15f;
    private const float ArtHeight = 60f;
    private const float Padding = 2f;
    private const float Spacing = 3f;
    private const float CloseSize = 15f;

    private IBundle? _pending;
    private Control _art = null!;

    [Export, BindTo(nameof(ItemInformationVM.Title))]
    private Label _title = null!;

    [Export, BindTo(nameof(ItemInformationVM.Description))]
    private Label _description = null!;

    [Export, BindTo(nameof(ItemInformationVM.IconKey), Converter = typeof(IconKeyToTextureConverter))]
    private TextureRect _icon = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();
        base._ExitTree();
    }

    public ItemInformationWindow()
    {
        WindowType = WindowType.Popup;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(PanelWidth, Padding * 2 + HeaderHeight + Spacing + ArtHeight + Spacing + 24);
        Size = CustomMinimumSize;
    }

    partial void OnViewReady()
    {
        _title = GetNode<Label>("Drag/Box/Header/Title");
        _art = GetNode<Control>("Drag/Box/Art");
        _icon = GetNode<TextureRect>("Drag/Box/Art/IconHost/Icon");
        _description = GetNode<Label>("Drag/Box/Description");
        var close = GetNode<TextureButton>("Drag/Box/Header/Close");
        close.Pressed += OnClosePressed;
    }

    private void OnClosePressed() => AppContext.Current.WindowManager.Dismiss(this);

    private bool _needsCenter = true;

    protected override void OnCreate(IBundle? bundle)
    {
        _pending = bundle;
        _needsCenter = true;
        ApplyBundle();
    }

    partial void OnViewModelBound()
    {
        if (ViewModel != null)
            ViewModel.PropertyChanged += OnInfoPropertyChanged;
        _needsCenter = true;
        ApplyBundle();
        CallDeferred(nameof(FitWindow));
    }

    partial void OnViewDisposing()
    {
        var close = GetNodeOrNull<TextureButton>("Drag/Box/Header/Close");
        if (close != null && GodotObject.IsInstanceValid(close))
            close.Pressed -= OnClosePressed;

        if (ViewModel != null)
            ViewModel.PropertyChanged -= OnInfoPropertyChanged;
    }

    private void ApplyBundle()
    {
        if (_pending == null || ViewModel == null) return;
        ViewModel.Bind(
            _pending.Get<ItemDetails>("details"),
            _pending.ContainsKey("stack") ? _pending.Get<int>("stack") : 1);
        _pending = null;
        CallDeferred(nameof(FitWindow));
    }

    private void OnInfoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ItemInformationVM.Description)
            or nameof(ItemInformationVM.Title)
            or nameof(ItemInformationVM.IconKey))
            CallDeferred(nameof(FitWindow));
    }

    private void FitIcon()
    {
        if (_icon?.Texture == null) return;
        var native = _icon.Texture.GetSize();
        if (native.X < 1f || native.Y < 1f) return;
        var max = _art != null && _art.Size.X >= 1f
            ? _art.Size
            : new Vector2(PanelWidth - Padding * 2, ArtHeight);
        var scale = MathF.Min(1f, MathF.Min(max.X / native.X, max.Y / native.Y));
        _icon.CustomMinimumSize = native * scale;
        _icon.Size = _icon.CustomMinimumSize;
    }

    private void FitWindow()
    {
        if (_description == null) return;
        FitIcon();
        var descH = MathF.Max(8f, _description.GetCombinedMinimumSize().Y);
        var height = Padding * 2 + HeaderHeight + Spacing + ArtHeight + Spacing + descH;
        CustomMinimumSize = new Vector2(PanelWidth, height);
        Size = CustomMinimumSize;
        if (_needsCenter)
        {
            _needsCenter = false;
            CtisUi.CenterWindowOnScreen(this);
        }
    }
}

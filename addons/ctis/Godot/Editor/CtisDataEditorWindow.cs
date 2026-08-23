using Ctis.Core;
using Godot;
using TetrisCoordLib.Core.Math;

#if TOOLS
namespace Ctis.Presentation.Editor;

[Tool]
[GlobalClass]
public partial class CtisDataEditorWindow : Control
{
    private List<ItemDetails> _items = new();
    private readonly PlacementConfig _config = new();
    private readonly List<ItemDetails> _filteredItems = new();

    private int _tab;
    private int _itemIndex = -1;
    private bool _suppress;
    private bool _itemsDirty;
    private bool _configDirty;
    private bool _equipmentDirty;
    private bool _localeDirty;
    private bool _settingsDirty;

    private Button _itemsTab = null!;
    private Button _equipmentTab = null!;
    private Button _configTab = null!;
    private Button _settingsTab = null!;
    private Control _itemsPanel = null!;
    private Control _equipmentPanel = null!;
    private Control _configPanel = null!;
    private Control _settingsPanel = null!;
    private Label _status = null!;

    private List<EquipmentSlotSpec> _equipmentSlots = new();
    private ItemList _equipmentList = null!;
    private Control _equipmentDetails = null!;
    private Control _equipmentPlaceholder = null!;
    private int _selectedEquipmentIndex = -1;

    private InspectorIntField _slotIndexField = null!;
    private OptionButton _slotGroupField = null!;
    private OptionButton _slotTypeField = null!;
    private InspectorIntField _slotCellsWField = null!;
    private InspectorIntField _slotCellsHField = null!;
    private LineEdit _slotTitleKeyField = null!;

    private LineEdit _pathCatalog = null!;
    private LineEdit _pathEquipment = null!;
    private LineEdit _pathPlacement = null!;
    private LineEdit _pathLocale = null!;
    private LineEdit _pathMenuTheme = null!;
    private LineEdit _pathPocketScene = null!;
    private LineEdit _pathCofferScene = null!;

    private InspectorFloatField _gridTileW = null!;
    private InspectorFloatField _gridTileH = null!;
    private InspectorIntField _depositoryCols = null!;
    private InspectorIntField _depositoryRows = null!;

    private InspectorFloatField _longPressDuration = null!;
    private CheckBox _defaultMobileMode = null!;

    private ItemList _itemList = null!;
    private LineEdit _itemSearch = null!;
    private Control _itemDetails = null!;
    private Control _itemLogo = null!;
    private TextureRect _iconPreview = null!;
    private IconSheetPreview _iconSheet = null!;
    private Control _iconSheetHost = null!;
    private InspectorIntField _itemId = null!;
    private LineEdit _itemName = null!;
    private LineEdit _itemIcon = null!;
    private InspectorIntField _iconCols = null!;
    private InspectorIntField _iconRows = null!;
    private ShapeGridPreview _itemShapePreview = null!;
    private OptionButton _itemSlot = null!;
    private OptionButton _itemRarity = null!;
    private InspectorIntField _itemWidth = null!;
    private InspectorIntField _itemHeight = null!;
    private InspectorFloatField _itemWeight = null!;
    private OptionButton _itemDir = null!;
    private InspectorIntField _itemDamage = null!;
    private InspectorIntField _itemMaxStack = null!;
    private InspectorIntField _itemPrice = null!;
    private LineEdit _gridScene = null!;
    private LineEdit _nameKey = null!;
    private LineEdit _descKey = null!;
    private LineEdit _itemDesc = null!;
    private OptionButton _language = null!;
    private Button _saveButton = null!;
    private Label _headerTitle = null!;
    private bool _chinese;
    private int _iconCellX;
    private int _iconCellY;
    private readonly List<Action> _localeApplies = new();

    private CheckBox _blockSelf = null!;
    private CheckBox _blockBounds = null!;
    private CheckBox _blockOccupied = null!;
    private CheckBox _blockMismatch = null!;
    private CheckBox _overridePalette = null!;
    private ColorPickerButton _colorValid = null!;
    private ColorPickerButton _colorInvalid = null!;
    private ColorPickerButton _colorStack = null!;
    private ColorPickerButton _colorExchange = null!;
    private InspectorIntField _invalidSize = null!;
    private VBoxContainer _invalidRows = null!;

    public static Window CreateHostWindow()
    {
        var win = new Window
        {
            Title = "CTIS Data Editor",
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
            MinSize = new Vector2I(900, 600),
            Unresizable = false,
            WrapControls = true,
            Exclusive = false
        };
        var editor = new CtisDataEditorWindow();
        editor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        win.AddChild(editor);
        win.CloseRequested += () => win.QueueFree();
        return win;
    }

    public override void _Ready()
    {
        Name = "CtisDataEditor";
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _chinese = (bool)ProjectSettings.GetSetting("ctis/editor/use_chinese", false);
        CtisLocale.LoadCsv();
        LoadData();
        BuildUi();
        InspectorWidgets.NoTranslate(this);
        SwitchTab(0);
        SetStatus(Tr("Ready", "就绪"));
    }

    private string Tr(string en, string zh) => _chinese ? zh : en;

    private T Loc<T>(T control, string en, string zh) where T : Control
    {
        void Apply()
        {
            var text = Tr(en, zh);
            switch (control)
            {
                case Button button:
                    button.Text = text;
                    break;
                case Label label:
                    label.Text = text;
                    break;
                case LineEdit edit:
                    edit.PlaceholderText = text;
                    break;
                case FoldableContainer fold:
                    fold.Title = text;
                    break;
                case InspectorIntField intField:
                    intField.SetLabel(text);
                    break;
                case InspectorFloatField floatField:
                    floatField.SetLabel(text);
                    break;
            }
        }
        _localeApplies.Add(Apply);
        Apply();
        return control;
    }

    private void LocCaption(HBoxContainer row, string en, string zh)
    {
        if (row.GetChildCount() > 0 && row.GetChild(0) is Label label)
            Loc(label, en, zh);
    }

    private void ApplyEditorLocale()
    {
        foreach (var apply in _localeApplies)
            apply();
        RefreshItemList();
        if (CurrentItemOrNull() != null)
        {
            LoadNameFields(CurrentItem());
            LoadDescriptionFields(CurrentItem());
        }
        if (_invalidRows != null)
            RebuildInvalidRows();
        if (HasUnsavedChanges)
            SetStatus(Tr("Unsaved changes", "未保存的更改"));
        else
            SetStatus(Tr("Ready", "就绪"));
        SwitchTab(_tab);
    }

    private void LoadData()
    {
        _items = ItemCatalogLoader.LoadOrDefault();
        _equipmentSlots = EquipmentLayoutLoader.LoadOrDefault();
        _config.CopyFrom(PlacementConfigLoader.LoadOrDefault());
        _config.EnsureRarityColors();
        FilterItems(_itemSearch?.Text ?? "");
        RefreshEquipmentList();
    }

    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(root);

        var bg = new ColorRect
        {
            Color = CtisEditorTheme.RootBg,
            MouseFilter = MouseFilterEnum.Ignore
        };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
        MoveChild(bg, 0);

        root.AddChild(BuildHeader());

        var main = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(main);
        _itemsPanel = BuildItemsPanel();
        _equipmentPanel = BuildEquipmentPanel();
        _configPanel = BuildConfigPanel();
        _settingsPanel = BuildSettingsPanel();
        foreach (var panel in new[] { _itemsPanel, _equipmentPanel, _configPanel, _settingsPanel })
        {
            panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            main.AddChild(panel);
        }

        root.AddChild(BuildStatusBar());
        InspectorWidgets.NoTranslate(root);
        LoadSettingsValues();
    }

    private Control BuildHeader()
    {
        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.HeaderBg, marginX: 20, marginY: 0));
        header.CustomMinimumSize = new Vector2(0, 56);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        header.AddChild(row);

        _headerTitle = new Label
        {
            Text = "CTIS Data Editor",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin | SizeFlags.ExpandFill,
            ClipText = true,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        _headerTitle.AddThemeColorOverride("font_color", CtisEditorTheme.Text);
        _headerTitle.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontHeader);
        Loc(_headerTitle, "CTIS Data Editor", "CTIS 数据编辑器");
        var left = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        left.CustomMinimumSize = new Vector2(280, 0);
        left.AddChild(_headerTitle);
        row.AddChild(left);

        var tabs = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        tabs.AddThemeConstantOverride("separation", 2);
        var tabBar = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        tabBar.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.TabBarBg, radius: 6, marginX: 3, marginY: 3));
        tabBar.AddChild(tabs);
        row.AddChild(tabBar);

        _itemsTab = Loc(TabButton("Items"), "Items", "物品");
        _equipmentTab = Loc(TabButton("Equipment"), "Equipment", "装备");
        _configTab = Loc(TabButton("Config"), "Config", "配置");
        _settingsTab = Loc(TabButton("Settings"), "Settings", "设置");
        _itemsTab.Pressed += () => SwitchTab(0);
        _equipmentTab.Pressed += () => SwitchTab(1);
        _configTab.Pressed += () => SwitchTab(2);
        _settingsTab.Pressed += () => SwitchTab(3);
        tabs.AddChild(_itemsTab);
        tabs.AddChild(_equipmentTab);
        tabs.AddChild(_configTab);
        tabs.AddChild(_settingsTab);

        var right = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        right.CustomMinimumSize = new Vector2(280, 0);
        right.AddThemeConstantOverride("separation", 8);
        right.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        _language = new OptionButton
        {
            CustomMinimumSize = new Vector2(88, 28),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyOption(_language, compact: true);
        _language.AddItem("English");
        _language.AddItem("中文");
        _language.Select(_chinese ? 1 : 0);
        _language.ItemSelected += index =>
        {
            if (CurrentItemOrNull() != null)
            {
                ApplyNameFields(CurrentItem());
                ApplyDescriptionFields(CurrentItem());
            }
            _chinese = index == 1;
            ProjectSettings.SetSetting("ctis/editor/use_chinese", _chinese);
            ProjectSettings.Save();
            ApplyEditorLocale();
        };

        _saveButton = InspectorWidgets.ActionButton("Save All", CtisEditorTheme.Save, CtisEditorTheme.SaveHover, 2, 40);
        Loc(_saveButton, "Save All", "全局保存");
        _saveButton.Pressed += SaveAll;
        right.AddChild(_language);
        right.AddChild(_saveButton);
        row.AddChild(right);
        return header;
    }

    private static Button TabButton(string text)
    {
        var button = new Button
        {
            Text = text,
            ToggleMode = true,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(80, 40),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        button.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontBody);
        return button;
    }

    private void StyleTab(Button button, bool active)
    {
        var bg = active ? CtisEditorTheme.TabActive : Colors.Transparent;
        var hover = active ? CtisEditorTheme.TabActive.Lightened(0.08f) : new Color(0.18f, 0.18f, 0.19f);
        var fg = active ? Colors.White : CtisEditorTheme.Muted;
        CtisEditorTheme.ApplyButton(button, bg, hover, fg, 4);
        button.ButtonPressed = active;
    }

    private void SwitchTab(int tab)
    {
        _tab = tab;
        _itemsPanel.Visible = tab == 0;
        _equipmentPanel.Visible = tab == 1;
        _configPanel.Visible = tab == 2;
        _settingsPanel.Visible = tab == 3;
        StyleTab(_itemsTab, tab == 0);
        StyleTab(_equipmentTab, tab == 1);
        StyleTab(_configTab, tab == 2);
        StyleTab(_settingsTab, tab == 3);
    }

    private Control BuildStatusBar()
    {
        var bar = new PanelContainer();
        bar.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.Status, marginX: 16, marginY: 8));
        _status = new Label { Text = "Ready", AutoTranslateMode = AutoTranslateModeEnum.Disabled };
        _status.AddThemeColorOverride("font_color", Colors.White);
        _status.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontStatus);
        bar.AddChild(_status);
        return bar;
    }

    private void SetStatus(string text)
    {
        if (_status != null) _status.Text = text;
    }

    private bool HasUnsavedChanges => _itemsDirty || _equipmentDirty || _configDirty || _localeDirty || _settingsDirty;

    private void UpdateStatusUnsaved()
    {
        if (HasUnsavedChanges)
            SetStatus(Tr("Unsaved changes", "未保存的更改"));
    }

    private void MarkDirty()
    {
        if (_suppress) return;
        _itemsDirty = true;
        _localeDirty = true;
        UpdateStatusUnsaved();
    }

    private void MarkItemsDirty()
    {
        if (_suppress) return;
        _itemsDirty = true;
        UpdateStatusUnsaved();
    }

    private void MarkEquipmentDirty()
    {
        if (_suppress) return;
        _equipmentDirty = true;
        UpdateStatusUnsaved();
    }

    private void MarkConfigDirty()
    {
        if (_suppress) return;
        _configDirty = true;
        UpdateStatusUnsaved();
    }

    private void MarkLocaleDirty()
    {
        if (_suppress) return;
        _localeDirty = true;
        UpdateStatusUnsaved();
    }

    private void MarkSettingsDirty()
    {
        if (_suppress) return;
        _settingsDirty = true;
        UpdateStatusUnsaved();
    }

    private void SaveAll()
    {
        ApplyItemForm();
        if (CurrentItemOrNull() != null)
        {
            ApplyNameFields(CurrentItem());
            ApplyDescriptionFields(CurrentItem());
        }

        foreach (var item in _items)
            CommitLocaleKeys(item);

        if (!HasUnsavedChanges)
        {
            SetStatus(Tr("All data is up to date", "所有数据均已是最新，无需保存"));
            return;
        }

        var savedParts = new List<string>();

        if (_itemsDirty)
        {
            var itemErr = ItemCatalogLoader.Save(_items);
            if (itemErr != Error.Ok)
            {
                SetStatus(Tr("Failed to save item catalog", "物品数据保存失败"));
                return;
            }
            TouchFile(ItemCatalogLoader.CatalogPath);
            _itemsDirty = false;
            savedParts.Add(Tr("Items", "物品数据"));
        }

        if (_equipmentDirty)
        {
            var eqErr = EquipmentLayoutLoader.Save(_equipmentSlots);
            if (eqErr != Error.Ok)
            {
                SetStatus(Tr("Failed to save equipment layout", "装备槽位配置保存失败"));
                return;
            }
            TouchFile(EquipmentLayoutLoader.LayoutPath);
            _equipmentDirty = false;
            savedParts.Add(Tr("Equipment", "装备槽位"));
        }

        if (_configDirty)
        {
            _config.EnsureRarityColors();
            var configErr = PlacementConfigLoader.Save(_config);
            if (configErr != Error.Ok)
            {
                SetStatus(Tr("Failed to save placement config", "摆放规则保存失败"));
                return;
            }
            TouchFile(PlacementConfigLoader.ConfigPath);
            _configDirty = false;
            savedParts.Add(Tr("Config", "摆放规则"));
        }

        if (_localeDirty)
        {
            CtisLocale.Retain(LocaleKeysInUse());
            var localeErr = CtisLocale.SaveCsv();
            if (localeErr != Error.Ok)
            {
                SetStatus(Tr("Failed to save locale CSV", "多语言表保存失败"));
                return;
            }
            TouchFile(CtisLocale.CsvPath);
            _localeDirty = false;
            savedParts.Add(Tr("Locale", "多语言"));
        }

        if (_settingsDirty)
        {
            ProjectSettings.SetSetting("ctis/item_catalog", _pathCatalog.Text.Trim());
            ProjectSettings.SetSetting("ctis/equipment_layout", _pathEquipment.Text.Trim());
            ProjectSettings.SetSetting("ctis/placement_config", _pathPlacement.Text.Trim());
            ProjectSettings.SetSetting("ctis/locale", _pathLocale.Text.Trim());
            ProjectSettings.SetSetting("ctis/menu_theme", _pathMenuTheme.Text.Trim());
            ProjectSettings.SetSetting("ctis/scenes/pocket", _pathPocketScene.Text.Trim());
            ProjectSettings.SetSetting("ctis/scenes/coffer", _pathCofferScene.Text.Trim());

            ProjectSettings.SetSetting("ctis/grid/tile_size_width", _gridTileW.Value);
            ProjectSettings.SetSetting("ctis/grid/tile_size_height", _gridTileH.Value);
            ProjectSettings.SetSetting("ctis/grid/depository_columns", _depositoryCols.Value);
            ProjectSettings.SetSetting("ctis/grid/depository_rows", _depositoryRows.Value);
            ProjectSettings.SetSetting("ctis/input/long_press_duration", _longPressDuration.Value);
            ProjectSettings.SetSetting("ctis/input/mobile_mode_default", _defaultMobileMode.ButtonPressed);

            ProjectSettings.Save();
            _settingsDirty = false;
            savedParts.Add(Tr("Project Settings", "项目设置"));
        }

        var partList = string.Join(", ", savedParts);
        SetStatus(Tr($"Saved: {partList}", $"已保存: {partList}"));
    }

    private IEnumerable<string> LocaleKeysInUse()
    {
        foreach (var item in _items)
        {
            if (!string.IsNullOrEmpty(item.NameKey))
                yield return item.NameKey;
            if (!string.IsNullOrEmpty(item.DescriptionKey))
                yield return item.DescriptionKey;
        }
    }

    private static void TouchFile(string path)
    {
        try
        {
            EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(path);
        }
        catch (Exception)
        {
            // Not running inside the Godot editor host.
        }
    }

    #region Items

    private Control BuildItemsPanel()
    {
        var split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        split.SplitOffsets = new[] { 280 };
        split.AddChild(BuildItemListPane());
        split.AddChild(BuildItemDetailsHost());
        return split;
    }

    private Control BuildItemListPane()
    {
        var pane = new VBoxContainer();
        pane.AddThemeConstantOverride("separation", 0);
        pane.CustomMinimumSize = new Vector2(200, 0);
        var header = ListHeader("Item List", "物品列表", out var add, out var remove);
        add.Pressed += AddItem;
        remove.Pressed += DeleteItem;
        pane.AddChild(header);

        _itemSearch = new LineEdit
        {
            PlaceholderText = "Search...",
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyLineEdit(_itemSearch);
        Loc(_itemSearch, "Search...", "搜索...");
        _itemSearch.TextChanged += FilterItems;
        var searchPad = InspectorWidgets.Padded(_itemSearch, 8);
        searchPad.AddThemeConstantOverride("margin_top", 8);
        searchPad.AddThemeConstantOverride("margin_bottom", 4);
        pane.AddChild(searchPad);

        _itemList = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AllowReselect = true,
            FixedIconSize = new Vector2I(32, 32),
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        _itemList.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontBody);
        _itemList.ItemSelected += OnItemSelected;
        pane.AddChild(_itemList);
        RefreshItemList();
        return PaneWrap(pane);
    }

    private Control BuildItemDetailsHost()
    {
        var host = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _itemLogo = CreateLogo();
        _itemLogo.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        host.AddChild(_itemLogo);
        _itemDetails = BuildItemDetails();
        _itemDetails.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        host.AddChild(_itemDetails);
        _itemDetails.Visible = false;
        return host;
    }

    private Control BuildItemDetails()
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        var title = new Label
        {
            Text = "Item Details",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        title.AddThemeColorOverride("font_color", CtisEditorTheme.Text);
        title.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontTitle);
        Loc(title, "Item Details", "物品详情");
        header.AddChild(title);
        var headerPanel = new PanelContainer();
        headerPanel.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.SectionBg, marginX: 16, marginY: 12));
        headerPanel.AddChild(header);
        root.AddChild(headerPanel);

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(body);
        root.AddChild(scroll);

        var general = new VBoxContainer();
        general.AddThemeConstantOverride("separation", 8);
        _itemId = Loc(InspectorWidgets.IntField("ID", 0, 999999, 0), "ID", "ID");
        _itemId.Changed += v => { CurrentItem().ItemId = v; RefreshItemList(); MarkDirty(); };
        general.AddChild(_itemId);

        _nameKey = InspectorWidgets.TextField("");
        _nameKey.Editable = false;
        _nameKey.FocusMode = FocusModeEnum.None;
        _itemName = InspectorWidgets.TextField("");
        _itemName.TextChanged += _ => OnNameFieldsChanged();
        _itemName.FocusExited += OnNameCommitted;
        var nameKeyRow = InspectorWidgets.Labeled("Name Key", _nameKey);
        LocCaption(nameKeyRow, "Name Key", "名称键");
        var nameRow = InspectorWidgets.Labeled("Name", _itemName);
        LocCaption(nameRow, "Name", "名称");
        general.AddChild(nameKeyRow);
        general.AddChild(nameRow);

        _itemSlot = InspectorWidgets.EnumField(InventorySlotType.Pocket);
        _itemRarity = InspectorWidgets.EnumField(ItemRarity.Common);
        _itemSlot.ItemSelected += _ => { CurrentItem().SlotType = InspectorWidgets.ReadEnum<InventorySlotType>(_itemSlot); MarkDirty(); };
        _itemRarity.ItemSelected += _ =>
        {
            CurrentItem().Rarity = InspectorWidgets.ReadEnum<ItemRarity>(_itemRarity);
            RefreshItemShapePreview();
            MarkDirty();
        };
        var slotRow = InspectorWidgets.Labeled("Slot Type", _itemSlot);
        LocCaption(slotRow, "Slot Type", "槽位类型");
        var rarityRow = InspectorWidgets.Labeled("Rarity", _itemRarity);
        LocCaption(rarityRow, "Rarity", "稀有度");
        general.AddChild(slotRow);
        general.AddChild(rarityRow);
        body.AddChild(Loc(InspectorWidgets.Foldout("General", true, InspectorWidgets.Padded(general)), "General", "常规"));
        body.AddChild(Loc(InspectorWidgets.Foldout("Image", true, InspectorWidgets.Padded(BuildItemImageEditor())), "Image", "图片"));

        var props = new GridContainer { Columns = 2 };
        props.AddThemeConstantOverride("h_separation", 12);
        props.AddThemeConstantOverride("v_separation", 8);
        _itemWeight = Loc(InspectorWidgets.FloatField("Weight", 0, 9999, 0), "Weight", "重量");
        _itemDir = InspectorWidgets.EnumField(Dir.Down);
        _itemWeight.Changed += v => { CurrentItem().Weight = v; MarkDirty(); };
        _itemDir.ItemSelected += _ =>
        {
            CurrentItem().DefaultDirection = InspectorWidgets.ReadEnum<Dir>(_itemDir);
            RefreshItemShapePreview();
            MarkDirty();
        };
        props.AddChild(_itemWeight);
        var dirRow = InspectorWidgets.Labeled("Dir", _itemDir, 40);
        LocCaption(dirRow, "Dir", "方向");
        props.AddChild(dirRow);
        body.AddChild(Loc(InspectorWidgets.Foldout("Properties", true, InspectorWidgets.Padded(props)), "Properties", "属性"));

        var combat = new GridContainer { Columns = 2 };
        combat.AddThemeConstantOverride("h_separation", 12);
        combat.AddThemeConstantOverride("v_separation", 8);
        _itemDamage = Loc(InspectorWidgets.IntField("Damage", 0, 99999, 0), "Damage", "伤害");
        _itemMaxStack = Loc(InspectorWidgets.IntField("Max Stack", 1, 999, 1), "Max Stack", "最大堆叠");
        _itemDamage.Changed += v => { CurrentItem().ItemDamage = v; MarkDirty(); };
        _itemMaxStack.Changed += v => { CurrentItem().MaxStack = v; MarkDirty(); };
        combat.AddChild(_itemDamage);
        combat.AddChild(_itemMaxStack);
        body.AddChild(Loc(InspectorWidgets.Foldout("Combat", false, InspectorWidgets.Padded(combat)), "Combat", "战斗"));

        var vendor = new VBoxContainer();
        _itemPrice = Loc(InspectorWidgets.IntField("Price", 0, 999999, 0), "Price", "价格");
        _itemPrice.Changed += v => { CurrentItem().ItemPrice = v; MarkDirty(); };
        vendor.AddChild(_itemPrice);
        body.AddChild(Loc(InspectorWidgets.Foldout("Vendor", false, InspectorWidgets.Padded(vendor)), "Vendor", "商店"));

        var refs = new VBoxContainer();
        refs.AddThemeConstantOverride("separation", 8);
        refs.AddChild(BuildPathRow(out _gridScene, "Grid UI", "网格界面", "*.tscn", path =>
        {
            CurrentItem().GridPanelSceneKey = path;
            MarkDirty();
        }));
        body.AddChild(Loc(InspectorWidgets.Foldout("References", false, InspectorWidgets.Padded(refs)), "References", "引用"));

        var desc = new VBoxContainer();
        desc.AddThemeConstantOverride("separation", 8);
        _descKey = InspectorWidgets.TextField("");
        _descKey.Editable = false;
        _descKey.FocusMode = FocusModeEnum.None;
        var descKeyRow = InspectorWidgets.Labeled("Desc Key", _descKey);
        LocCaption(descKeyRow, "Desc Key", "描述键");
        desc.AddChild(descKeyRow);
        _itemDesc = InspectorWidgets.TextField("");
        Loc(_itemDesc, "Item description", "物品描述");
        _itemDesc.TextChanged += _ => OnDescriptionChanged();
        var descRow = InspectorWidgets.Labeled("Description", _itemDesc);
        LocCaption(descRow, "Description", "描述");
        desc.AddChild(descRow);
        body.AddChild(Loc(InspectorWidgets.Foldout("Description", true, InspectorWidgets.Padded(desc)), "Description", "描述"));

        return root;
    }

    private Control BuildItemImageEditor()
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);

        var iconRow = new HBoxContainer();
        iconRow.AddThemeConstantOverride("separation", 16);
        _iconPreview = new TextureRect
        {
            CustomMinimumSize = new Vector2(80, 80),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest
        };
        var previewFrame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(80, 80),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        previewFrame.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.FieldBg, CtisEditorTheme.BorderStrong, 8, 4, 4));
        previewFrame.AddChild(_iconPreview);
        iconRow.AddChild(previewFrame);

        var sheet = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        sheet.AddThemeConstantOverride("separation", 6);
        _iconCols = Loc(InspectorWidgets.IntField("Cols", 1, 16, 1), "Cols", "列");
        _iconRows = Loc(InspectorWidgets.IntField("Rows", 1, 16, 1), "Rows", "行");
        _iconCols.Changed += _ => OnIconSheetChanged();
        _iconRows.Changed += _ => OnIconSheetChanged();
        sheet.AddChild(_iconCols);
        sheet.AddChild(_iconRows);
        iconRow.AddChild(sheet);
        root.AddChild(iconRow);

        root.AddChild(BuildPathRow(out _itemIcon, "Icon", "图标", "*.png,*.jpg,*.jpeg,*.webp,*.svg,*.tres", path =>
        {
            _iconCellX = 0;
            _iconCellY = 0;
            ApplyIconKey(path);
        }));

        _iconSheet = new IconSheetPreview
        {
            CustomMinimumSize = new Vector2(160, 160),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _iconSheet.CellPicked += OnIconCellPicked;
        _iconSheetHost = new PanelContainer
        {
            Visible = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 168)
        };
        _iconSheetHost.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.FieldBg, CtisEditorTheme.Border, 6, 8, 8));
        _iconSheetHost.AddChild(_iconSheet);
        root.AddChild(_iconSheetHost);
        root.AddChild(BuildItemShapeEditor());
        return root;
    }

    private Control BuildItemShapeEditor()
    {
        _itemWidth = Loc(InspectorWidgets.IntField("Width", 1, 32, 1), "Width", "宽度");
        _itemHeight = Loc(InspectorWidgets.IntField("Height", 1, 32, 1), "Height", "高度");
        _itemWidth.Changed += v => ResizeItemOccupancy(v, null);
        _itemHeight.Changed += v => ResizeItemOccupancy(null, v);

        _itemShapePreview = new ShapeGridPreview { Editable = true };
        _itemShapePreview.CellClicked += ToggleItemOccupancyCell;

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        center.AddChild(_itemShapePreview);

        var dims = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        dims.AddThemeConstantOverride("h_separation", 12);
        dims.AddChild(_itemWidth);
        dims.AddChild(_itemHeight);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(center);
        body.AddChild(dims);

        var frame = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 200)
        };
        frame.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.PaneBg, CtisEditorTheme.Border, 6, 12, 12));
        frame.AddChild(body);
        return frame;
    }

    private Control BuildPathRow(out LineEdit edit, string en, string zh, string filter, Action<string> onPicked)
    {
        edit = InspectorWidgets.TextField("");
        var localEdit = edit;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var pick = InspectorWidgets.IconButton("⊙", CtisEditorTheme.FieldBg, CtisEditorTheme.FieldBg.Lightened(0.1f));
        pick.Pressed += () =>
        {
            var dialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.OpenFile,
                Access = FileDialog.AccessEnum.Resources,
                Title = Tr(en, zh),
                AutoTranslateMode = AutoTranslateModeEnum.Disabled
            };
            dialog.AddFilter(filter);
            dialog.FileSelected += path =>
            {
                localEdit.Text = path;
                onPicked(path);
            };
            AddChild(dialog);
            dialog.PopupCentered(new Vector2I(700, 480));
        };
        localEdit.TextChanged += text => onPicked(text);
        row.AddChild(localEdit);
        row.AddChild(pick);
        var labeled = InspectorWidgets.Labeled(en, row);
        LocCaption(labeled, en, zh);
        return labeled;
    }


    private void FilterItems(string search)
    {
        var selected = CurrentItemOrNull();
        _filteredItems.Clear();
        if (string.IsNullOrWhiteSpace(search))
        {
            _filteredItems.AddRange(_items);
        }
        else
        {
            var q = search.Trim();
            foreach (var item in _items)
            {
                if (item.ItemId.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || ItemLabel(item).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || item.NameKey.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || CtisLocale.Get(item.NameKey, false).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || CtisLocale.Get(item.NameKey, true).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || item.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase))
                    _filteredItems.Add(item);
            }
        }
        RefreshItemList();
        if (selected == null) return;
        var index = _filteredItems.IndexOf(selected);
        if (index >= 0)
        {
            _itemIndex = index;
            _itemList.Select(index);
            _itemDetails.Visible = true;
            _itemLogo.Visible = false;
        }
        else
        {
            _itemIndex = -1;
            _itemDetails.Visible = false;
            _itemLogo.Visible = true;
        }
    }

    private void RefreshItemList()
    {
        if (_itemList == null) return;
        var selected = CurrentItemOrNull();
        _itemList.Clear();
        var fallback = CtisArt.LoadCopied(CtisArt.DefaultIcon);
        foreach (var item in _filteredItems)
        {
            var icon = CtisArt.LoadCopied(item.IconKey) ?? fallback;
            _itemList.AddItem(ItemLabel(item), icon);
        }
        if (selected != null)
        {
            var index = _filteredItems.IndexOf(selected);
            if (index >= 0)
            {
                var was = _suppress;
                _suppress = true;
                _itemList.Select(index);
                _suppress = was;
            }
        }
    }

    private string ItemLabel(ItemDetails item)
    {
        if (!string.IsNullOrEmpty(item.NameKey))
        {
            var translated = CtisLocale.Lookup(item.NameKey, _chinese);
            if (!string.IsNullOrEmpty(translated) && translated != item.NameKey)
                return translated;
        }
        return string.IsNullOrEmpty(item.DisplayName) ? $"Item {item.ItemId}" : item.DisplayName;
    }

    private void OnItemSelected(long index)
    {
        if (_suppress) return;
        var next = (int)index;
        if (next == _itemIndex && _itemDetails.Visible)
            return;
        ApplyItemForm();
        ShowItemAt(next);
    }

    private void ShowItemAt(int index)
    {
        if (index < 0 || index >= _filteredItems.Count)
        {
            _itemIndex = -1;
            _itemDetails.Visible = false;
            _itemLogo.Visible = true;
            return;
        }
        _itemIndex = index;
        var was = _suppress;
        _suppress = true;
        _itemList.Select(index);
        _itemList.EnsureCurrentIsVisible();
        _suppress = was;
        _itemDetails.Visible = true;
        _itemLogo.Visible = false;
        LoadItemForm();
    }

    private ItemDetails CurrentItem()
        => CurrentItemOrNull() ?? throw new InvalidOperationException("No item selected.");

    private ItemDetails? CurrentItemOrNull()
    {
        if (_itemIndex < 0 || _itemIndex >= _filteredItems.Count) return null;
        return _filteredItems[_itemIndex];
    }

    private void LoadItemForm()
    {
        var item = CurrentItemOrNull();
        if (item == null) return;
        _suppress = true;
        _itemId.SetValueWithoutNotify(item.ItemId);
        LoadNameFields(item);
        LoadIconFields(item);
        InspectorWidgets.SelectEnum(_itemSlot, item.SlotType);
        InspectorWidgets.SelectEnum(_itemRarity, item.Rarity);
        var occupancy = ItemShape.Ensure(item.Occupancy);
        item.Occupancy = occupancy;
        _itemWidth.SetValueWithoutNotify(occupancy.Width);
        _itemHeight.SetValueWithoutNotify(occupancy.Height);
        _itemWeight.SetValueWithoutNotify(item.Weight);
        InspectorWidgets.SelectEnum(_itemDir, item.DefaultDirection);
        _itemDamage.SetValueWithoutNotify(item.ItemDamage);
        _itemMaxStack.SetValueWithoutNotify(item.MaxStack);
        _itemPrice.SetValueWithoutNotify(item.ItemPrice);
        _gridScene.Text = item.GridPanelSceneKey;
        _descKey.Text = item.DescriptionKey;
        LoadDescriptionFields(item);
        _suppress = false;
        RefreshItemShapePreview();
    }

    private void LoadDescriptionFields(ItemDetails item)
    {
        var was = _suppress;
        _suppress = true;
        _descKey.Text = item.DescriptionKey;
        _itemDesc.Text = string.IsNullOrEmpty(item.DescriptionKey)
            ? ""
            : CtisLocale.Get(item.DescriptionKey, _chinese);
        _suppress = was;
    }

    private void ApplyDescriptionFields(ItemDetails item)
        => WriteDescriptionTranslation(item);

    private void OnDescriptionChanged()
    {
        if (_suppress) return;
        ApplyDescriptionFields(CurrentItem());
        MarkDirty();
    }

    private void UpdateIconPreview()
    {
        var item = CurrentItemOrNull();
        var key = item?.IconKey ?? "";
        _iconPreview.Texture = CtisArt.Load(key) ?? CtisArt.Load(CtisArt.DefaultIcon);
        bool sheet = _iconCols.Value > 1 || _iconRows.Value > 1;
        _iconSheetHost.Visible = sheet;
        if (!sheet) return;
        _iconSheet.SetSheet(
            CtisArt.LoadBase(_itemIcon.Text) ?? CtisArt.Load(CtisArt.DefaultIcon),
            _iconCols.Value,
            _iconRows.Value,
            _iconCellX,
            _iconCellY);
    }

    private void LoadIconFields(ItemDetails item)
    {
        var icon = ItemIconRef.Parse(item.IconKey);
        _itemIcon.Text = icon.Path;
        _iconCellX = 0;
        _iconCellY = 0;
        _iconCols.SetValueWithoutNotify(1);
        _iconRows.SetValueWithoutNotify(1);
        var tex = CtisArt.LoadBase(icon.Path);
        if (icon.HasRegion && tex != null && icon.Width > 0 && icon.Height > 0)
        {
            int cols = Math.Max(1, tex.GetWidth() / icon.Width);
            int rows = Math.Max(1, tex.GetHeight() / icon.Height);
            _iconCols.SetValueWithoutNotify(cols);
            _iconRows.SetValueWithoutNotify(rows);
            _iconCellX = Math.Clamp(icon.X / icon.Width, 0, cols - 1);
            _iconCellY = Math.Clamp(icon.Y / icon.Height, 0, rows - 1);
        }
        UpdateIconPreview();
    }

    private void OnIconSheetChanged()
    {
        if (_suppress) return;
        _iconCellX = Math.Clamp(_iconCellX, 0, Math.Max(0, _iconCols.Value - 1));
        _iconCellY = Math.Clamp(_iconCellY, 0, Math.Max(0, _iconRows.Value - 1));
        ApplyIconKey(_itemIcon.Text);
    }

    private void OnIconCellPicked(int x, int y)
    {
        if (_suppress) return;
        _iconCellX = x;
        _iconCellY = y;
        ApplyIconKey(_itemIcon.Text);
    }

    private void ApplyIconKey(string path)
    {
        if (_suppress) return;
        var item = CurrentItemOrNull();
        if (item == null) return;
        item.IconKey = ComposeIconKey(path);
        var icon = ItemIconRef.Parse(item.IconKey);
        var was = _suppress;
        _suppress = true;
        _itemIcon.Text = icon.Path;
        _suppress = was;
        UpdateIconPreview();
        RefreshItemShapePreview();
        RefreshItemList();
        MarkDirty();
    }

    private string ComposeIconKey(string path)
    {
        var source = ItemIconRef.Parse(path).Path;
        int cols = Math.Max(1, _iconCols.Value);
        int rows = Math.Max(1, _iconRows.Value);
        if (cols == 1 && rows == 1)
            return source;
        var tex = CtisArt.LoadBase(source);
        if (tex == null)
            return source;
        int cellW = Math.Max(1, tex.GetWidth() / cols);
        int cellH = Math.Max(1, tex.GetHeight() / rows);
        int x = Math.Clamp(_iconCellX, 0, cols - 1) * cellW;
        int y = Math.Clamp(_iconCellY, 0, rows - 1) * cellH;
        return new ItemIconRef(source, x, y, cellW, cellH).ToKey();
    }

    private void RefreshItemShapePreview()
    {
        if (_itemShapePreview == null) return;
        var item = CurrentItemOrNull();
        if (item == null)
        {
            _itemShapePreview.SetCanvas(1, 1, Array.Empty<Vec2I>());
            _itemShapePreview.SetTexture(null);
            _itemShapePreview.SetCellTint(Colors.Transparent);
            return;
        }
        var occupancy = ItemShape.Ensure(item.Occupancy);
        item.Occupancy = occupancy;
        _itemShapePreview.SetCanvas(occupancy.Width, occupancy.Height, occupancy.Cells);
        _itemShapePreview.SetTexture(CtisArt.Load(item.IconKey) ?? CtisArt.Load(CtisArt.DefaultIcon));
        _itemShapePreview.SetCellTint(InspectorWidgets.ToColor(_config.GetRarityColor(item.Rarity)));
    }

    private void ResizeItemOccupancy(int? width, int? height)
    {
        var item = CurrentItemOrNull();
        if (item == null || _suppress) return;
        var occupancy = ItemShape.Ensure(item.Occupancy);
        item.Occupancy = ItemShape.Resize(occupancy, width ?? occupancy.Width, height ?? occupancy.Height);
        RefreshItemShapePreview();
        MarkDirty();
    }

    private void ToggleItemOccupancyCell(int x, int y)
    {
        var item = CurrentItemOrNull();
        if (item == null || _suppress) return;
        var occupancy = ItemShape.Ensure(item.Occupancy);
        item.Occupancy = occupancy;
        if ((uint)x >= (uint)occupancy.Width || (uint)y >= (uint)occupancy.Height) return;
        var cell = new Vec2I(x, y);
        var index = occupancy.Cells.FindIndex(p => p.X == cell.X && p.Y == cell.Y);
        if (index >= 0)
        {
            if (occupancy.Cells.Count <= 1) return;
            occupancy.Cells.RemoveAt(index);
        }
        else
            occupancy.Cells.Add(cell);
        RefreshItemShapePreview();
        MarkDirty();
    }

    private void ApplyItemForm()
    {
        var item = CurrentItemOrNull();
        if (item == null || _suppress) return;
        item.ItemId = _itemId.Value;
        ApplyNameFields(item);
        item.IconKey = ComposeIconKey(_itemIcon.Text);
        item.SlotType = InspectorWidgets.ReadEnum<InventorySlotType>(_itemSlot);
        item.Rarity = InspectorWidgets.ReadEnum<ItemRarity>(_itemRarity);
        item.Occupancy = ItemShape.Resize(ItemShape.Ensure(item.Occupancy), _itemWidth.Value, _itemHeight.Value);
        item.Weight = _itemWeight.Value;
        item.DefaultDirection = InspectorWidgets.ReadEnum<Dir>(_itemDir);
        item.ItemDamage = _itemDamage.Value;
        item.MaxStack = _itemMaxStack.Value;
        item.ItemPrice = _itemPrice.Value;
        item.GridPanelSceneKey = _gridScene.Text;
        ApplyDescriptionFields(item);
    }

    private void LoadNameFields(ItemDetails item)
    {
        var was = _suppress;
        _suppress = true;
        _nameKey.Text = item.NameKey;
        var text = CtisLocale.Get(item.NameKey, _chinese);
        if (string.IsNullOrEmpty(text)
            && string.IsNullOrEmpty(CtisLocale.Get(item.NameKey, !_chinese)))
            text = item.DisplayName;
        _itemName.Text = text;
        _suppress = was;
    }

    private void ApplyNameFields(ItemDetails item)
    {
        CommitLocaleKeys(item);
        WriteNameTranslation(item);
    }

    private void CommitLocaleKeys(ItemDetails item)
    {
        var token = UniqueToken(EnglishName(item), item);
        if (string.IsNullOrEmpty(token)) return;
        var nameKey = ItemLocaleKeys.Name(token);
        var descKey = ItemLocaleKeys.Desc(token);
        if (item.NameKey != nameKey)
            CtisLocale.Rename(item.NameKey, nameKey);
        if (item.DescriptionKey != descKey)
            CtisLocale.Rename(item.DescriptionKey, descKey);
        item.NameKey = nameKey;
        item.DescriptionKey = descKey;
        if (!ReferenceEquals(item, CurrentItemOrNull())) return;
        var was = _suppress;
        _suppress = true;
        _nameKey.Text = item.NameKey;
        _descKey.Text = item.DescriptionKey;
        _suppress = was;
    }

    private string EnglishName(ItemDetails item)
    {
        if (!_chinese && ReferenceEquals(item, CurrentItemOrNull()))
            return _itemName.Text;
        var english = CtisLocale.Get(item.NameKey, false);
        return string.IsNullOrEmpty(english) ? item.DisplayName : english;
    }

    private void WriteNameTranslation(ItemDetails item)
    {
        if (_chinese)
            CtisLocale.SetMessage(item.NameKey, zh: _itemName.Text);
        else
            CtisLocale.SetMessage(item.NameKey, en: _itemName.Text);
        var zh = CtisLocale.Get(item.NameKey, true);
        var en = CtisLocale.Get(item.NameKey, false);
        item.DisplayName = string.IsNullOrEmpty(zh) ? en : zh;
    }

    private void WriteDescriptionTranslation(ItemDetails item)
    {
        if (string.IsNullOrEmpty(item.DescriptionKey)) return;
        if (_chinese)
            CtisLocale.SetMessage(item.DescriptionKey, zh: _itemDesc.Text);
        else
            CtisLocale.SetMessage(item.DescriptionKey, en: _itemDesc.Text);
    }

    private void OnNameFieldsChanged()
    {
        if (_suppress) return;
        WriteNameTranslation(CurrentItem());
        RefreshItemList();
        MarkDirty();
    }

    private void OnNameCommitted()
    {
        if (_suppress) return;
        var item = CurrentItemOrNull();
        if (item == null) return;
        ApplyNameFields(item);
        RefreshItemList();
    }

    private void AddItem()
    {
        ApplyItemForm();
        var nextId = _items.Count > 0 ? _items.Max(i => i.ItemId) + 1 : 1;
        var token = UniqueToken("NEW_ITEM");
        var item = new ItemDetails
        {
            ItemId = nextId,
            NameKey = ItemLocaleKeys.Name(token),
            DescriptionKey = ItemLocaleKeys.Desc(token),
            DisplayName = "New Item",
            Occupancy = ItemOccupancy.Filled(1, 1),
            SlotType = InventorySlotType.Pocket,
            Rarity = ItemRarity.Common,
            MaxStack = 1
        };
        CtisLocale.SetMessage(item.NameKey, "New Item", "新物品");
        _items.Add(item);
        if (!string.IsNullOrEmpty(_itemSearch.Text))
            _itemSearch.Text = "";
        FilterItems(_itemSearch.Text);
        ShowItemAt(_filteredItems.IndexOf(item));
        MarkDirty();
        SetStatus(string.Format(Tr("Added item {0}", "已添加物品 {0}"), nextId));
    }

    private string UniqueToken(string desired, ItemDetails? owner = null)
        => ItemLocaleKeys.UniqueToken(desired, token => TokenTaken(token, owner));

    private bool TokenTaken(string token, ItemDetails? owner)
    {
        var nameKey = ItemLocaleKeys.Name(token);
        var descKey = ItemLocaleKeys.Desc(token);
        foreach (var item in _items)
        {
            if (ReferenceEquals(item, owner)) continue;
            if (item.NameKey == nameKey || item.NameKey == descKey
                || item.DescriptionKey == nameKey || item.DescriptionKey == descKey)
                return true;
        }
        return false;
    }

    private void DeleteItem()
    {
        var item = CurrentItemOrNull();
        if (item == null) return;
        CtisLocale.Remove(item.NameKey);
        CtisLocale.Remove(item.DescriptionKey);
        _items.Remove(item);
        _itemIndex = -1;
        _itemDetails.Visible = false;
        _itemLogo.Visible = true;
        FilterItems(_itemSearch.Text);
        MarkDirty();
        SetStatus(Tr("Item deleted", "已删除物品"));
    }

    #endregion

    #region Equipment Tab

    private Control BuildEquipmentPanel()
    {
        var split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        split.SplitOffsets = new[] { 280 };
        split.AddChild(BuildEquipmentListPane());
        split.AddChild(BuildEquipmentDetailsHost());
        return split;
    }

    private Control BuildEquipmentListPane()
    {
        var pane = new VBoxContainer();
        pane.AddThemeConstantOverride("separation", 0);
        pane.CustomMinimumSize = new Vector2(200, 0);
        var header = ListHeader("Equipment Slots", "装备槽位", out var add, out var remove);
        add.Pressed += AddEquipmentSlot;
        remove.Pressed += DeleteEquipmentSlot;
        pane.AddChild(header);

        _equipmentList = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AllowReselect = true,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        _equipmentList.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontBody);
        _equipmentList.ItemSelected += OnEquipmentSlotSelected;
        pane.AddChild(_equipmentList);
        RefreshEquipmentList();
        return PaneWrap(pane);
    }

    private Control BuildEquipmentDetailsHost()
    {
        var host = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _equipmentPlaceholder = new Label
        {
            Text = "Select an equipment slot to edit properties",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        _equipmentPlaceholder.AddThemeColorOverride("font_color", CtisEditorTheme.Muted);
        _equipmentPlaceholder.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontHeader);
        Loc(_equipmentPlaceholder, "Select an equipment slot to edit properties", "选择一个装备槽位以编辑属性");
        _equipmentPlaceholder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        host.AddChild(_equipmentPlaceholder);

        _equipmentDetails = BuildEquipmentDetails();
        _equipmentDetails.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        host.AddChild(_equipmentDetails);
        _equipmentDetails.Visible = false;
        return host;
    }

    private Control BuildEquipmentDetails()
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        var title = new Label
        {
            Text = "Slot Specification",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        title.AddThemeColorOverride("font_color", CtisEditorTheme.Text);
        title.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontTitle);
        Loc(title, "Slot Specification", "槽位规格与规则");
        header.AddChild(title);
        var headerPanel = new PanelContainer();
        headerPanel.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.SectionBg, marginX: 16, marginY: 12));
        headerPanel.AddChild(header);
        root.AddChild(headerPanel);

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(body);
        root.AddChild(scroll);

        var props = new VBoxContainer();
        props.AddThemeConstantOverride("separation", 8);

        _slotIndexField = Loc(InspectorWidgets.IntField("Slot Index (Unique ID)", 0, 100, 0), "Slot Index (Unique ID)", "槽位唯一索引 (Slot Index)");
        _slotIndexField.Changed += v => { if (CurrentSlotOrNull() is { } slot) { slot.SlotIndex = v; RefreshEquipmentList(); MarkEquipmentDirty(); } };
        props.AddChild(_slotIndexField);

        _slotGroupField = InspectorWidgets.EnumField(EquipmentSlotGroup.Character);
        _slotGroupField.ItemSelected += _ =>
        {
            if (CurrentSlotOrNull() is { } slot)
            {
                slot.Group = InspectorWidgets.ReadEnum<EquipmentSlotGroup>(_slotGroupField);
                RefreshEquipmentList();
                MarkEquipmentDirty();
            }
        };
        var groupRow = InspectorWidgets.Labeled("Slot Group", _slotGroupField, 200);
        LocCaption(groupRow, "Slot Group", "槽位归属分类");
        props.AddChild(groupRow);

        _slotTypeField = InspectorWidgets.EnumField(InventorySlotType.Helmet);
        _slotTypeField.ItemSelected += _ =>
        {
            if (CurrentSlotOrNull() is { } slot)
            {
                slot.SlotType = InspectorWidgets.ReadEnum<InventorySlotType>(_slotTypeField);
                RefreshEquipmentList();
                MarkEquipmentDirty();
            }
        };
        var typeRow = InspectorWidgets.Labeled("Accepted Slot Type", _slotTypeField, 200);
        LocCaption(typeRow, "Accepted Slot Type", "允许装备类型");
        props.AddChild(typeRow);

        var sizeGrid = new GridContainer { Columns = 2 };
        sizeGrid.AddThemeConstantOverride("h_separation", 12);
        sizeGrid.AddThemeConstantOverride("v_separation", 8);
        _slotCellsWField = Loc(InspectorWidgets.IntField("Cells Width", 1, 10, 2), "Cells Width", "槽位宽度 (格子)");
        _slotCellsHField = Loc(InspectorWidgets.IntField("Cells Height", 1, 10, 2), "Cells Height", "槽位高度 (格子)");
        _slotCellsWField.Changed += v => { if (CurrentSlotOrNull() is { } slot) { slot.CellsWidth = v; MarkEquipmentDirty(); } };
        _slotCellsHField.Changed += v => { if (CurrentSlotOrNull() is { } slot) { slot.CellsHeight = v; MarkEquipmentDirty(); } };
        sizeGrid.AddChild(_slotCellsWField);
        sizeGrid.AddChild(_slotCellsHField);
        props.AddChild(sizeGrid);

        _slotTitleKeyField = InspectorWidgets.TextField("");
        _slotTitleKeyField.PlaceholderText = "CTIS_VEST";
        _slotTitleKeyField.TextChanged += text => { if (CurrentSlotOrNull() is { } slot) { slot.TitleKey = text.Trim(); MarkEquipmentDirty(); } };
        var titleRow = InspectorWidgets.Labeled("Title Locale Key", _slotTitleKeyField, 200);
        LocCaption(titleRow, "Title Locale Key", "多语言标题键");
        props.AddChild(titleRow);

        body.AddChild(Loc(InspectorWidgets.Foldout("Properties", true, InspectorWidgets.Padded(props)), "Properties", "属性配置"));
        return root;
    }

    private EquipmentSlotSpec? CurrentSlotOrNull()
    {
        if (_selectedEquipmentIndex < 0 || _selectedEquipmentIndex >= _equipmentSlots.Count)
            return null;
        return _equipmentSlots[_selectedEquipmentIndex];
    }

    private void RefreshEquipmentList()
    {
        if (_equipmentList == null) return;
        _equipmentList.Clear();
        for (int i = 0; i < _equipmentSlots.Count; i++)
        {
            var slot = _equipmentSlots[i];
            var label = $"[#{slot.SlotIndex}] {slot.SlotType} ({slot.Group})";
            _equipmentList.AddItem(label);
        }
        if (_selectedEquipmentIndex >= 0 && _selectedEquipmentIndex < _equipmentSlots.Count)
            _equipmentList.Select(_selectedEquipmentIndex);
    }

    private void OnEquipmentSlotSelected(long index)
    {
        _selectedEquipmentIndex = (int)index;
        var slot = CurrentSlotOrNull();
        if (slot == null)
        {
            _equipmentPlaceholder.Visible = true;
            _equipmentDetails.Visible = false;
            return;
        }

        _equipmentPlaceholder.Visible = false;
        _equipmentDetails.Visible = true;

        _slotIndexField.SetValueWithoutNotify(slot.SlotIndex);
        InspectorWidgets.SelectEnum(_slotGroupField, slot.Group);
        InspectorWidgets.SelectEnum(_slotTypeField, slot.SlotType);
        _slotCellsWField.SetValueWithoutNotify(slot.CellsWidth);
        _slotCellsHField.SetValueWithoutNotify(slot.CellsHeight);
        _slotTitleKeyField.Text = slot.TitleKey;
    }

    private void AddEquipmentSlot()
    {
        int nextIndex = _equipmentSlots.Count > 0 ? _equipmentSlots.Max(s => s.SlotIndex) + 1 : 0;
        var newSlot = new EquipmentSlotSpec
        {
            SlotIndex = nextIndex,
            SlotType = InventorySlotType.Pocket,
            Group = EquipmentSlotGroup.Container,
            CellsWidth = 2,
            CellsHeight = 2,
            TitleKey = ""
        };
        _equipmentSlots.Add(newSlot);
        RefreshEquipmentList();
        _equipmentList.Select(_equipmentSlots.Count - 1);
        OnEquipmentSlotSelected(_equipmentSlots.Count - 1);
        MarkEquipmentDirty();
        SetStatus(Tr($"Added new slot #{nextIndex}", $"已添加新槽位 #{nextIndex}"));
    }

    private void DeleteEquipmentSlot()
    {
        var slot = CurrentSlotOrNull();
        if (slot == null) return;
        int idx = _selectedEquipmentIndex;
        _equipmentSlots.RemoveAt(idx);
        _selectedEquipmentIndex = Math.Min(idx, _equipmentSlots.Count - 1);
        RefreshEquipmentList();
        if (_selectedEquipmentIndex >= 0)
            OnEquipmentSlotSelected(_selectedEquipmentIndex);
        else
        {
            _equipmentPlaceholder.Visible = true;
            _equipmentDetails.Visible = false;
        }
        MarkEquipmentDirty();
        SetStatus(Tr("Slot deleted", "已删除槽位"));
    }

    #endregion

    #region Config

    private Control BuildConfigPanel()
    {
        var scroll = new ScrollContainer();
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 16);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(InspectorWidgets.Padded(body, 16));

        var title = new Label { Text = "Configuration", AutoTranslateMode = AutoTranslateModeEnum.Disabled };
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontHeader);
        Loc(title, "Configuration", "配置");
        var titlePanel = new PanelContainer();
        var titleStyle = CtisEditorTheme.Flat(CtisEditorTheme.SectionBg, radius: 8, marginX: 20, marginY: 16);
        titleStyle.BorderWidthLeft = 4;
        titleStyle.BorderColor = CtisEditorTheme.Accent;
        titlePanel.AddThemeStyleboxOverride("panel", titleStyle);
        titlePanel.AddChild(title);
        body.AddChild(titlePanel);

        var rules = new VBoxContainer();
        rules.AddThemeConstantOverride("separation", 8);
        _blockSelf = AddRuleToggle(rules, "Block Self Owned Container", "阻止放入自身容器", _config.BlockSelfOwnedContainer, v => _config.BlockSelfOwnedContainer = v);
        _blockBounds = AddRuleToggle(rules, "Block Out Of Bounds", "阻止越界", _config.BlockOutOfBounds, v => _config.BlockOutOfBounds = v);
        _blockOccupied = AddRuleToggle(rules, "Block Slot Occupied", "阻止占用格", _config.BlockSlotOccupied, v => _config.BlockSlotOccupied = v);
        _blockMismatch = AddRuleToggle(rules, "Block Slot Type Mismatch", "阻止槽位类型不匹配", _config.BlockSlotTypeMismatch, v => _config.BlockSlotTypeMismatch = v);
        body.AddChild(Loc(InspectorWidgets.Foldout("Rules", true, InspectorWidgets.Padded(rules)), "Rules", "规则"));

        var colors = new VBoxContainer();
        colors.AddThemeConstantOverride("separation", 8);
        _overridePalette = InspectorWidgets.Check(_config.OverrideHighlightPalette);
        _overridePalette.Toggled += v => { _config.OverrideHighlightPalette = v; MarkConfigDirty(); };
        var overrideRow = InspectorWidgets.Labeled("Override Highlight Palette", _overridePalette, 220, expandField: false);
        LocCaption(overrideRow, "Override Highlight Palette", "覆盖高亮色板");
        colors.AddChild(overrideRow);

        var palette = new VBoxContainer();
        palette.AddThemeConstantOverride("separation", 8);
        var paletteFrame = new PanelContainer();
        paletteFrame.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.RootBg, CtisEditorTheme.BorderStrong, 8, 16, 16));
        paletteFrame.AddChild(palette);
        var p = _config.HighlightPalette;
        _colorValid = AddColorRow(palette, "Valid Empty", "可放置空位", p.ValidEmpty, c => UpdatePalette(pal => pal with { ValidEmpty = c }));
        _colorInvalid = AddColorRow(palette, "Invalid", "无效", p.Invalid, c => UpdatePalette(pal => pal with { Invalid = c }));
        _colorStack = AddColorRow(palette, "Can Stack", "可堆叠", p.CanStack, c => UpdatePalette(pal => pal with { CanStack = c }));
        _colorExchange = AddColorRow(palette, "Can Quick Exchange", "可快速交换", p.CanQuickExchange, c => UpdatePalette(pal => pal with { CanQuickExchange = c }));
        colors.AddChild(paletteFrame);

        var reasonTitle = new Label { Text = "Invalid Reason Colors", AutoTranslateMode = AutoTranslateModeEnum.Disabled };
        reasonTitle.AddThemeColorOverride("font_color", CtisEditorTheme.Accent);
        reasonTitle.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontTitle);
        Loc(reasonTitle, "Invalid Reason Colors", "无效原因颜色");
        colors.AddChild(reasonTitle);

        var reasonBox = new VBoxContainer();
        reasonBox.AddThemeConstantOverride("separation", 8);
        var sizeRow = new HBoxContainer();
        sizeRow.AddThemeConstantOverride("separation", 8);
        _invalidSize = Loc(InspectorWidgets.IntField("Size", 0, 16, _config.InvalidReasonColors.Count), "Size", "数量");
        _invalidSize.Changed += ResizeInvalidReasons;
        var addOverride = InspectorWidgets.IconButton("+", CtisEditorTheme.Add, CtisEditorTheme.AddHover);
        addOverride.Pressed += () => ResizeInvalidReasons(_config.InvalidReasonColors.Count + 1);
        sizeRow.AddChild(_invalidSize);
        sizeRow.AddChild(addOverride);
        reasonBox.AddChild(sizeRow);
        _invalidRows = new VBoxContainer();
        _invalidRows.AddThemeConstantOverride("separation", 4);
        reasonBox.AddChild(_invalidRows);
        var reasonFrame = new PanelContainer();
        reasonFrame.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.RootBg, CtisEditorTheme.BorderStrong, 8, 12, 12));
        reasonFrame.AddChild(reasonBox);
        colors.AddChild(reasonFrame);
        body.AddChild(Loc(InspectorWidgets.Foldout("Highlight Colors", true, InspectorWidgets.Padded(colors)), "Highlight Colors", "高亮颜色"));

        var rarityBox = new VBoxContainer();
        rarityBox.AddThemeConstantOverride("separation", 8);
        _config.EnsureRarityColors();
        foreach (var rarity in Enum.GetValues<ItemRarity>())
        {
            var current = rarity;
            AddColorRow(rarityBox, current.ToString(), RarityCaption(current), _config.GetRarityColor(current), color =>
            {
                _config.SetRarityColor(current, color);
                RefreshItemShapePreview();
                MarkConfigDirty();
            });
        }
        body.AddChild(Loc(InspectorWidgets.Foldout("Rarity Colors", true, InspectorWidgets.Padded(rarityBox)), "Rarity Colors", "稀有度颜色"));

        RebuildInvalidRows();
        return scroll;
    }

    private static string RarityCaption(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "普通",
        ItemRarity.Uncommon => "优秀",
        ItemRarity.Rare => "稀有",
        ItemRarity.Epic => "史诗",
        ItemRarity.Legendary => "传说",
        ItemRarity.Artifact => "神器",
        _ => rarity.ToString()
    };

    private CheckBox AddRuleToggle(Control parent, string en, string zh, bool value, Action<bool> setter)
    {
        var box = InspectorWidgets.Check(value);
        box.Toggled += v => { setter(v); MarkConfigDirty(); };
        var row = InspectorWidgets.Labeled(en, box, 280, expandField: false);
        LocCaption(row, en, zh);
        parent.AddChild(row);
        return box;
    }

    private ColorPickerButton AddColorRow(Control parent, string en, string zh, Rgba value, Action<Rgba> setter)
    {
        var button = InspectorWidgets.ColorField(value);
        button.ColorChanged += color => { setter(InspectorWidgets.ToRgba(color)); MarkConfigDirty(); };
        var row = InspectorWidgets.Labeled(en, button, 180);
        LocCaption(row, en, zh);
        parent.AddChild(row);
        return button;
    }

    private void UpdatePalette(Func<InventoryHighlightPalette, InventoryHighlightPalette> update)
    {
        _config.HighlightPalette = update(_config.HighlightPalette);
        MarkConfigDirty();
    }

    private void ResizeInvalidReasons(int size)
    {
        size = Math.Clamp(size, 0, 16);
        while (_config.InvalidReasonColors.Count < size)
        {
            _config.InvalidReasonColors.Add(new InventoryPlacementBlockColorOverride
            {
                Reason = InventoryPlacementBlockReason.SelfOwnedContainer,
                Color = CtisSettings.HighlightInvalid
            });
        }
        while (_config.InvalidReasonColors.Count > size)
            _config.InvalidReasonColors.RemoveAt(_config.InvalidReasonColors.Count - 1);
        _invalidSize.SetValueWithoutNotify(_config.InvalidReasonColors.Count);
        RebuildInvalidRows();
        MarkConfigDirty();
    }

    private void RebuildInvalidRows()
    {
        foreach (var child in _invalidRows.GetChildren())
            child.QueueFree();
        for (int i = 0; i < _config.InvalidReasonColors.Count; i++)
        {
            int index = i;
            var entry = _config.InvalidReasonColors[i];
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var reason = InspectorWidgets.EnumField(entry.Reason);
            reason.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            reason.CustomMinimumSize = new Vector2(240, InspectorWidgets.SwatchHeight);
            reason.ItemSelected += _ =>
            {
                _config.InvalidReasonColors[index].Reason = InspectorWidgets.ReadEnum<InventoryPlacementBlockReason>(reason);
                MarkConfigDirty();
            };
            var color = InspectorWidgets.ColorField(entry.Color);
            color.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            color.ColorChanged += c =>
            {
                _config.InvalidReasonColors[index].Color = InspectorWidgets.ToRgba(c);
                MarkConfigDirty();
            };
            row.AddChild(reason);
            row.AddChild(color);
            var del = InspectorWidgets.IconButton("×", CtisEditorTheme.Delete, CtisEditorTheme.DeleteHover);
            del.Pressed += () => RemoveInvalidReason(index);
            row.AddChild(del);
            _invalidRows.AddChild(row);
        }
    }

    private void RemoveInvalidReason(int index)
    {
        if ((uint)index >= (uint)_config.InvalidReasonColors.Count) return;
        _config.InvalidReasonColors.RemoveAt(index);
        _invalidSize.SetValueWithoutNotify(_config.InvalidReasonColors.Count);
        RebuildInvalidRows();
        MarkConfigDirty();
    }

    #endregion

    private static PanelContainer PaneWrap(Control child)
    {
        var pane = new PanelContainer();
        pane.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.PaneBg));
        pane.AddChild(child);
        return pane;
    }

    private Control ListHeader(string en, string zh, out Button add, out Button remove)
    {
        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.SectionBg, marginX: 12, marginY: 10));
        var row = new HBoxContainer();
        var label = new Label
        {
            Text = en,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        label.AddThemeColorOverride("font_color", CtisEditorTheme.Text);
        label.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontTitle);
        Loc(label, en, zh);
        add = InspectorWidgets.IconButton("+", CtisEditorTheme.Add, CtisEditorTheme.AddHover);
        remove = InspectorWidgets.IconButton("−", CtisEditorTheme.Delete, CtisEditorTheme.DeleteHover);
        row.AddChild(label);
        row.AddChild(add);
        row.AddChild(remove);
        header.AddChild(row);
        return header;
    }

    private static Control CreateLogo()
    {
        var wrap = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var logo = new TextureRect
        {
            Texture = CtisArt.Load(CtisArt.EditorLogo),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(420, 420),
            Modulate = new Color(1, 1, 1, 0.45f),
            TextureFilter = TextureFilterEnum.Nearest
        };
        wrap.AddChild(logo);
        return wrap;
    }

    #region Settings Tab & Toolbox

    private Control BuildSettingsPanel()
    {
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 16);
        scroll.AddChild(body);

        // Section 1: Grid & Inventory Dimensions
        var gridProps = new GridContainer { Columns = 2 };
        gridProps.AddThemeConstantOverride("h_separation", 16);
        gridProps.AddThemeConstantOverride("v_separation", 10);

        _gridTileW = Loc(InspectorWidgets.FloatField("Tile Width", 16, 64, 32), "Tile Width", "单元格宽度 (px)");
        _gridTileH = Loc(InspectorWidgets.FloatField("Tile Height", 16, 64, 32), "Tile Height", "单元格高度 (px)");
        _depositoryCols = Loc(InspectorWidgets.IntField("Depository Cols", 1, 40, 10), "Depository Cols", "主仓库列数");
        _depositoryRows = Loc(InspectorWidgets.IntField("Depository Rows", 1, 80, 24), "Depository Rows", "主仓库行数");

        _gridTileW.Changed += _ => MarkSettingsDirty();
        _gridTileH.Changed += _ => MarkSettingsDirty();
        _depositoryCols.Changed += _ => MarkSettingsDirty();
        _depositoryRows.Changed += _ => MarkSettingsDirty();

        gridProps.AddChild(_gridTileW);
        gridProps.AddChild(_gridTileH);
        gridProps.AddChild(_depositoryCols);
        gridProps.AddChild(_depositoryRows);
        body.AddChild(Loc(InspectorWidgets.Foldout("Grid & Inventory Settings", true, InspectorWidgets.Padded(gridProps)), "Grid & Inventory Settings", "网格与仓库参数"));

        // Section 2: Interaction & Input Settings
        var inputProps = new VBoxContainer();
        inputProps.AddThemeConstantOverride("separation", 10);

        _longPressDuration = Loc(InspectorWidgets.FloatField("Long Press (s)", 0.1f, 2.0f, 0.38f), "Long Press (s)", "长按判定时间 (秒)");
        _longPressDuration.Changed += _ => MarkSettingsDirty();
        inputProps.AddChild(_longPressDuration);

        _defaultMobileMode = InspectorWidgets.Check(false);
        _defaultMobileMode.Toggled += _ => MarkSettingsDirty();
        var mobileRow = InspectorWidgets.Labeled("Mobile Mode Default", _defaultMobileMode, 280, expandField: false);
        LocCaption(mobileRow, "Mobile Mode Default", "默认启用移动端控制模式");
        inputProps.AddChild(mobileRow);

        body.AddChild(Loc(InspectorWidgets.Foldout("Interaction & Controls", true, InspectorWidgets.Padded(inputProps)), "Interaction & Controls", "交互与控制配置"));

        // Section 3: Data Paths
        var dataBox = new VBoxContainer();
        dataBox.AddThemeConstantOverride("separation", 10);
        dataBox.AddChild(BuildPathRow(out _pathCatalog, "Item Catalog", "物品数据", "*.json", _ => MarkSettingsDirty()));
        dataBox.AddChild(BuildPathRow(out _pathEquipment, "Equipment Layout", "装备槽位", "*.json", _ => MarkSettingsDirty()));
        dataBox.AddChild(BuildPathRow(out _pathPlacement, "Placement Config", "摆放规则", "*.json", _ => MarkSettingsDirty()));
        dataBox.AddChild(BuildPathRow(out _pathLocale, "Locale CSV", "多语言表", "*.csv", _ => MarkSettingsDirty()));
        body.AddChild(Loc(InspectorWidgets.Foldout("Data Paths", true, InspectorWidgets.Padded(dataBox)), "Data Paths", "数据文件路径"));

        // Section 4: UI & Preset Scenes
        var uiBox = new VBoxContainer();
        uiBox.AddThemeConstantOverride("separation", 10);
        uiBox.AddChild(BuildPathRow(out _pathMenuTheme, "Menu Theme", "菜单主题", "*.tres", _ => MarkSettingsDirty()));
        uiBox.AddChild(BuildPathRow(out _pathPocketScene, "Pocket Scene", "默认口袋场景", "*.tscn", _ => MarkSettingsDirty()));
        uiBox.AddChild(BuildPathRow(out _pathCofferScene, "Coffer Scene", "默认保险箱场景", "*.tscn", _ => MarkSettingsDirty()));
        body.AddChild(Loc(InspectorWidgets.Foldout("UI & Preset Scenes", true, InspectorWidgets.Padded(uiBox)), "UI & Preset Scenes", "UI 与预设场景"));

        // Section 5: Toolbox (Horizontal layout with unified styling)
        var toolsRow = new HBoxContainer();
        toolsRow.AddThemeConstantOverride("separation", 12);

        var scanBtn = InspectorWidgets.ActionButton("Auto-Scan & Self-Healing", CtisEditorTheme.FieldBg, CtisEditorTheme.FieldBg.Lightened(0.12f), 4, 34);
        Loc(scanBtn, "Auto-Scan & Self-Healing", "自动扫描并修复失效路径");
        scanBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scanBtn.Pressed += AutoScanAndHealPaths;
        toolsRow.AddChild(scanBtn);

        var openSaveDirBtn = InspectorWidgets.ActionButton("Open Save Directory", CtisEditorTheme.FieldBg, CtisEditorTheme.FieldBg.Lightened(0.12f), 4, 34);
        Loc(openSaveDirBtn, "Open Save Directory", "打开用户存档目录 (user://)");
        openSaveDirBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        openSaveDirBtn.Pressed += () =>
        {
            var global = ProjectSettings.GlobalizePath("user://");
            if (!System.IO.Directory.Exists(global))
                System.IO.Directory.CreateDirectory(global);
            OS.ShellOpen(global);
            SetStatus(Tr($"Opened save directory: {global}", $"已打开本地存档目录: {global}"));
        };
        toolsRow.AddChild(openSaveDirBtn);

        body.AddChild(Loc(InspectorWidgets.Foldout("Toolbox", true, InspectorWidgets.Padded(toolsRow)), "Toolbox", "工具箱"));

        return scroll;
    }

    private void LoadSettingsValues()
    {
        if (_pathCatalog == null) return;
        _pathCatalog.Text = (string)ProjectSettings.GetSetting("ctis/item_catalog", "");
        _pathEquipment.Text = (string)ProjectSettings.GetSetting("ctis/equipment_layout", "");
        _pathPlacement.Text = (string)ProjectSettings.GetSetting("ctis/placement_config", "");
        _pathLocale.Text = (string)ProjectSettings.GetSetting("ctis/locale", "");
        _pathMenuTheme.Text = (string)ProjectSettings.GetSetting("ctis/menu_theme", CtisMenuStyles.BuiltinMenuThemePath);
        _pathPocketScene.Text = (string)ProjectSettings.GetSetting("ctis/scenes/pocket", CtisRuntime.BuiltinPocketScenePath);
        _pathCofferScene.Text = (string)ProjectSettings.GetSetting("ctis/scenes/coffer", CtisRuntime.BuiltinCofferScenePath);

        _gridTileW.SetValueWithoutNotify((float)ProjectSettings.GetSetting("ctis/grid/tile_size_width", 32f));
        _gridTileH.SetValueWithoutNotify((float)ProjectSettings.GetSetting("ctis/grid/tile_size_height", 32f));
        _depositoryCols.SetValueWithoutNotify((int)ProjectSettings.GetSetting("ctis/grid/depository_columns", 10));
        _depositoryRows.SetValueWithoutNotify((int)ProjectSettings.GetSetting("ctis/grid/depository_rows", 24));
        _longPressDuration.SetValueWithoutNotify((float)ProjectSettings.GetSetting("ctis/input/long_press_duration", 0.38f));
        _defaultMobileMode.ButtonPressed = (bool)ProjectSettings.GetSetting("ctis/input/mobile_mode_default", false);
    }

    private void AutoScanAndHealPaths()
    {
        int healed = 0;
        int checkedCount = 0;
        int missingCount = 0;

        // 1. Data file paths
        CheckAndHealDataPath(ref _pathCatalog, "ItemCatalog.json", ref healed, ref checkedCount, ref missingCount);
        CheckAndHealDataPath(ref _pathEquipment, "EquipmentLayout.json", ref healed, ref checkedCount, ref missingCount);
        CheckAndHealDataPath(ref _pathPlacement, "PlacementConfig.json", ref healed, ref checkedCount, ref missingCount);
        CheckAndHealDataPath(ref _pathLocale, "ctis.csv", ref healed, ref checkedCount, ref missingCount);

        // 2. UI & Preset scenes
        CheckAndHealScenePath(ref _pathMenuTheme, "ctis_menu_theme.tres", CtisMenuStyles.BuiltinMenuThemePath, ref healed, ref checkedCount, ref missingCount);
        CheckAndHealScenePath(ref _pathPocketScene, "GP_Pocket.tscn", CtisRuntime.BuiltinPocketScenePath, ref healed, ref checkedCount, ref missingCount);
        CheckAndHealScenePath(ref _pathCofferScene, "GP_Coffer.tscn", CtisRuntime.BuiltinCofferScenePath, ref healed, ref checkedCount, ref missingCount);

        // 3. GridPanelSceneKey in items
        foreach (var item in _items)
        {
            if (string.IsNullOrEmpty(item.GridPanelSceneKey)) continue;
            checkedCount++;
            if (!Godot.FileAccess.FileExists(item.GridPanelSceneKey) && !ResourceLoader.Exists(item.GridPanelSceneKey))
            {
                var resolved = CtisRuntime.ResolveGridPanelScene(item.GridPanelSceneKey);
                if (Godot.FileAccess.FileExists(resolved) || ResourceLoader.Exists(resolved))
                {
                    item.GridPanelSceneKey = resolved;
                    healed++;
                    MarkItemsDirty();
                }
                else
                {
                    var filename = System.IO.Path.GetFileName(item.GridPanelSceneKey);
                    var candidate = FindFirstFile(filename);
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        item.GridPanelSceneKey = candidate;
                        healed++;
                        MarkItemsDirty();
                    }
                    else
                    {
                        missingCount++;
                    }
                }
            }
        }

        if (healed > 0)
        {
            _settingsDirty = true;
            SaveAll();
            if (CurrentItemOrNull() != null)
                _gridScene.Text = CurrentItem().GridPanelSceneKey;
            SetStatus(Tr($"Auto-scan completed: {healed} paths repaired", $"自动修复完成：已成功修复 {healed} 处失效路径并保存"));
        }
        else if (missingCount > 0)
        {
            SetStatus(Tr($"Auto-scan completed: {missingCount} files missing in project", $"自动扫描完成：发现 {missingCount} 处文件在项目中缺失"));
        }
        else
        {
            SetStatus(Tr($"All paths are healthy ({checkedCount} resources verified)", $"所有路径状态良好（已校验全部 {checkedCount} 项资源）"));
        }
    }

    private static void CheckAndHealDataPath(ref LineEdit edit, string fileName, ref int healed, ref int checkedCount, ref int missingCount)
    {
        checkedCount++;
        var current = edit.Text.Trim();
        if (!string.IsNullOrEmpty(current) && Godot.FileAccess.FileExists(current))
            return;

        var candidate = FindFirstFile(fileName);
        if (!string.IsNullOrEmpty(candidate))
        {
            edit.Text = candidate;
            healed++;
        }
        else
        {
            missingCount++;
        }
    }

    private static void CheckAndHealScenePath(ref LineEdit edit, string fileName, string builtinFallback, ref int healed, ref int checkedCount, ref int missingCount)
    {
        checkedCount++;
        var current = edit.Text.Trim();
        if (!string.IsNullOrEmpty(current) && (Godot.FileAccess.FileExists(current) || ResourceLoader.Exists(current)))
            return;

        var candidate = FindFirstFile(fileName);
        if (!string.IsNullOrEmpty(candidate))
        {
            edit.Text = candidate;
            healed++;
        }
        else if (Godot.FileAccess.FileExists(builtinFallback) || ResourceLoader.Exists(builtinFallback))
        {
            edit.Text = builtinFallback;
            healed++;
        }
        else
        {
            missingCount++;
        }
    }

    private static string FindFirstFile(string targetFileName)
    {
        var root = ProjectSettings.GlobalizePath("res://");
        try
        {
            var files = System.IO.Directory.GetFiles(root, targetFileName, System.IO.SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                var rel = System.IO.Path.GetRelativePath(root, files[0]).Replace('\\', '/');
                return "res://" + rel;
            }
        }
        catch
        {
            // Ignore IO errors during search
        }
        return string.Empty;
    }

    #endregion
}
#endif

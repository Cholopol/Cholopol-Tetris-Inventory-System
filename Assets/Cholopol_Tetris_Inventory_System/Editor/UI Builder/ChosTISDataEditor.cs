/*
 * Copyright 2026 Cholopol
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Cholopol.TIS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.UIElements;
using UnityEngine;
using Cholopol.TIS.Debug;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

public class ChosTISDataEditor : EditorWindow
{
    // Data Sources
    private ItemDataList_SO itemDatabase;
    private TetrisItemPointSet_SO shapeDatabase;
    private InventoryPlacementConfig_SO configDatabase;
    private SerializedObject serializedConfig;
    private CTISPrefabConfig prefabConfig;
    
    // Items Tab
    private List<ItemDetails> itemList = new List<ItemDetails>();
    private List<ItemDetails> filteredItemList = new List<ItemDetails>();
    private ItemDetails activeItem;
    private ListView itemListView;
    private ScrollView itemDetailsPane;
    private VisualElement itemIconPreview;
    private Sprite defaultIcon;
    private TextField itemSearchField;
    
    // Shapes Tab
    private List<PointSet> shapeList = new List<PointSet>();
    private PointSet activeShape;
    private ListView shapeListView;
    private VisualElement shapeDetailsPane;
    private VisualElement shapeGridPreview;
    private ScrollView pointsListContainer;
    
    // Config Tab
    private VisualElement configPanel;
    private ListView invalidReasonColorsList;

    // Localization
    private PopupField<string> languageSelector;
    private int selectedLanguageIndex = 0;
    private List<Locale> availableLocales = new List<Locale>();
    private StringTableCollection itemStringTable;
    private const string StringTableName = "ItemStrings";
    
    // UI Elements
    private Button itemsTabButton;
    private Button shapesTabButton;
    private Button configTabButton;
    private VisualElement itemsPanel;
    private VisualElement shapesPanel;
    private Label statusText;
    
    private VisualElement itemsLogoContainer;
    private VisualElement shapesLogoContainer;
    private Texture2D logoTexture;
    
    [MenuItem("CTIS/Data Editor")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<ChosTISDataEditor>();
        wnd.titleContent = new GUIContent("Cholopol TIS Editor");
        wnd.minSize = new Vector2(900, 600);
    }
    
    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedo;
        LoadAvailableLocales();
        LoadOrCreateStringTable();
    }
    
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        if (prefabConfig != null)
        {
            if (prefabConfig.defaultItemIcon != null && prefabConfig.defaultItemIcon.IsValid())
                prefabConfig.defaultItemIcon.ReleaseAsset();
            if (prefabConfig.editorLogoSprite != null && prefabConfig.editorLogoSprite.IsValid())
                prefabConfig.editorLogoSprite.ReleaseAsset();
        }
    }

    private void OnInspectorUpdate()
    {
        if (serializedConfig != null && serializedConfig.targetObject != null)
        {
            serializedConfig.Update();
        }
    }
    
    private void OnUndoRedo()
    {
        if (activeItem != null) RefreshItemDetails();
        if (activeShape != null) RefreshShapeDetails();
        itemListView?.Rebuild();
        shapeListView?.Rebuild();
    }

    public void CreateGUI()
    {
        // 动态查找 UXML
        var guids = AssetDatabase.FindAssets("ChosTISDataEditor t:VisualTreeAsset");
        if (guids.Length == 0)
        {
            GameDebug.Log(DebugChannel.UIOverlay, DebugLevel.Error, "Missing ChosTISDataEditor.uxml");
            return;
        }
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        visualTree.CloneTree(rootVisualElement);
        
        var configGuids = AssetDatabase.FindAssets("t:CTISPrefabConfig");
        if (configGuids.Length > 0)
        {
            prefabConfig = AssetDatabase.LoadAssetAtPath<CTISPrefabConfig>(AssetDatabase.GUIDToAssetPath(configGuids[0]));
            if (prefabConfig != null)
            {
                if (prefabConfig.defaultItemIcon != null && prefabConfig.defaultItemIcon.RuntimeKeyIsValid())
                {
                    var iconHandle = prefabConfig.defaultItemIcon.LoadAssetAsync<Sprite>();
                    defaultIcon = iconHandle.WaitForCompletion();
                }
                
                if (prefabConfig.editorLogoSprite != null && prefabConfig.editorLogoSprite.RuntimeKeyIsValid())
                {
                    var logoHandle = prefabConfig.editorLogoSprite.LoadAssetAsync<Sprite>();
                    var logoSprite = logoHandle.WaitForCompletion();
                    logoTexture = logoSprite != null ? logoSprite.texture : null;
                }
            }
        }
        
        // Get UI references
        itemsTabButton = rootVisualElement.Q<Button>("ItemsTab");
        shapesTabButton = rootVisualElement.Q<Button>("ShapesTab");
        configTabButton = rootVisualElement.Q<Button>("ConfigTab");
        itemsPanel = rootVisualElement.Q<VisualElement>("ItemsPanel");
        shapesPanel = rootVisualElement.Q<VisualElement>("ShapesPanel");
        configPanel = rootVisualElement.Q<VisualElement>("ConfigPanel");
        statusText = rootVisualElement.Q<Label>("StatusText");
        
        // Tab switching
        itemsTabButton.clicked += () => SwitchTab(0);
        shapesTabButton.clicked += () => SwitchTab(1);
        configTabButton.clicked += () => SwitchTab(2);
        
        // Save Button
        var saveBtn = rootVisualElement.Q<Button>("SaveAllBtn");
        if (saveBtn != null) saveBtn.clicked += OnSaveAll;
        
        // Initialize panels
        InitializeItemsPanel();
        InitializeShapesPanel();
        
        // Load data
        LoadDatabases();
        
        // Initialize config panel after data load
        InitializeConfigPanel();
        
        UpdateStatus("Ready");
    }
    
    private void SwitchTab(int tabIndex)
    {
        itemsPanel.style.display = tabIndex == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        shapesPanel.style.display = tabIndex == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        configPanel.style.display = tabIndex == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        
        itemsTabButton.RemoveFromClassList("tab-active");
        shapesTabButton.RemoveFromClassList("tab-active");
        configTabButton.RemoveFromClassList("tab-active");
        
        if (tabIndex == 0) itemsTabButton.AddToClassList("tab-active");
        else if (tabIndex == 1) shapesTabButton.AddToClassList("tab-active");
        else configTabButton.AddToClassList("tab-active");
    }

    private VisualElement CreateLogoContainer()
    {
        var container = new VisualElement();
        container.style.flexGrow = 1;
        container.style.justifyContent = Justify.Center;
        container.style.alignItems = Align.Center;

        if (logoTexture != null)
        {
            var logo = new Image();
            logo.image = logoTexture;
            logo.style.width = 500;
            logo.style.height = 500;
            logo.style.opacity = 0.5f;
            logo.scaleMode = ScaleMode.ScaleToFit;
            container.Add(logo);
        }
        return container;
    }
    
    #region Items Panel
    private void InitializeItemsPanel()
    {
        itemListView = rootVisualElement.Q<ListView>("ItemListView");
        itemDetailsPane = rootVisualElement.Q<ScrollView>("ItemDetailsPane");
        
        itemsLogoContainer = CreateLogoContainer();
        if (itemDetailsPane != null && itemDetailsPane.parent != null)
        {
            var splitView = itemDetailsPane.parent;
            var rightPaneContainer = new VisualElement();
            rightPaneContainer.style.flexGrow = 1;
            
            int index = splitView.IndexOf(itemDetailsPane);
            if (index >= 0)
            {
                splitView.Remove(itemDetailsPane);
                splitView.Insert(index, rightPaneContainer);
                rightPaneContainer.Add(itemDetailsPane);
                rightPaneContainer.Add(itemsLogoContainer);
                
                itemDetailsPane.style.flexGrow = 1;
            }
        }

        itemIconPreview = rootVisualElement.Q<VisualElement>("ItemIconPreview");
        itemSearchField = rootVisualElement.Q<TextField>("ItemSearchField");
        
        if (itemSearchField != null)
        {
            itemSearchField.RegisterValueChangedCallback(evt => FilterItemList(evt.newValue));
        }

        rootVisualElement.Q<Button>("AddItemBtn").clicked += OnAddItem;
        rootVisualElement.Q<Button>("DeleteItemBtn").clicked += OnDeleteItem;
        
        CreateLanguageSelector();
        SetupItemListView();
        BindItemFields();
        
        itemDetailsPane.style.display = DisplayStyle.None;
        if (itemsLogoContainer != null) itemsLogoContainer.style.display = DisplayStyle.Flex;
    }

    private void FilterItemList(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            filteredItemList = new List<ItemDetails>(itemList);
        }
        else
        {
            searchText = searchText.ToLower();
            filteredItemList = itemList.Where(item => 
                item.itemID.ToString().Contains(searchText) || 
                GetLocalizedStringValue(item.localizedName).ToLower().Contains(searchText)
            ).ToList();
        }
        
        itemListView.itemsSource = filteredItemList;
        itemListView.Rebuild();
        
        if (activeItem != null && !filteredItemList.Contains(activeItem))
        {
            itemListView.ClearSelection();
            activeItem = null;
            itemDetailsPane.style.display = DisplayStyle.None;
        }
        else if (activeItem != null)
        {
            itemListView.selectedIndex = filteredItemList.IndexOf(activeItem);
        }
    }
    
    private void CreateLanguageSelector()
    {
        var container = rootVisualElement.Q<VisualElement>("LanguageContainer");
        if (container == null) return;
        
        var languageNames = availableLocales.Select(l => l.LocaleName).ToList();
        if (languageNames.Count == 0) languageNames.Add("No Locales");
        
        languageSelector = new PopupField<string>("Language", languageNames, 0);
        languageSelector.style.minWidth = 220;
        languageSelector.style.maxWidth = 300;
        languageSelector.labelElement.style.minWidth = 60;
        languageSelector.RegisterValueChangedCallback(evt =>
        {
            selectedLanguageIndex = languageNames.IndexOf(evt.newValue);
            itemListView.Rebuild();
            if (activeItem != null) RefreshItemDetails();
        });
        container.Add(languageSelector);
    }
    
    private void SetupItemListView()
    {
        itemListView.selectionType = SelectionType.Single;
        itemListView.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
        
        itemListView.makeItem = () =>
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            
            var icon = new VisualElement();
            icon.name = "RowIcon";
            icon.style.width = 24;
            icon.style.height = 24;
            icon.style.minWidth = 24;
            icon.style.minHeight = 24;
            icon.style.marginRight = 8;
            icon.style.borderTopLeftRadius = 3;
            icon.style.borderTopRightRadius = 3;
            icon.style.borderBottomLeftRadius = 3;
            icon.style.borderBottomRightRadius = 3;
            row.Add(icon);
            
            var label = new Label();
            label.name = "RowLabel";
            label.style.flexGrow = 1;
            label.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(label);
            
            return row;
        };
        
        itemListView.bindItem = (e, i) =>
        {
            if (i >= filteredItemList.Count) return;
            var item = filteredItemList[i];
            var icon = e.Q<VisualElement>("RowIcon");
            var label = e.Q<Label>("RowLabel");
            
            if (item.itemIcon != null)
            {
                icon.style.backgroundImage = new StyleBackground(Background.FromSprite(item.itemIcon));
            }
            else if (defaultIcon != null)
            {
                icon.style.backgroundImage = new StyleBackground(Background.FromSprite(defaultIcon));
            }
            else
            {
                icon.style.backgroundImage = StyleKeyword.None;
            }
            
            string name = GetLocalizedStringValue(item.localizedName);
            label.text = !string.IsNullOrEmpty(name) ? name : $"Item {item.itemID}";
        };
        
        itemListView.selectionChanged += OnItemSelectionChanged;
    }
    
    private void BindItemFields()
    {
        var idField = itemDetailsPane.Q<IntegerField>("ItemID");
        idField?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Item ID");
            activeItem.itemID = evt.newValue;
            EditorUtility.SetDirty(itemDatabase);
        });
        
        var nameField = itemDetailsPane.Q<TextField>("ItemName");
        
        var nameFieldParent = nameField?.parent;
        if (nameFieldParent != null)
        {
            var applyAllBtn = new Button(() => ApplyNameToAllLanguages());
            applyAllBtn.text = "All";
            applyAllBtn.tooltip = "Apply this name to all languages";
            applyAllBtn.style.width = 36;
            applyAllBtn.style.height = 18;
            applyAllBtn.style.marginLeft = 4;
            applyAllBtn.style.backgroundColor = new StyleColor(new Color(0.3f, 0.5f, 0.7f));
            applyAllBtn.style.color = Color.white;
            applyAllBtn.style.borderTopLeftRadius = 3;
            applyAllBtn.style.borderTopRightRadius = 3;
            applyAllBtn.style.borderBottomLeftRadius = 3;
            applyAllBtn.style.borderBottomRightRadius = 3;
            applyAllBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            applyAllBtn.style.fontSize = 9;
            
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexShrink = 0;
            
            int index = nameFieldParent.IndexOf(nameField);
            nameFieldParent.Remove(nameField);
            nameField.style.flexGrow = 1;
            nameField.style.flexShrink = 1;
            nameField.style.maxWidth = new StyleLength(new Length(85, LengthUnit.Percent));
            applyAllBtn.style.flexShrink = 0;
            row.Add(nameField);
            row.Add(applyAllBtn);
            nameFieldParent.Insert(index, row);
        }
        
        nameField?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            EnsureLocalizedStringInitialized(ref activeItem.localizedName, "Name");
            SetLocalizedStringValue(activeItem.localizedName, evt.newValue, false);
        });
        nameField?.RegisterCallback<FocusOutEvent>(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Item Name");
            EditorUtility.SetDirty(itemDatabase);
            SavePendingChanges();
            itemListView.Rebuild();
        });
        
        var iconField = itemDetailsPane.Q<ObjectField>("ItemIcon");
        iconField.objectType = typeof(Sprite);
        iconField?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Item Icon");
            activeItem.itemIcon = evt.newValue as Sprite;
            if (activeItem.itemIcon != null)
                itemIconPreview.style.backgroundImage = new StyleBackground(Background.FromSprite(activeItem.itemIcon));
            else if (defaultIcon != null)
                itemIconPreview.style.backgroundImage = new StyleBackground(Background.FromSprite(defaultIcon));
            else
                itemIconPreview.style.backgroundImage = StyleKeyword.None;
            EditorUtility.SetDirty(itemDatabase);
            itemListView.Rebuild();
        });
        
        BindEnumField<TetrisPieceShape>("TetrisPieceShape", v => activeItem.tetrisPieceShape = v, () => activeItem?.tetrisPieceShape ?? default);
        BindEnumField<InventorySlotType>("InventorySlotType", v => activeItem.inventorySlotType = v, () => activeItem?.inventorySlotType ?? default);
        BindEnumField<ItemRarity>("ItemRarity", v => activeItem.itemRarity = v, () => activeItem?.itemRarity ?? default);
        BindEnumField<Dir>("Dir", v => activeItem.dir = v, () => activeItem?.dir ?? default);
        
        BindIntField("Width", v => activeItem.xWidth = v);
        BindIntField("Height", v => activeItem.yHeight = v);
        BindIntField("ItemDamage", v => activeItem.itemDamage = v);
        BindIntField("MaxStack", v => activeItem.maxStack = v);
        BindIntField("Price", v => activeItem.itemPrice = v);
        
        BindFloatField("Weight", v => activeItem.weight = v);
        BindFloatField("ReloadTime", v => activeItem.reloadTime = v);
        
        var sellSlider = itemDetailsPane.Q<Slider>("SellPercentage");
        sellSlider?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Sell Percentage");
            activeItem.sellPercentage = evt.newValue;
            EditorUtility.SetDirty(itemDatabase);
        });
        
        var entityField = itemDetailsPane.Q<ObjectField>("ItemEntity");
        entityField.objectType = typeof(GameObject);
        entityField?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Item Entity");
            activeItem.itemEntity = evt.newValue as GameObject;
            EditorUtility.SetDirty(itemDatabase);
        });
        
        var gridUIField = itemDetailsPane.Q<ObjectField>("GridUIPrefab");
        gridUIField.objectType = typeof(Transform);
        gridUIField?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Grid UI Prefab");
            activeItem.gridUIPrefab = evt.newValue as Transform;
            EditorUtility.SetDirty(itemDatabase);
        });
        
        var descField = itemDetailsPane.Q<TextField>("Description");
        descField?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            EnsureLocalizedStringInitialized(ref activeItem.localizedDescription, "Desc");
            SetLocalizedStringValue(activeItem.localizedDescription, evt.newValue, false);
        });
        descField?.RegisterCallback<FocusOutEvent>(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, "Changed Item Description");
            EditorUtility.SetDirty(itemDatabase);
            SavePendingChanges();
        });
    }
    
    private void BindEnumField<T>(string name, Action<T> setter, Func<T> getter) where T : Enum
    {
        var field = itemDetailsPane.Q<EnumField>(name);
        field?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, $"Changed {name}");
            setter((T)evt.newValue);
            EditorUtility.SetDirty(itemDatabase);
        });
    }
    
    private void BindIntField(string name, Action<int> setter)
    {
        var field = itemDetailsPane.Q<IntegerField>(name);
        field?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, $"Changed {name}");
            setter(evt.newValue);
            EditorUtility.SetDirty(itemDatabase);
        });
    }
    
    private void BindFloatField(string name, Action<float> setter)
    {
        var field = itemDetailsPane.Q<FloatField>(name);
        field?.RegisterValueChangedCallback(evt =>
        {
            if (activeItem == null) return;
            Undo.RecordObject(itemDatabase, $"Changed {name}");
            setter(evt.newValue);
            EditorUtility.SetDirty(itemDatabase);
        });
    }
    
    private void OnItemSelectionChanged(IEnumerable<object> selection)
    {
        activeItem = selection.FirstOrDefault() as ItemDetails;
        bool hasSelection = activeItem != null;
        itemDetailsPane.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
        if (itemsLogoContainer != null) itemsLogoContainer.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
        
        if (activeItem != null) RefreshItemDetails();
    }
    
    private void RefreshItemDetails()
    {
        if (activeItem == null) return;
        
        itemDetailsPane.Q<IntegerField>("ItemID").SetValueWithoutNotify(activeItem.itemID);
        itemDetailsPane.Q<TextField>("ItemName").SetValueWithoutNotify(GetLocalizedStringValue(activeItem.localizedName));
        itemDetailsPane.Q<ObjectField>("ItemIcon").SetValueWithoutNotify(activeItem.itemIcon);
        if (activeItem.itemIcon != null)
            itemIconPreview.style.backgroundImage = new StyleBackground(Background.FromSprite(activeItem.itemIcon));
        else if (defaultIcon != null)
            itemIconPreview.style.backgroundImage = new StyleBackground(Background.FromSprite(defaultIcon));
        else
            itemIconPreview.style.backgroundImage = StyleKeyword.None;
        
        var shapeField = itemDetailsPane.Q<EnumField>("TetrisPieceShape");
        shapeField.Init(activeItem.tetrisPieceShape);
        shapeField.SetValueWithoutNotify(activeItem.tetrisPieceShape);
        
        var slotField = itemDetailsPane.Q<EnumField>("InventorySlotType");
        slotField.Init(activeItem.inventorySlotType);
        slotField.SetValueWithoutNotify(activeItem.inventorySlotType);
        
        var rarityField = itemDetailsPane.Q<EnumField>("ItemRarity");
        rarityField.Init(activeItem.itemRarity);
        rarityField.SetValueWithoutNotify(activeItem.itemRarity);
        
        var dirField = itemDetailsPane.Q<EnumField>("Dir");
        dirField.Init(activeItem.dir);
        dirField.SetValueWithoutNotify(activeItem.dir);
        
        itemDetailsPane.Q<IntegerField>("Width").SetValueWithoutNotify(activeItem.xWidth);
        itemDetailsPane.Q<IntegerField>("Height").SetValueWithoutNotify(activeItem.yHeight);
        itemDetailsPane.Q<FloatField>("Weight").SetValueWithoutNotify(activeItem.weight);
        itemDetailsPane.Q<IntegerField>("ItemDamage").SetValueWithoutNotify(activeItem.itemDamage);
        itemDetailsPane.Q<IntegerField>("MaxStack").SetValueWithoutNotify(activeItem.maxStack);
        itemDetailsPane.Q<FloatField>("ReloadTime").SetValueWithoutNotify(activeItem.reloadTime);
        itemDetailsPane.Q<IntegerField>("Price").SetValueWithoutNotify(activeItem.itemPrice);
        itemDetailsPane.Q<Slider>("SellPercentage").SetValueWithoutNotify(activeItem.sellPercentage);
        itemDetailsPane.Q<ObjectField>("ItemEntity").SetValueWithoutNotify(activeItem.itemEntity);
        itemDetailsPane.Q<ObjectField>("GridUIPrefab").SetValueWithoutNotify(activeItem.gridUIPrefab);
        itemDetailsPane.Q<TextField>("Description").SetValueWithoutNotify(GetLocalizedStringValue(activeItem.localizedDescription));
    }
    
    private void OnAddItem()
    {
        var newItem = new ItemDetails();
        newItem.itemID = itemList.Count > 0 ? itemList.Max(i => i.itemID) + 1 : 1001;
        newItem.localizedName = new LocalizedString(StringTableName, $"Item_{newItem.itemID}_Name");
        newItem.localizedDescription = new LocalizedString(StringTableName, $"Item_{newItem.itemID}_Desc");
        
        Undo.RecordObject(itemDatabase, "Add Item");
        itemList.Add(newItem);
        EditorUtility.SetDirty(itemDatabase);
        
        CreateStringTableEntry(newItem.localizedName.TableEntryReference.Key, "New Item");
        CreateStringTableEntry(newItem.localizedDescription.TableEntryReference.Key, "");
        
        FilterItemList(itemSearchField?.value);
        itemListView.selectedIndex = filteredItemList.IndexOf(newItem);
        UpdateStatus($"Added item {newItem.itemID}");
    }
    
    private void OnDeleteItem()
    {
        if (activeItem == null) return;
        Undo.RecordObject(itemDatabase, "Delete Item");
        itemList.Remove(activeItem);
        EditorUtility.SetDirty(itemDatabase);
        itemListView.ClearSelection();
        FilterItemList(itemSearchField?.value);
        itemDetailsPane.style.display = DisplayStyle.None;
        activeItem = null;
        UpdateStatus("Item deleted");
    }
    #endregion
    
    #region Shapes Panel
    private void InitializeShapesPanel()
    {
        shapeListView = rootVisualElement.Q<ListView>("ShapeListView");
        shapeDetailsPane = rootVisualElement.Q<VisualElement>("ShapeDetailsPane");
        shapeGridPreview = rootVisualElement.Q<VisualElement>("ShapeGridPreview");
        pointsListContainer = rootVisualElement.Q<ScrollView>("PointsListContainer");
        
        rootVisualElement.Q<Button>("AddShapeBtn").clicked += OnAddShape;
        rootVisualElement.Q<Button>("DeleteShapeBtn").clicked += OnDeleteShape;
        rootVisualElement.Q<Button>("AddPointBtn").clicked += OnAddPoint;
        
        SetupShapeListView();
        BindShapeFields();
        
        shapesLogoContainer = CreateLogoContainer();
        if (shapeDetailsPane != null && shapeDetailsPane.parent != null)
        {
            var parent = shapeDetailsPane.parent;
            var rightPaneContainer = new VisualElement();
            rightPaneContainer.style.flexGrow = 1;
            
            int index = parent.IndexOf(shapeDetailsPane);
            if (index >= 0)
            {
                parent.Remove(shapeDetailsPane);
                parent.Insert(index, rightPaneContainer);
                rightPaneContainer.Add(shapeDetailsPane);
                rightPaneContainer.Add(shapesLogoContainer);
                shapeDetailsPane.style.flexGrow = 1;
            }
        }
        
        shapeDetailsPane.style.display = DisplayStyle.None;
        if (shapesLogoContainer != null) shapesLogoContainer.style.display = DisplayStyle.Flex;
    }
    
    private void SetupShapeListView()
    {
        shapeListView.makeItem = () =>
        {
            var label = new Label();
            label.style.paddingTop = 6;
            label.style.paddingBottom = 6;
            label.style.paddingLeft = 8;
            label.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
            return label;
        };
        
        shapeListView.bindItem = (e, i) =>
        {
            if (i >= shapeList.Count) return;
            var shape = shapeList[i];
            (e as Label).text = $"{shape.tetrisPieceShape} ({shape.points?.Count ?? 0} pts)";
        };
        
        shapeListView.selectionChanged += OnShapeSelectionChanged;
    }
    
    private void BindShapeFields()
    {
        var typeField = shapeDetailsPane.Q<EnumField>("ShapeType");
        typeField?.RegisterValueChangedCallback(evt =>
        {
            if (activeShape == null) return;
            Undo.RecordObject(shapeDatabase, "Changed Shape Type");
            activeShape.tetrisPieceShape = (TetrisPieceShape)evt.newValue;
            EditorUtility.SetDirty(shapeDatabase);
            shapeListView.Rebuild();
        });
    }
    
    private void OnShapeSelectionChanged(IEnumerable<object> selection)
    {
        activeShape = selection.FirstOrDefault() as PointSet;
        bool hasSelection = activeShape != null;
        shapeDetailsPane.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
        if (shapesLogoContainer != null) shapesLogoContainer.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
        
        if (activeShape != null) RefreshShapeDetails();
    }
    
    private void RefreshShapeDetails()
    {
        if (activeShape == null) return;
        
        var typeField = shapeDetailsPane.Q<EnumField>("ShapeType");
        typeField.Init(activeShape.tetrisPieceShape);
        typeField.SetValueWithoutNotify(activeShape.tetrisPieceShape);
        
        RefreshGridPreview();
        RefreshPointsList();
    }
    
    private void RefreshGridPreview()
    {
        shapeGridPreview.Clear();
        if (activeShape?.points == null || activeShape.points.Count == 0) return;
        
        int maxX = activeShape.points.Max(p => p.x) + 1;
        int maxY = activeShape.points.Max(p => p.y) + 1;
        
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        
        for (int y = 0; y < maxY; y++)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            
            for (int x = 0; x < maxX; x++)
            {
                var cell = new VisualElement();
                cell.AddToClassList("grid-cell");
                
                if (activeShape.points.Any(p => p.x == x && p.y == y))
                    cell.AddToClassList("grid-cell-active");
                
                row.Add(cell);
            }
            container.Add(row);
        }
        
        shapeGridPreview.Add(container);
    }
    
    private void RefreshPointsList()
    {
        pointsListContainer.Clear();
        if (activeShape?.points == null) return;
        
        for (int i = 0; i < activeShape.points.Count; i++)
        {
            int index = i;
            var point = activeShape.points[i];
            
            var row = new VisualElement();
            row.AddToClassList("point-row");
            
            var xField = new IntegerField("X") { value = point.x };
            xField.style.width = 80;
            xField.style.marginRight = 8;
            xField.labelElement.style.minWidth = 15;
            xField.labelElement.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            var xInput = xField.Q<VisualElement>("unity-text-input");
            if (xInput != null)
            {
                xInput.style.color = Color.white;
                xInput.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            }
            xField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(shapeDatabase, "Changed Point X");
                activeShape.points[index] = new Vector2Int(evt.newValue, activeShape.points[index].y);
                EditorUtility.SetDirty(shapeDatabase);
                RefreshGridPreview();
            });
            
            var yField = new IntegerField("Y") { value = point.y };
            yField.style.width = 80;
            yField.style.marginRight = 8;
            yField.labelElement.style.minWidth = 15;
            yField.labelElement.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            var yInput = yField.Q<VisualElement>("unity-text-input");
            if (yInput != null)
            {
                yInput.style.color = Color.white;
                yInput.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            }
            yField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(shapeDatabase, "Changed Point Y");
                activeShape.points[index] = new Vector2Int(activeShape.points[index].x, evt.newValue);
                EditorUtility.SetDirty(shapeDatabase);
                RefreshGridPreview();
            });
            
            var deleteBtn = new Button(() =>
            {
                Undo.RecordObject(shapeDatabase, "Delete Point");
                activeShape.points.RemoveAt(index);
                EditorUtility.SetDirty(shapeDatabase);
                RefreshShapeDetails();
                shapeListView.Rebuild();
            });
            deleteBtn.text = "×";
            deleteBtn.AddToClassList("point-delete-btn");
            
            row.Add(xField);
            row.Add(yField);
            row.Add(deleteBtn);
            
            pointsListContainer.Add(row);
        }
    }
    
    private void OnAddShape()
    {
        var newShape = new PointSet();
        newShape.tetrisPieceShape = TetrisPieceShape.Frame;
        newShape.points = new List<Vector2Int> { new Vector2Int(0, 0) };
        
        Undo.RecordObject(shapeDatabase, "Add Shape");
        shapeList.Add(newShape);
        EditorUtility.SetDirty(shapeDatabase);
        
        shapeListView.Rebuild();
        shapeListView.selectedIndex = shapeList.Count - 1;
        UpdateStatus($"Added shape {newShape.tetrisPieceShape}");
    }
    
    private void OnDeleteShape()
    {
        if (activeShape == null) return;
        Undo.RecordObject(shapeDatabase, "Delete Shape");
        shapeList.Remove(activeShape);
        EditorUtility.SetDirty(shapeDatabase);
        shapeListView.ClearSelection();
        shapeListView.Rebuild();
        shapeDetailsPane.style.display = DisplayStyle.None;
        if (shapesLogoContainer != null) shapesLogoContainer.style.display = DisplayStyle.Flex;
        activeShape = null;
        UpdateStatus("Shape deleted");
    }
    
    private void OnAddPoint()
    {
        if (activeShape == null) return;
        if (activeShape.points == null) activeShape.points = new List<Vector2Int>();
        
        Undo.RecordObject(shapeDatabase, "Add Point");
        activeShape.points.Add(new Vector2Int(0, 0));
        EditorUtility.SetDirty(shapeDatabase);
        RefreshShapeDetails();
        shapeListView.Rebuild();
    }
    #endregion
    
    #region Config Panel
    private void InitializeConfigPanel()
    {
        if (configDatabase == null) return;
        serializedConfig = new SerializedObject(configDatabase);
        
        BindConfigFields();
        SetupInvalidReasonColorsList();
    }
    
    private void BindConfigFields()
    {
        BindToggleField("BlockSelfOwnedContainer", "blockSelfOwnedContainer");
        BindToggleField("BlockOutOfBounds", "blockOutOfBounds");
        BindToggleField("BlockSlotOccupied", "blockSlotOccupied");
        BindToggleField("BlockSlotTypeMismatch", "blockSlotTypeMismatch");
        
        BindToggleField("OverrideHighlightPalette", "overrideHighlightPalette");
        
        var paletteProp = serializedConfig.FindProperty("highlightPalette");
        if (paletteProp != null)
        {
            BindColorField("ColorValidEmpty", paletteProp.FindPropertyRelative("ValidEmpty"));
            BindColorField("ColorInvalid", paletteProp.FindPropertyRelative("Invalid"));
            BindColorField("ColorCanStack", paletteProp.FindPropertyRelative("CanStack"));
            BindColorField("ColorCanQuickExchange", paletteProp.FindPropertyRelative("CanQuickExchange"));
        }
        
        var addBtn = configPanel.Q<Button>("AddColorOverrideBtn");
        if (addBtn != null) addBtn.clicked += OnAddColorOverride;
    }
    
    private void BindToggleField(string uiName, string propertyName)
    {
        var field = configPanel.Q<Toggle>(uiName);
        var prop = serializedConfig.FindProperty(propertyName);
        if (field != null && prop != null)
        {
            field.BindProperty(prop);
        }
    }
    
    private void BindColorField(string uiName, SerializedProperty property)
    {
        var field = configPanel.Q<ColorField>(uiName);
        if (field != null && property != null)
        {
            field.BindProperty(property);
            field.labelElement.style.minWidth = 150;
        }
    }
    
    private void SetupInvalidReasonColorsList()
    {
        invalidReasonColorsList = configPanel.Q<ListView>("InvalidReasonColorsList");
        var listProp = serializedConfig.FindProperty("invalidReasonColors");
        
        if (invalidReasonColorsList != null && listProp != null)
        {
            invalidReasonColorsList.BindProperty(listProp);
            
            invalidReasonColorsList.makeItem = () =>
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.marginBottom = 2;
                
                var reasonField = new EnumField(InventoryPlacementBlockReason.None);
                reasonField.name = "Reason";
                reasonField.style.flexGrow = 1;
                reasonField.style.flexBasis = 0;
                reasonField.style.marginRight = 5;
                
                var colorField = new ColorField();
                colorField.name = "Color";
                colorField.style.flexGrow = 1;
                colorField.style.flexBasis = 0;
                
                container.Add(reasonField);
                container.Add(colorField);
                
                return container;
            };

            invalidReasonColorsList.bindItem = (e, i) =>
            {
                if (serializedConfig == null || listProp == null) return;
                if (i < 0 || i >= listProp.arraySize) return;

                var prop = listProp.GetArrayElementAtIndex(i);
                if (prop != null)
                {
                    var reasonProp = prop.FindPropertyRelative("Reason");
                    var colorProp = prop.FindPropertyRelative("Color");
                    
                    var reasonField = e.Q<EnumField>("Reason");
                    var colorField = e.Q<ColorField>("Color");
                    
                    if (reasonField != null && reasonProp != null) 
                        reasonField.BindProperty(reasonProp);
                        
                    if (colorField != null && colorProp != null) 
                        colorField.BindProperty(colorProp);
                }
            };
        }
    }
    
    private void OnAddColorOverride()
    {
        var listProp = serializedConfig.FindProperty("invalidReasonColors");
        listProp.InsertArrayElementAtIndex(listProp.arraySize);
        serializedConfig.ApplyModifiedProperties();
    }
    #endregion
    
    #region Data Management
    private void LoadDatabases()
    {
        // Load Item Database
        itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDataList_SO>("Assets/GameData/ItemDataList_SO.asset");
        if (itemDatabase == null)
        {
            var guids = AssetDatabase.FindAssets("t:ItemDataList_SO");
            if (guids.Length > 0)
                itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDataList_SO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        
        if (itemDatabase != null)
        {
            itemList = itemDatabase.itemDetailsList;
            FilterItemList(itemSearchField?.value);
        }
        
        // Load Shape Database
        shapeDatabase = AssetDatabase.LoadAssetAtPath<TetrisItemPointSet_SO>("Assets/GameData/TetrisItemPointSet_SO.asset");
        if (shapeDatabase == null)
        {
            var guids = AssetDatabase.FindAssets("t:TetrisItemPointSet_SO");
            if (guids.Length > 0)
                shapeDatabase = AssetDatabase.LoadAssetAtPath<TetrisItemPointSet_SO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        
        if (shapeDatabase != null)
        {
            shapeList = shapeDatabase.TetrisPieceShapeList;
            shapeListView.itemsSource = shapeList;
            shapeListView.Rebuild();
        }

        // Load Config Database
        configDatabase = AssetDatabase.LoadAssetAtPath<InventoryPlacementConfig_SO>("Assets/GameData/InventoryPlacementConfig_SO.asset");
        if (configDatabase == null)
        {
            var guids = AssetDatabase.FindAssets("t:InventoryPlacementConfig_SO");
            if (guids.Length > 0)
                configDatabase = AssetDatabase.LoadAssetAtPath<InventoryPlacementConfig_SO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
    #endregion
    
    #region Localization Helpers
    private void LoadAvailableLocales()
    {
        availableLocales.Clear();
        var locales = LocalizationEditorSettings.GetLocales();
        if (locales != null && locales.Count > 0)
        {
            availableLocales.AddRange(locales);
        }
        else
        {
            var guids = AssetDatabase.FindAssets("t:Locale", new[] { "Assets/Localization Settings/Locales" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
                if (locale != null) availableLocales.Add(locale);
            }
        }
    }
    
    private void LoadOrCreateStringTable()
    {
        var collections = LocalizationEditorSettings.GetStringTableCollections();
        foreach (var collection in collections)
        {
            if (collection.TableCollectionName == StringTableName)
            {
                itemStringTable = collection;
                return;
            }
        }
        
        if (itemStringTable == null && availableLocales.Count > 0)
        {
            itemStringTable = LocalizationEditorSettings.CreateStringTableCollection(
                StringTableName, "Assets/Localization Settings/Tables");
        }
    }
    
    private string GetLocalizedStringValue(LocalizedString localizedString)
    {
        if (localizedString == null || localizedString.IsEmpty) return string.Empty;
        if (selectedLanguageIndex < 0 || selectedLanguageIndex >= availableLocales.Count) return string.Empty;
        
        var locale = availableLocales[selectedLanguageIndex];
        if (itemStringTable == null) return string.Empty;
        
        var table = itemStringTable.GetTable(locale.Identifier) as StringTable;
        if (table == null) return string.Empty;
        
        var entry = table.GetEntry(localizedString.TableEntryReference.Key);
        return entry != null ? entry.LocalizedValue : string.Empty;
    }
    
    private void SetLocalizedStringValue(LocalizedString localizedString, string value, bool saveImmediately = true)
    {
        if (localizedString == null || localizedString.IsEmpty) return;
        if (selectedLanguageIndex < 0 || selectedLanguageIndex >= availableLocales.Count) return;
        
        var locale = availableLocales[selectedLanguageIndex];
        if (itemStringTable == null)
        {
            LoadOrCreateStringTable();
            if (itemStringTable == null) return;
        }
        
        var table = itemStringTable.GetTable(locale.Identifier) as StringTable;
        if (table == null) return;
        
        var key = localizedString.TableEntryReference.Key;
        if (string.IsNullOrEmpty(key)) return;

        if (itemStringTable.SharedData.GetId(key) == SharedTableData.EmptyId)
        {
            itemStringTable.SharedData.AddKey(key);
            EditorUtility.SetDirty(itemStringTable.SharedData);
        }
        
        var entry = table.GetEntry(key);
        if (entry != null)
            entry.Value = value;
        else
            table.AddEntry(key, value);
        
        EditorUtility.SetDirty(table);
        if (saveImmediately) AssetDatabase.SaveAssets();
    }
    
    private void EnsureLocalizedStringInitialized(ref LocalizedString localizedString, string suffix)
    {
        if (activeItem == null) return;
        
        if (localizedString == null || localizedString.IsEmpty)
        {
            string key = $"Item_{activeItem.itemID}_{suffix}";
            localizedString = new LocalizedString(StringTableName, key);
            
            if (itemStringTable == null) LoadOrCreateStringTable();
            
            if (itemStringTable != null)
            {
                if (itemStringTable.SharedData.GetId(key) == SharedTableData.EmptyId)
                {
                    itemStringTable.SharedData.AddKey(key);
                    EditorUtility.SetDirty(itemStringTable.SharedData);
                }

                foreach (var table in itemStringTable.StringTables)
                {
                    if (table != null && table.GetEntry(key) == null)
                    {
                        table.AddEntry(key, "");
                        EditorUtility.SetDirty(table);
                    }
                }
            }
            EditorUtility.SetDirty(itemDatabase);
        }
    }
    
    private void CreateStringTableEntry(string key, string defaultValue)
    {
        if (itemStringTable == null) return;

        if (itemStringTable.SharedData.GetId(key) == SharedTableData.EmptyId)
        {
            itemStringTable.SharedData.AddKey(key);
            EditorUtility.SetDirty(itemStringTable.SharedData);
        }

        foreach (var table in itemStringTable.StringTables)
        {
            if (table != null)
            {
                if (table.GetEntry(key) == null)
                {
                    table.AddEntry(key, defaultValue);
                    EditorUtility.SetDirty(table);
                }
            }
        }
        AssetDatabase.SaveAssets();
    }
    
    private void OnSaveAll()
    {
        EditorUtility.SetDirty(itemDatabase);
        if (shapeDatabase != null) EditorUtility.SetDirty(shapeDatabase);
        if (configDatabase != null) EditorUtility.SetDirty(configDatabase);
        
        if (itemStringTable != null)
        {
            if (itemStringTable.SharedData != null)
                EditorUtility.SetDirty(itemStringTable.SharedData);
                
            foreach (var table in itemStringTable.StringTables)
            {
                if (table != null) EditorUtility.SetDirty(table);
            }
        }
        
        AssetDatabase.SaveAssets();
        UpdateStatus("Saved all data successfully!");
    }

    private void SavePendingChanges()
    {
        OnSaveAll();
    }
    
    private void ApplyNameToAllLanguages()
    {
        if (activeItem == null) return;
        if (activeItem.localizedName == null || activeItem.localizedName.IsEmpty)
        {
            UpdateStatus("No name to apply - please enter a name first");
            return;
        }
        
        var currentName = GetLocalizedStringValue(activeItem.localizedName);
        if (string.IsNullOrEmpty(currentName))
        {
            UpdateStatus("Current name is empty");
            return;
        }
        
        if (itemStringTable == null) return;
        
        var key = activeItem.localizedName.TableEntryReference.Key;
        if (string.IsNullOrEmpty(key)) return;
        
        int count = 0;
        foreach (var table in itemStringTable.StringTables)
        {
            if (table != null)
            {
                var entry = table.GetEntry(key);
                if (entry != null)
                    entry.Value = currentName;
                else
                    table.AddEntry(key, currentName);
                EditorUtility.SetDirty(table);
                count++;
            }
        }
        
        AssetDatabase.SaveAssets();
        UpdateStatus($"Applied \"{currentName}\" to {count} languages");
    }
    #endregion
}

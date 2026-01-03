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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemEditor : EditorWindow
{
    private ItemDataList_SO dataBase;
    private List<ItemDetails> itemList = new List<ItemDetails>();
    private VisualTreeAsset itemRowTemplate;
    private ScrollView itemDetailsSection;
    private ItemDetails activeItem;

    private Sprite defaultIcon;

    private VisualElement iconPreview;
    private ListView itemListView;

    [MenuItem("ChosTIS/ItemEditor")]
    public static void ShowExample()
    {
        ItemEditor wnd = GetWindow<ItemEditor>();
        wnd.titleContent = new GUIContent("ItemEditor");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/UI Builder/ItemEditor.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
        itemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/UI Builder/ItemRowTemplate.uxml");
        defaultIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtResource/ItemEditor/DefaultIcon.png");
        itemListView = root.Q<VisualElement>("ItemList").Q<ListView>("ListView");
        itemDetailsSection = root.Q<ScrollView>("ItemDetails");
        iconPreview = itemDetailsSection.Q<VisualElement>("Icon");
        root.Q<Button>("AddButton").clicked += OnAddItemClicked;
        root.Q<Button>("DeleteButton").clicked += OnDeleteClicked;
        LoadDataBase();
        GenerateListView();
    }

    #region Key Events
    private void OnDeleteClicked()
    {
        itemList.Remove(activeItem);
        itemListView.Rebuild();
        itemDetailsSection.visible = false;
    }

    private void OnAddItemClicked()
    {
        ItemDetails newItem = new ItemDetails();
        newItem.itemName = "NEW ITEM";
        newItem.itemID = 1001 + itemList.Count;
        itemList.Add(newItem);
        itemListView.Rebuild();
    }
    #endregion

    private void LoadDataBase()
    {
        var dataArray = AssetDatabase.FindAssets("t:ItemDataList_SO");

        if (dataArray != null)
        {
            var path = AssetDatabase.GUIDToAssetPath(dataArray[0]);
            dataBase = AssetDatabase.LoadAssetAtPath(path, typeof(ItemDataList_SO)) as ItemDataList_SO;
        }

        itemList = dataBase.itemDetailsList;
        EditorUtility.SetDirty(dataBase);
    }

    private void GenerateListView()
    {
        Func<VisualElement> makeItem = () => itemRowTemplate.CloneTree();

        Action<VisualElement, int> bindItem = (e, i) =>
        {
            if (i < itemList.Count)
            {
                if (itemList[i].itemIcon != null)
                    e.Q<VisualElement>("Icon").style.backgroundImage = itemList[i].itemIcon.texture;
                e.Q<Label>("Name").text = itemList[i] == null ? "NO ITEM" : itemList[i].itemName;
            }
        };

        itemListView.fixedItemHeight = 50;
        itemListView.itemsSource = itemList;
        itemListView.makeItem = makeItem;
        itemListView.bindItem = bindItem;

        itemListView.selectionChanged += OnListSelectionChange;

        itemDetailsSection.visible = false;
    }

    private void OnListSelectionChange(IEnumerable<object> selectedItem)
    {
        activeItem = (ItemDetails)selectedItem.First();
        GetItemDetails();
        itemDetailsSection.visible = true;
    }

    private void GetItemDetails()
    {
        itemDetailsSection.MarkDirtyRepaint();

        itemDetailsSection.Q<IntegerField>("ItemID").value = activeItem.itemID;
        itemDetailsSection.Q<IntegerField>("ItemID").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemID = evt.newValue;
        });

        itemDetailsSection.Q<TextField>("ItemName").value = activeItem.itemName;
        itemDetailsSection.Q<TextField>("ItemName").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemName = evt.newValue;
            itemListView.Rebuild();
        });

        iconPreview.style.backgroundImage = activeItem.itemIcon == null ? defaultIcon.texture : activeItem.itemIcon.texture;
        itemDetailsSection.Q<ObjectField>("ItemIcon").value = activeItem.itemIcon;
        itemDetailsSection.Q<ObjectField>("ItemIcon").RegisterValueChangedCallback(evt =>
        {
            Sprite newIcon = evt.newValue as Sprite;
            activeItem.itemIcon = newIcon;
            iconPreview.style.backgroundImage = newIcon == null ? defaultIcon.texture : newIcon.texture;
            itemListView.Rebuild();
        });

        itemDetailsSection.Q<ObjectField>("ItemUI").value = activeItem.itemUI;
        itemDetailsSection.Q<ObjectField>("ItemUI").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemUI = (Sprite)evt.newValue;
        });

        itemDetailsSection.Q<ObjectField>("GridUIPrefab").value = activeItem.gridUIPrefab;
        itemDetailsSection.Q<ObjectField>("GridUIPrefab").RegisterValueChangedCallback(evt =>
        {
            activeItem.gridUIPrefab = (Transform)evt.newValue;
        });

        itemDetailsSection.Q<ObjectField>("ItemEntity").value = activeItem.itemEntity;
        itemDetailsSection.Q<ObjectField>("ItemEntity").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemEntity = (GameObject)evt.newValue;
        });

        itemDetailsSection.Q<EnumField>("TetrisPieceShape").Init(activeItem.tetrisPieceShape);
        itemDetailsSection.Q<EnumField>("TetrisPieceShape").value = activeItem.tetrisPieceShape;
        itemDetailsSection.Q<EnumField>("TetrisPieceShape").RegisterValueChangedCallback(evt =>
        {
            activeItem.tetrisPieceShape = (TetrisPieceShape)evt.newValue;
        });

        itemDetailsSection.Q<EnumField>("InventorySlotType").Init(activeItem.inventorySlotType);
        itemDetailsSection.Q<EnumField>("InventorySlotType").value = activeItem.inventorySlotType;
        itemDetailsSection.Q<EnumField>("InventorySlotType").RegisterValueChangedCallback(evt =>
        {
            activeItem.inventorySlotType = (InventorySlotType)evt.newValue;
        });

        itemDetailsSection.Q<EnumField>("ItemRarity").Init(activeItem.itemRarity);
        itemDetailsSection.Q<EnumField>("ItemRarity").value = activeItem.itemRarity;
        itemDetailsSection.Q<EnumField>("ItemRarity").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemRarity = (ItemRarity)evt.newValue;
        });

        itemDetailsSection.Q<TextField>("Description").value = activeItem.itemDescription;
        itemDetailsSection.Q<TextField>("Description").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemDescription = evt.newValue;
        });

        itemDetailsSection.Q<IntegerField>("ItemDamage").value = activeItem.itemDamage;
        itemDetailsSection.Q<IntegerField>("ItemDamage").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemDamage = evt.newValue;
        });

        itemDetailsSection.Q<IntegerField>("MaxStack").value = activeItem.maxStack;
        itemDetailsSection.Q<IntegerField>("MaxStack").RegisterValueChangedCallback(evt =>
        {
            activeItem.maxStack = evt.newValue;
        });

        itemDetailsSection.Q<FloatField>("ReloadTime").value = activeItem.reloadTime;
        itemDetailsSection.Q<FloatField>("ReloadTime").RegisterValueChangedCallback(evt =>
        {
            activeItem.reloadTime = evt.newValue;
        });

        itemDetailsSection.Q<IntegerField>("Height").value = activeItem.yHeight;
        itemDetailsSection.Q<IntegerField>("Height").RegisterValueChangedCallback(evt =>
        {
            activeItem.yHeight = evt.newValue;
        });

        itemDetailsSection.Q<IntegerField>("Width").value = activeItem.xWidth;
        itemDetailsSection.Q<IntegerField>("Width").RegisterValueChangedCallback(evt =>
        {
            activeItem.xWidth = evt.newValue;
        });

        itemDetailsSection.Q<FloatField>("Weight").value = activeItem.weight;
        itemDetailsSection.Q<FloatField>("Weight").RegisterValueChangedCallback(evt =>
        {
            activeItem.weight = evt.newValue;
        });

        itemDetailsSection.Q<EnumField>("Dir").Init(activeItem.dir);
        itemDetailsSection.Q<EnumField>("Dir").value = activeItem.dir;
        itemDetailsSection.Q<EnumField>("Dir").RegisterValueChangedCallback(evt =>
        {
            activeItem.dir = (Dir)evt.newValue;
        });

        itemDetailsSection.Q<IntegerField>("Price").value = activeItem.itemPrice;
        itemDetailsSection.Q<IntegerField>("Price").RegisterValueChangedCallback(evt =>
        {
            activeItem.itemPrice = evt.newValue;
        });

        itemDetailsSection.Q<Slider>("SellPercentage").value = activeItem.sellPercentage;
        itemDetailsSection.Q<Slider>("SellPercentage").RegisterValueChangedCallback(evt =>
        {
            activeItem.sellPercentage = evt.newValue;
        });
    }
}

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
using UnityEngine;

[Serializable]
public class ItemDetails
{
    public int itemID;
    public string itemName;
    public string itemDescription;

    public GameObject itemEntity;

    public TetrisPieceShape tetrisPieceShape;
    public Sprite itemIcon;
    public Sprite itemUI;


    public Transform gridUIPrefab;
    public InventorySlotType inventorySlotType;
    public ItemRarity itemRarity;
    public int itemDamage;
    public int maxStack;
    public float reloadTime;

    public int yHeight;
    public int xWidth;
    public float weight;
    public Dir dir;

    public int itemPrice;
    [Range(0f, 1f)]
    public float sellPercentage;
    
}

[Serializable]
public class PointSet
{
    public int itemShapeID;
    public TetrisPieceShape tetrisPieceShape;
    public List<Vector2Int> points;
}

[Serializable]
public class GameSaveData
{
    public Dictionary<string, List<TetrisItemPersistentData>> inventoryDict;
}

[Serializable]
public class SaveFileWrapper<T>
{
    public int Version;
    public string Timestamp;
    public T Payload;
}

[Serializable]
public class TetrisItemPersistentData
{
    public int itemID;
    public string itemGuid;
    public Vector2Int orginPosition;
    public Dir direction;
    public int stack;
    public string parentItemGuid;
    public string persistentGridGuid;
    public bool isOnSlot;
    public int slotIndex;
    public int gridPIndex;

    public Dictionary<string, object> CustomData;
}

[Serializable]
public struct InventoryHighlightPalette
{
    public Color ValidEmpty;
    public Color Invalid;
    public Color CanStack;
    public Color CanQuickExchange;

    public static InventoryHighlightPalette DefaultFromSettings()
    {
        return new InventoryHighlightPalette
        {
            ValidEmpty = Settings.Green,
            Invalid = Settings.Red,
            CanStack = Settings.Yellow,
            CanQuickExchange = Settings.LightBlue,
        };
    }
}

[Serializable]
public struct InventoryPlacementBlockColorOverride
{
    public InventoryPlacementBlockReason Reason;
    public Color Color;
}

public struct ItemTextStyle
{
    public readonly float Padding;
    public readonly float LeftOffset;
    public readonly float RightOffset;
    public readonly float BottomOffset;
    public readonly float TopOffset;
    public readonly int MaxFontSize;
    public readonly int MinFontSize;
    public readonly Vector2 OutlineDistance;
    public readonly TextAnchor Alignment;
    public readonly HorizontalWrapMode HorizontalOverflow;
    public readonly VerticalWrapMode VerticalOverflow;
    public readonly float LineSpacing;
    public readonly bool BestFit;

    public ItemTextStyle(
        float padding,
        float rightOffset,
        float bottomOffset,
        int maxFontSize,
        Vector2 outlineDistance,
        TextAnchor alignment = TextAnchor.LowerRight,
        int minFontSize = 6,
        float leftOffset = float.NaN,
        float topOffset = float.NaN,
        HorizontalWrapMode horizontalOverflow = HorizontalWrapMode.Overflow,
        VerticalWrapMode verticalOverflow = VerticalWrapMode.Overflow,
        float lineSpacing = 1f,
        bool bestFit = true)
    {
        Padding = padding;
        LeftOffset = float.IsNaN(leftOffset) ? padding : leftOffset;
        RightOffset = rightOffset;
        BottomOffset = bottomOffset;
        TopOffset = float.IsNaN(topOffset) ? padding : topOffset;
        MaxFontSize = maxFontSize;
        MinFontSize = minFontSize;
        OutlineDistance = outlineDistance;
        Alignment = alignment;
        HorizontalOverflow = horizontalOverflow;
        VerticalOverflow = verticalOverflow;
        LineSpacing = lineSpacing;
        BestFit = bestFit;
    }
}

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
using UnityEngine;

public class Settings
{
    public const int CURRENT_VERSION = 1;

    public static readonly Color Green = new Color(0f, 1f, 0f, 100f / 255f);
    public static readonly Color Red = new Color(1f, 0f, 0f, 100f / 255f);
    public static readonly Color Yellow = new Color(1f, 1f, 0f, 100f / 255f);
    public static readonly Color LightBlue = new Color(0.4f, 0.6f, 1f, 100f / 255f);
    public static readonly Vector3 centerOfScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

    public static readonly Color ItemRarityCommonColor = new Color(0.9f, 0.9f, 0.9f, 0.25f);
    public static readonly Color ItemRarityUncommonColor = new Color(0.55f, 0.85f, 0.55f, 0.25f);
    public static readonly Color ItemRarityRareColor = new Color(0.55f, 0.7f, 0.95f, 0.25f);
    public static readonly Color ItemRarityEpicColor = new Color(0.85f, 0.55f, 0.9f, 0.25f);
    public static readonly Color ItemRarityLegendaryColor = new Color(0.95f, 0.8f, 0.55f, 0.25f);
    public static readonly Color ItemRarityArtifactColor = new Color(0.95f, 0.6f, 0.6f, 0.25f);

    public const float gridTileSizeWidth = 20f;
    public const float gridTileSizeHeight = 20f;

}

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

namespace Cholopol.TIS.Debug
{
    public enum DebugChannel
    {
        InventoryFlow,
        ViewLifecycle,
        ViewModelLifecycle,
        UIOverlay,
        CacheSync,
        SaveLoad,
        Other
    }

    public enum DebugLevel
    {
        Verbose,
        Info,
        Warning,
        Error
    }

    public static class GameDebug
    {
        private static int _nextChainId = 1;

        private static string NextChainId()
        {
            var id = _nextChainId;
            _nextChainId++;
            if (_nextChainId == int.MaxValue)
                _nextChainId = 1;
            return id.ToString("D5");
        }

        public static string NewChain(string prefix)
        {
            var suffix = NextChainId();
            if (string.IsNullOrEmpty(prefix))
                return suffix;
            return prefix + "#" + suffix;
        }

        public static string NewChain(DebugChannel channel)
        {
            return NextChainId();
        }

        private static string GetChannelColor(DebugChannel channel)
        {
            switch (channel)
            {
                case DebugChannel.InventoryFlow:
                    return "#00FFFF";
                case DebugChannel.ViewLifecycle:
                    return "#00FF00";
                case DebugChannel.ViewModelLifecycle:
                    return "#FF00FF";
                case DebugChannel.UIOverlay:
                    return "#FFA500";
                case DebugChannel.CacheSync:
                    return "#CCCCFF";
                case DebugChannel.SaveLoad:
                    return "#4FC3F7";
                default:
                    return "#FFFFFF";
            }
        }

        private static string GetLevelPrefix(DebugLevel level)
        {
            switch (level)
            {
                case DebugLevel.Verbose:
                    return "V/";
                case DebugLevel.Warning:
                    return "⚠️";
                case DebugLevel.Error:
                    return "X";
                default:
                    return "";
            }
        }

        public static void Log(DebugChannel channel, DebugLevel level, string message, Object context = null, string chainId = null)
        {
            var center = DebugCenter.Instance;
            if (center != null)
            {
                if (!center.IsChannelEnabled(channel))
                    return;
                if (!center.IsLevelEnabled(level))
                    return;
            }

            string scopePrefix;
            if (!string.IsNullOrEmpty(chainId))
                scopePrefix = "[" + channel + "#" + chainId + "][" + level + "]";
            else
                scopePrefix = "[" + channel + "][" + level + "]";

            var levelPrefix = GetLevelPrefix(level);
            if (!string.IsNullOrEmpty(levelPrefix))
                scopePrefix = levelPrefix + " " + scopePrefix;

            var color = GetChannelColor(channel);
            var coloredPrefix = "<color=" + color + ">" + scopePrefix + "</color>";

            var text = coloredPrefix + " " + message;

            switch (level)
            {
                case DebugLevel.Warning:
                    UnityEngine.Debug.LogWarning(text, context);
                    break;
                case DebugLevel.Error:
                    UnityEngine.Debug.LogError(text, context);
                    break;
                default:
                    UnityEngine.Debug.Log(text, context);
                    break;
            }
        }

        public static void LogBlockHeader(DebugChannel channel, DebugLevel level, string title, Object context = null, string chainId = null)
        {
            var header = "╔════════════════════════════════════════════════════════════";
            Log(channel, level, header, context, chainId);
            if (!string.IsNullOrEmpty(title))
            {
                Log(channel, level, "║ " + title, context, chainId);
            }
        }

        public static void LogBlockLine(DebugChannel channel, DebugLevel level, string message, Object context = null, string chainId = null)
        {
            Log(channel, level, "║ " + message, context, chainId);
        }

        public static void LogBlockFooter(DebugChannel channel, DebugLevel level, string message = null, Object context = null, string chainId = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Log(channel, level, "║ " + message, context, chainId);
            }
            var footer = "╚════════════════════════════════════════════════════════════";
            Log(channel, level, footer, context, chainId);
        }
    }
}

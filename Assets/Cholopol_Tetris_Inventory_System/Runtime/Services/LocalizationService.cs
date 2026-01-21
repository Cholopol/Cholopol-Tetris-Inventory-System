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

using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Cholopol.TIS.Events;

namespace Cholopol.TIS.Services
{
    /// <summary>
    /// Monitor language changes and broadcast them via EventBus.
    /// </summary>
    public static class LocalizationService
    {
        private static bool _initialized;

        /// <summary>
        /// Initialize service (called once when the game starts)
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public static void ChangeLanguage(Locale locale)
        {
            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
        }

        public static void ChangeLanguage(int index)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (index >= 0 && index < locales.Count)
                ChangeLanguage(locales[index]);
        }

        private static void OnLocaleChanged(Locale newLocale)
        {
            EventBus.Instance.Publish(EventNames.LanguageChangedEvent);
        }
    }
}

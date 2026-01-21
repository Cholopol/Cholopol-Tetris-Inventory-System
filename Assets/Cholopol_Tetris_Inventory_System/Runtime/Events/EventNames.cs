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

namespace Cholopol.TIS.Events
{
    public static class EventNames
    {
        public const string StartNewGameEvent = nameof(StartNewGameEvent);
        public const string ContinueGameEvent = nameof(ContinueGameEvent);
        public const string BegineGameEvent = nameof(BegineGameEvent);
        public const string StartGameEvent = nameof(StartGameEvent);
        public const string SaveGameEvent = nameof(SaveGameEvent);
        public const string DeleteDataEvent = nameof(DeleteDataEvent);
        public const string DeleteObjectEvent = nameof(DeleteObjectEvent);
        public const string InstantiateInventoryItemUI = nameof(InstantiateInventoryItemUI);
        public const string RecycleInventoryItemUI = nameof(RecycleInventoryItemUI);
        public const string LanguageChangedEvent = nameof(LanguageChangedEvent);
    }
}

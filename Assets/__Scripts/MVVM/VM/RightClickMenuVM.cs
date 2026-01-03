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
using Loxodon.Framework.Commands;
using Loxodon.Framework.Interactivity;
using Loxodon.Framework.ViewModels;
using Loxodon.Framework.Contexts;

namespace Cholopol.TIS.MVVM.ViewModels
{
    public class RightClickMenuVM : ViewModelBase
    {
        private TetrisItemVM _currentItem;
        public TetrisItemVM CurrentItem { get => _currentItem; set => Set(ref _currentItem, value); }

        public readonly InteractionRequest<ItemDetails> ShowInfoRequest = new();
        public readonly InteractionRequest<TetrisItemVM> OpenPanelRequest = new();
        public readonly InteractionRequest<object> CloseRequest = new();

        public SimpleCommand CheckCommand { get; private set; }
        public SimpleCommand SplitCommand { get; private set; }
        public SimpleCommand UseCommand { get; private set; }
        public SimpleCommand OpenCommand { get; private set; }

        public RightClickMenuVM()
        {
            CheckCommand = new SimpleCommand(OnCheck);
            SplitCommand = new SimpleCommand(OnSplit);
            UseCommand = new SimpleCommand(OnUse);
            OpenCommand = new SimpleCommand(OnOpen);
        }

        private void OnCheck()
        {
            var details = _currentItem != null ? _currentItem.ItemDetails : null;
            ShowInfoRequest.Raise(details);
            CloseRequest.Raise(null);
        }

        private void OnSplit()
        {
            var item = _currentItem;
            if (item == null) return;
            int amount = item.CurrentStack / 2;
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            if (service != null)
            {
                service.TrySplit(item, amount);
            }
            CloseRequest.Raise(null);
        }

        private void OnUse()
        {
            if (_currentItem == null) return;
            CloseRequest.Raise(null);
        }

        private void OnOpen()
        {
            var item = _currentItem;
            if (item == null) return;
            OpenPanelRequest.Raise(item);
            CloseRequest.Raise(null);
        }
    }
}

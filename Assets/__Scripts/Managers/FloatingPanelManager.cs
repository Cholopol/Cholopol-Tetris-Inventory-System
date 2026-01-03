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
using System.Collections.Generic;
using UnityEngine;
using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.Windows;

public class FloatingPanelManager : Singleton<FloatingPanelManager>
{
    public Canvas targetCanvas;
    public GameObject floatingPanelParent;
    public int maxConcurrentPanels = 10;

    private readonly Dictionary<string, FloatingTetrisGridWindow> _itemWindows = new Dictionary<string, FloatingTetrisGridWindow>();
    private readonly LinkedList<string> _openOrder = new LinkedList<string>();
    private Transform ParentTransform => floatingPanelParent != null ? floatingPanelParent.transform : (targetCanvas != null ? targetCanvas.transform : null);

    public bool IsItemWindowOpen(TetrisItemVM vm)
    {
        if (vm == null) return false;
        return _itemWindows.ContainsKey(vm.Guid);
    }

    public void OpenItemWindow(TetrisItemVM vm)
    {
        if (vm == null) return;
        if (ParentTransform == null)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null) targetCanvas = canvas;
        }

        var guid = vm.Guid;
        if (_itemWindows.TryGetValue(guid, out var existing))
        {
            BringToFront(existing.transform);
            return;
        }

        if (maxConcurrentPanels > 0)
        {
            // include incoming window
            var desiredCount = _itemWindows.Count + 1; 
            var excess = desiredCount - maxConcurrentPanels;
            if (excess > 0)
            {
                for (int i = 0; i < excess; i++)
                {
                    var first = _openOrder.First;
                    if (first == null) break;
                    var oldestGuid = first.Value;
                    if (_itemWindows.TryGetValue(oldestGuid, out var oldestWindow) && oldestWindow != null)
                    {
                        var transition = oldestWindow.Dismiss();
                    }
                    else
                    {
                        _openOrder.RemoveFirst();
                        _itemWindows.Remove(oldestGuid);
                    }
                }
            }
        }

        var window = FloatingTetrisGridWindow.Open(vm);
        if (window == null) return;
        window.OnDismissed += (s, e) =>
        {
            var removeGuid = vm.Guid;
            _itemWindows.Remove(removeGuid);
            var node = _openOrder.Find(removeGuid);
            if (node != null) _openOrder.Remove(node);
        };
        _itemWindows[guid] = window;
        _openOrder.AddLast(guid);
    }



    private void BringToFront(Transform panelTransform)
    {
        if (panelTransform == null) return;
        panelTransform.SetAsLastSibling();
        foreach (var kv in _itemWindows)
        {
            if (kv.Value != null && kv.Value.transform == panelTransform)
            {
                var node = _openOrder.Find(kv.Key);
                if (node != null)
                {
                    _openOrder.Remove(node);
                    _openOrder.AddLast(kv.Key);
                }
                break;
            }
        }
    }
}

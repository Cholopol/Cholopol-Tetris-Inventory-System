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
using Cholopol.TIS;
using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.Windows;

/// <summary>
/// A unified focus window manager that manages all windows that implement IFocusableWindow.
/// </summary>
public class FloatingPanelManager : Singleton<FloatingPanelManager>
{
    public Canvas targetCanvas;
    public GameObject floatingPanelParent;
    public int maxConcurrentPanels = 10;
    public int maxInfoPanels = 5;

    // A unified dictionary of focusable windows (including floating windows and item information windows).
    private readonly Dictionary<string, IFocusableWindow> _focusableWindows = new Dictionary<string, IFocusableWindow>();
    // Focus sequence stack
    private readonly LinkedList<string> _focusOrder = new LinkedList<string>();
    private IFocusableWindow _currentFocusedWindow;

    // Floating window collection
    private readonly Dictionary<string, FloatingTetrisGridWindow> _gridWindows = new Dictionary<string, FloatingTetrisGridWindow>();
    // Item information panel collection
    private readonly Dictionary<string, ItemInformationPanel> _infoPanels = new Dictionary<string, ItemInformationPanel>();

    #region General Focusable Window Management

    /// <summary>
    /// Register a focusable window to the manager
    /// </summary>
    public void RegisterFocusableWindow(IFocusableWindow window)
    {
        if (window == null || string.IsNullOrEmpty(window.UniqueId)) return;
        
        var id = window.UniqueId;
        if (_focusableWindows.ContainsKey(id))
        {
            FocusFocusableWindow(window);
            return;
        }
        
        _focusableWindows[id] = window;
        _focusOrder.AddLast(id);
        
        window.OnFocusableDismissed += OnFocusableWindowDismissed;
        
        FocusFocusableWindow(window);
    }

    /// <summary>
    /// Unregister a focusable window
    /// </summary>
    public void UnregisterFocusableWindow(IFocusableWindow window)
    {
        if (window == null || string.IsNullOrEmpty(window.UniqueId)) return;
        
        var id = window.UniqueId;
        window.OnFocusableDismissed -= OnFocusableWindowDismissed;
        _focusableWindows.Remove(id);
        
        var node = _focusOrder.Find(id);
        if (node != null) _focusOrder.Remove(node);
    }

    /// <summary>
    /// Focus on the specified focusable window
    /// </summary>
    public void FocusFocusableWindow(IFocusableWindow window)
    {
        if (window == null) return;
        if (_currentFocusedWindow == window) return;
        
        var id = window.UniqueId;

        // Move the window to the top of the hierarchy
        if (window.WindowInstance != null)
        {
            window.WindowInstance.transform.SetAsLastSibling();
        }

        // Update focus order
        var node = _focusOrder.Find(id);
        if (node != null)
        {
            _focusOrder.Remove(node);
            _focusOrder.AddLast(id);
        }

        // Update the focus state of all windows
        foreach (var kv in _focusableWindows)
        {
            var w = kv.Value;
            if (w == null) continue;
            
            bool isFocused = (w == window);
            w.SetFocused(isFocused);
            w.WindowInstance?.Activate(isFocused);
        }
        
        _currentFocusedWindow = window;
    }
    
    private void OnFocusableWindowDismissed(object sender, EventArgs e)
    {
        var window = sender as IFocusableWindow;
        if (window == null) return;
        
        var id = window.UniqueId;
        
        _focusableWindows.Remove(id);
        var node = _focusOrder.Find(id);
        if (node != null) _focusOrder.Remove(node);
        
        _gridWindows.Remove(id);
        _infoPanels.Remove(id);

        // Focus on the next window
        if (_focusOrder.Last != null)
        {
            var lastId = _focusOrder.Last.Value;
            if (_focusableWindows.TryGetValue(lastId, out var lastWindow))
            {
                _currentFocusedWindow = null; // Reset to allow re-focus
                FocusFocusableWindow(lastWindow);
            }
        }
        else
        {
            _currentFocusedWindow = null;
        }
    }

    #endregion

    #region Floating grid window management

    /// <summary>
    /// Check if the item's floating window is open.
    /// </summary>
    public bool IsGridWindowOpen(TetrisItemVM vm)
    {
        if (vm == null) return false;
        return _gridWindows.ContainsKey(vm.Guid);
    }

    /// <summary>
    /// Register floating grid windows (for quantity limits)
    /// </summary>
    public void RegisterGridWindow(FloatingTetrisGridWindow window)
    {
        if (window == null || string.IsNullOrEmpty(window.UniqueId)) return;
        
        if (maxConcurrentPanels > 0 && _gridWindows.Count >= maxConcurrentPanels)
        {
            // Find the oldest floating window and close it.
            string oldestId = null;
            foreach (var nodeId in _focusOrder)
            {
                if (_gridWindows.ContainsKey(nodeId))
                {
                    oldestId = nodeId;
                    break;
                }
            }
            if (oldestId != null && _gridWindows.TryGetValue(oldestId, out var oldestWindow))
            {
                oldestWindow.Dismiss();
            }
        }
        
        _gridWindows[window.UniqueId] = window;
        RegisterFocusableWindow(window);
    }

    #endregion

    #region Item Information Panel Management

    /// <summary>
    /// Registered item information panel (for quantity limits)
    /// </summary>
    public void RegisterInfoPanel(ItemInformationPanel panel)
    {
        if (panel == null || string.IsNullOrEmpty(panel.UniqueId)) return;
        
        if (maxInfoPanels > 0 && _infoPanels.Count >= maxInfoPanels)
        {
            string oldestId = null;
            foreach (var nodeId in _focusOrder)
            {
                if (_infoPanels.ContainsKey(nodeId))
                {
                    oldestId = nodeId;
                    break;
                }
            }
            if (oldestId != null && _infoPanels.TryGetValue(oldestId, out var oldestPanel))
            {
                oldestPanel.Dismiss();
            }
        }
        
        _infoPanels[panel.UniqueId] = panel;
        RegisterFocusableWindow(panel);
    }
    
    #endregion
}

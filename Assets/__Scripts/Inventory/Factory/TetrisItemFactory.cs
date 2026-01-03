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

using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.MVVM.Views;
using Cholopol.TIS.SaveLoadSystem;
using Cholopol.TIS.Utility;
using Loxodon.Framework.Binding;
using System;
using System.Collections.Generic;

namespace Cholopol.TIS
{
    public static class TetrisItemFactory
    {
        private const string ResourcePath = "Prefabs/Items/GeneralTetrisItemPrefab";
        private static UnityEngine.GameObject _cachedPrefab;
        private static Dictionary<string, TetrisItemVM> _vmRegistry = new();
        private static Dictionary<string, List<TetrisItemView>> _viewRegistry = new();
        public static Dictionary<string, TetrisItemVM> VmRegistry { get => _vmRegistry; }
        public static Dictionary<string, List<TetrisItemView>> ViewRegistry { get => _viewRegistry; }

        private static UnityEngine.GameObject GetPrefab()
        {
            if (_cachedPrefab != null) return _cachedPrefab;
            _cachedPrefab = UnityEngine.Resources.Load<UnityEngine.GameObject>(ResourcePath);
            if (_cachedPrefab == null)
            {
                UnityEngine.Debug.LogError("Failed to load GeneralTetrisItemPrefab from Resources.");
            }
            return _cachedPrefab;
        }

        private static UnityEngine.GameObject AcquireGameObject()
        {
            var prefab = GetPrefab();
            if (prefab == null) return null;
            UnityEngine.GameObject go;
            if (PoolManager.Instance != null)
            {
                go = PoolManager.Instance.GetObject(prefab);
            }
            else
            {
                go = UnityEngine.Object.Instantiate(prefab);
            }

            ResetPooledTransform(go);
            return go;
        }

        private static void ResetPooledTransform(UnityEngine.GameObject go)
        {
            if (go == null) return;
            var t = go.transform;
            t.localScale = UnityEngine.Vector3.one;
            t.localRotation = UnityEngine.Quaternion.identity;

            var rt = go.GetComponent<UnityEngine.RectTransform>();
            if (rt == null) return;
            rt.anchoredPosition3D = UnityEngine.Vector3.zero;
            rt.localScale = UnityEngine.Vector3.one;
            rt.localRotation = UnityEngine.Quaternion.identity;
        }

        public static void ReleaseView(TetrisItemView view)
        {
            if (view == null) return;

            bool alreadyInPool = PoolManager.Instance != null && PoolManager.Instance.IsPooled(view);
            var vm = view.ViewModel;
            var guid = vm != null ? vm.Guid : null;

            view.DestroyGridPanel();
            view.SetDataContext(null);
            view.ViewModel = null;

            if (!string.IsNullOrEmpty(guid))
            {
                UnregisterView(guid, view);
            }

            if (PoolManager.Instance != null)
            {
                if (!alreadyInPool || view.gameObject.activeSelf)
                {
                    PoolManager.Instance.PushObject(view.gameObject);
                }
            }
            else
            {
                UnityEngine.Object.Destroy(view.gameObject);
            }
        }

        public static TetrisItemVM GetOrCreateVM(ItemDetails details, TetrisItemPersistentData data, TetrisItemContainerVM container)
        {
            if (data == null)
            {
                var vmNew = new TetrisItemVM(details, null, container);
                RegisterVM(vmNew.Guid, vmNew);
                return vmNew;
            }
            var guid = data.itemGuid;
            if (string.IsNullOrEmpty(guid))
            {
                guid = System.Guid.NewGuid().ToString();
                data.itemGuid = guid;
            }

            if (TryGetVM(guid, out var existing) && existing != null)
            {
                existing.ItemDetails = details;
                existing.CurrentStack = data.stack > 0 ? data.stack : 1;
                existing.Direction = data.direction;
                existing.LocalGridCoordinate = data.orginPosition;
                existing.Rotated = TetrisUtilities.RotationHelper.IsRotated(existing.Direction);
                existing.RotationOffset = TetrisUtilities.RotationHelper.GetRotationOffset(existing.Direction, existing.Width, existing.Height);
                if (container != null)
                {
                    existing.CurrentTetrisContainer = container;
                }
                return existing;
            }

            var vm = new TetrisItemVM(details, data, container);
            RegisterVM(guid, vm);
            return vm;
        }

        public static void RegisterVM(string guid, TetrisItemVM vm)
        {
            if (string.IsNullOrEmpty(guid) || vm == null) return;
            _vmRegistry[guid] = vm;
        }

        public static void RegisterView(string guid, TetrisItemView view)
        {
            if (string.IsNullOrEmpty(guid) || view == null) return;
            if (!_viewRegistry.TryGetValue(guid, out var list))
            {
                list = new List<TetrisItemView>();
                _viewRegistry[guid] = list;
            }
            if (!list.Contains(view)) list.Add(view);
        }

        public static bool TryGetVM(string guid, out TetrisItemVM vm)
        {
            return _vmRegistry.TryGetValue(guid, out vm);
        }

        public static bool TryGetView(string guid, out TetrisItemView view)
        {
            view = null;
            if (_viewRegistry.TryGetValue(guid, out var list) && list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v != null) { view = v; return true; }
                }
            }
            return false;
        }

        public static bool TryGetViews(string guid, out List<TetrisItemView> views)
        {
            views = null;
            if (_viewRegistry.TryGetValue(guid, out var list) && list != null && list.Count > 0)
            {
                views = list;
                return true;
            }
            return false;
        }

        public static bool TryGetView(TetrisItemVM vm, out TetrisItemView view)
        {
            view = null;
            var guid = vm != null ? vm.Guid : null;
            if (string.IsNullOrEmpty(guid)) return false;
            return TryGetView(guid, out view);
        }

        public static bool TryGetViews(TetrisItemVM vm, out List<TetrisItemView> views)
        {
            views = null;
            var guid = vm != null ? vm.Guid : null;
            if (string.IsNullOrEmpty(guid)) return false;
            return TryGetViews(guid, out views);
        }
        public static bool TryGetVMByView(TetrisItemView view, out TetrisItemVM vm)
        {
            vm = null;
            if (view == null) return false;
            vm = view.ViewModel;
            if (vm == null) return false;
            var guid = vm.Guid;
            if (string.IsNullOrEmpty(guid)) return false;
            if (_vmRegistry.TryGetValue(guid, out var existing) && existing != null) { vm = existing; return true; }
            return true;
        }

        public static void UnregisterView(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;
            _viewRegistry.Remove(guid);
        }

        public static void UnregisterView(string guid, TetrisItemView view)
        {
            if (string.IsNullOrEmpty(guid) || view == null) return;
            if (_viewRegistry.TryGetValue(guid, out var list) && list != null)
            {
                list.Remove(view);
                if (list.Count == 0) _viewRegistry.Remove(guid);
            }
        }

        public static void UnregisterVM(string guid, bool removeViews = false)
        {
            if (string.IsNullOrEmpty(guid)) return;
            _vmRegistry.Remove(guid);
            if (removeViews) _viewRegistry.Remove(guid);
        }

        public static void UnregisterVM(TetrisItemVM vm, bool removeViews = false)
        {
            var guid = vm != null ? vm.Guid : null;
            if (string.IsNullOrEmpty(guid)) return;
            UnregisterVM(guid, removeViews);
        }

        public static bool TryGetVMAndViews(string guid, out TetrisItemVM vm, out List<TetrisItemView> views)
        {
            vm = null;
            views = null;
            bool vmOk = TryGetVM(guid, out vm);
            bool viewsOk = TryGetViews(guid, out views);
            return vmOk || viewsOk;
        }

        public static int UnregisterAndDestroyUIByGuid(string guid, bool searchSceneFallback = true)
        {
            if (string.IsNullOrEmpty(guid)) return 0;

            int destroyed = 0;
            var releasedSet = new HashSet<TetrisItemView>();

            if (_viewRegistry.TryGetValue(guid, out var list) && list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v != null)
                    {
                        releasedSet.Add(v);
                        ReleaseView(v);
                        destroyed++;
                    }
                }
                _viewRegistry.Remove(guid);
            }

            if (searchSceneFallback)
            {
                var allViews = UnityEngine.Object.FindObjectsOfType<TetrisItemView>(true);
                for (int i = 0; i < allViews.Length; i++)
                {
                    var v = allViews[i];
                    if (releasedSet.Contains(v)) continue;
                    var vmv = v.ViewModel;
                    if (vmv != null && vmv.Guid == guid)
                    {
                        ReleaseView(v);
                        destroyed++;
                    }
                }
            }

            if (_vmRegistry.TryGetValue(guid, out var vm) && vm != null)
            {
                vm.Dispose();
            }
            _vmRegistry.Remove(guid);
            return destroyed;
        }

        public static string RegisterAssociation(TetrisItemView view, TetrisItemVM vm)
        {
            if (view == null || vm == null) return string.Empty;
            var guid = !string.IsNullOrEmpty(vm.Guid) ? vm.Guid : System.Guid.NewGuid().ToString();
            vm.Guid = guid;
            RegisterVM(guid, vm);
            RegisterView(guid, view);
            return guid;
        }

        public static TetrisItemView CreateViewByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var gridItem = InventorySaveLoadService.Instance.inventoryData_SO.GetTetrisItemPersistentDataByGuid(guid);
            if (gridItem == null) return null;
            var details = InventorySaveLoadService.Instance.itemDataList_SO.GetItemDetailsByID(gridItem.itemID);
            var go = AcquireGameObject();
            if (go == null) return null;
            var view = go.GetComponent<TetrisItemView>();
            if (view == null) view = go.AddComponent<TetrisItemView>();

            var vm = GetOrCreateVM(details, gridItem, null);
            view.ViewModel = vm;
            if (view.ViewModel == null) return null;
            RegisterView(guid, view);
            return view;
        }

        public static bool BindViewToGuid(TetrisItemView view, string guid)
        {
            if (view == null || string.IsNullOrEmpty(guid)) return false;
            
            var gridItem = InventorySaveLoadService.Instance.inventoryData_SO.GetTetrisItemPersistentDataByGuid(guid);
            if (gridItem == null) return false;
            var details = InventorySaveLoadService.Instance.itemDataList_SO.GetItemDetailsByID(gridItem.itemID);
            
            var vm = GetOrCreateVM(details, gridItem, null);
            view.ViewModel = vm;
            if (view.ViewModel == null) return false;
            RegisterView(guid, view);
            return true;
        }


    }
}

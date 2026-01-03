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
using Cholopol.TIS.MVVM.Views;

namespace Cholopol.TIS
{
    public static class TetrisGridFactory
    {
        private static readonly Dictionary<string, TetrisGridVM> _vmRegistry = new();
        private static readonly Dictionary<string, List<TetrisGridView>> _viewRegistry = new();

        public static Dictionary<string, TetrisGridVM> VmRegistry { get => _vmRegistry; }
        public static Dictionary<string, List<TetrisGridView>> ViewRegistry { get => _viewRegistry; }

        public static void RegisterVM(string guid, TetrisGridVM vm)
        {
            if (string.IsNullOrEmpty(guid) || vm == null) return;
            _vmRegistry[guid] = vm;
        }

        public static void RegisterView(string guid, TetrisGridView view)
        {
            if (string.IsNullOrEmpty(guid) || view == null) return;
            if (!_viewRegistry.TryGetValue(guid, out var list))
            {
                list = new List<TetrisGridView>();
                _viewRegistry[guid] = list;
            }
            if (!list.Contains(view)) list.Add(view);
        }

        public static bool TryGetVM(string guid, out TetrisGridVM vm)
        {
            return _vmRegistry.TryGetValue(guid, out vm);
        }

        public static bool TryGetView(string guid, out TetrisGridView view)
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

        public static bool TryGetViews(string guid, out List<TetrisGridView> views)
        {
            views = null;
            if (_viewRegistry.TryGetValue(guid, out var list) && list != null && list.Count > 0)
            {
                views = list;
                return true;
            }
            return false;
        }

        public static bool TryGetVMAndViews(string guid, out TetrisGridVM vm, out List<TetrisGridView> views)
        {
            vm = null;
            views = null;
            bool vmOk = TryGetVM(guid, out vm);
            bool viewsOk = TryGetViews(guid, out views);
            return vmOk || viewsOk;
        }

        public static void UnregisterView(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;
            _viewRegistry.Remove(guid);
        }

        public static void UnregisterView(string guid, TetrisGridView view)
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

        public static void UnregisterVM(TetrisGridVM vm, bool removeViews = false)
        {
            var guid = vm != null ? vm.GridGuid : null;
            if (string.IsNullOrEmpty(guid)) return;
            UnregisterVM(guid, removeViews);
        }

        public static int UnregisterAndDestroyUIByGuid(string guid, bool searchSceneFallback = true)
        {
            if (string.IsNullOrEmpty(guid)) return 0;

            int destroyed = 0;
            var destroyedSet = new HashSet<TetrisGridView>();

            if (_viewRegistry.TryGetValue(guid, out var list) && list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v != null)
                    {
                        destroyedSet.Add(v);
                        UnityEngine.Object.Destroy(v.gameObject);
                        destroyed++;
                    }
                }
                _viewRegistry.Remove(guid);
            }

            if (searchSceneFallback)
            {
                var allViews = UnityEngine.Object.FindObjectsOfType<TetrisGridView>(true);
                for (int i = 0; i < allViews.Length; i++)
                {
                    var v = allViews[i];
                    if (destroyedSet.Contains(v)) continue;
                    var guidComp = v.gameObject.GetComponent<DataGUID>();
                    if (guidComp != null && guidComp.guid == guid)
                    {
                        UnityEngine.Object.Destroy(v.gameObject);
                        destroyed++;
                    }
                }
            }

            if (_vmRegistry.TryGetValue(guid, out var vm) && vm != null)
            {
                _vmRegistry.Remove(guid);
            }
            return destroyed;
        }

        public static string RegisterAssociation(TetrisGridView view, TetrisGridVM vm)
        {
            if (view == null || vm == null) return string.Empty;
            var guid = !string.IsNullOrEmpty(vm.GridGuid) ? vm.GridGuid : System.Guid.NewGuid().ToString();
            vm.GridGuid = guid;
            var guidComp = view.gameObject.GetComponent<DataGUID>();
            if (guidComp == null) guidComp = view.gameObject.AddComponent<DataGUID>();
            guidComp.guid = guid;
            RegisterVM(guid, vm);
            RegisterView(guid, view);
            return guid;
        }

        public static TetrisGridView BindView(TetrisGridView view)
        {
            if (view == null) return null;
            var guidComp = view.gameObject.GetComponent<DataGUID>();
            if (guidComp == null) guidComp = view.gameObject.AddComponent<DataGUID>();
            if (string.IsNullOrEmpty(guidComp.guid)) guidComp.guid = System.Guid.NewGuid().ToString();

            if (!TryGetVM(guidComp.guid, out var vm))
            {
                vm = new TetrisGridVM(1, 1);
                RegisterVM(guidComp.guid, vm);
            }

            view.ViewModel = vm;
            RegisterView(guidComp.guid, view);
            return view;
        }

        public static bool BindViewToGuid(TetrisGridView view, string guid)
        {
            if (view == null || string.IsNullOrEmpty(guid)) return false;
            if (!TryGetVM(guid, out var vm))
            {
                vm = new TetrisGridVM(1, 1);
                RegisterVM(guid, vm);
            }
            var guidComp = view.gameObject.GetComponent<DataGUID>();
            if (guidComp == null) guidComp = view.gameObject.AddComponent<DataGUID>();
            guidComp.guid = guid;
            view.ViewModel = vm;
            RegisterView(guid, view);
            return true;
        }

        public static TetrisGridView CreateViewByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var allViews = UnityEngine.Object.FindObjectsOfType<TetrisGridView>(true);
            for (int i = 0; i < allViews.Length; i++)
            {
                var v = allViews[i];
                var guidComp = v.gameObject.GetComponent<DataGUID>();
                if (guidComp != null && guidComp.guid == guid)
                {
                    if (!TryGetVM(guid, out var vm))
                    {
                        vm = new TetrisGridVM(1, 1);
                        RegisterVM(guid, vm);
                    }
                    v.ViewModel = vm;
                    RegisterView(guid, v);
                    return v;
                }
            }
            return null;
        }
    }
}

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

namespace Cholopol.TIS.Utility
{
    public class PoolManager : Singleton<PoolManager>, IPoolService
    {
        private Dictionary<string, Queue<GameObject>> objectPool = new();
        private GameObject pool;

        public GameObject GetObject(GameObject prefab)
        {
            GameObject _object = null;
            string poolKey = prefab.name;

            if (objectPool.ContainsKey(poolKey) && objectPool[poolKey].Count > 0)
            {
                while (objectPool[poolKey].Count > 0)
                {
                    _object = objectPool[poolKey].Dequeue();
                    if (_object != null)
                    {
                        _object.SetActive(true);
                        return _object;
                    }
                }
            }

            _object = Instantiate(prefab);
            EnsurePoolHierarchy(prefab.name);
            GameObject childPool = GetChildPool(prefab.name);
            if (childPool) _object.transform.SetParent(childPool.transform);

            return _object;
        }

        public void PushObject(GameObject obj)
        {
            if (obj == null) return;

            string _name = obj.name.Replace("(Clone)", string.Empty);
            if (!objectPool.ContainsKey(_name))
            {
                objectPool.Add(_name, new Queue<GameObject>());
            }
            
            objectPool[_name].Enqueue(obj);
            obj.SetActive(false);

            GameObject childPool = GetChildPool(_name);
            if (childPool != null)
            {
                obj.transform.SetParent(childPool.transform);
            }
            else
            {
                if (pool == null) EnsurePoolHierarchy(_name);
                if (pool != null) obj.transform.SetParent(pool.transform);
            }
        }

        public bool IsPooled(Component component)
        {
            if (component == null) return false;
            return component.transform.IsChildOf(this.transform);
        }

        private void EnsurePoolHierarchy(string prefabName)
        {
            if (pool == null)
            {
                pool = new GameObject("ObjectPool");
                pool.transform.SetParent(this.transform);
            }
            GetChildPool(prefabName);
        }

        private GameObject GetChildPool(string prefabName)
        {
            if (pool == null) return null;
            Transform t = pool.transform.Find(prefabName + "Pool");
            if (t != null) return t.gameObject;

            GameObject childPool = new GameObject(prefabName + "Pool");
            childPool.transform.SetParent(pool.transform);
            return childPool;
        }

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register<IPoolService>(this);
        }
    }
}

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
using Cholopol.TIS.Events;
using Cholopol.TIS.SaveLoadSystem;
using Cholopol.TIS.Utility;

namespace Cholopol.TIS
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }

        public static bool TryResolve<T>(out T instance) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                instance = (T)s;
                return true;
            }
            instance = AutoResolve<T>();
            return instance != null;
        }

        public static T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                return (T)s;
            }
            var fallback = AutoResolve<T>();
            if (fallback == null)
            {
                throw new InvalidOperationException($"Service not found: {typeof(T).Name}");
            }
            return fallback;
        }

        private static T AutoResolve<T>() where T : class
        {
            var t = typeof(T);
            if (t == typeof(IPoolService) && PoolManager.Instance != null)
            {
                return (T)(object)PoolManager.Instance;
            }
            if (t == typeof(ISaveLoadService) && SaveLoadManager.Instance != null)
            {
                return (T)(object)SaveLoadManager.Instance;
            }
            if (t == typeof(IEventBus))
            {
                return (T)(object)EventBus.Instance;
            }
            return null;
        }
    }
}
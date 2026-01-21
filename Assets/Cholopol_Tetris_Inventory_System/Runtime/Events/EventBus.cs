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

namespace Cholopol.TIS.Events
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _subscriptions = new();
        private readonly Dictionary<string, Delegate> _namedSubscriptions = new();
        private readonly Dictionary<string, Type> _namedParamType = new();

        public static readonly EventBus Instance = new EventBus();

        public void Publish<TEvent>(TEvent evt)
        {
            if (_subscriptions.TryGetValue(typeof(TEvent), out var d))
            {
                ((Action<TEvent>)d)?.Invoke(evt);
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var t = typeof(TEvent);
            if (_subscriptions.TryGetValue(t, out var d))
            {
                _subscriptions[t] = Delegate.Combine(d, handler);
            }
            else
            {
                _subscriptions[t] = handler;
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var t = typeof(TEvent);
            if (_subscriptions.TryGetValue(t, out var d))
            {
                var nd = Delegate.Remove(d, handler);
                if (nd == null)
                {
                    _subscriptions.Remove(t);
                }
                else
                {
                    _subscriptions[t] = nd;
                }
            }
        }

        public void Publish(string eventName)
        {
            if (_namedSubscriptions.TryGetValue(eventName, out var d))
            {
                if (!_namedParamType.TryGetValue(eventName, out var t) || t != typeof(void))
                    throw new NotSupportedException($"##Event@ {eventName} require parameters");
                ((Action)d)?.Invoke();
            }
        }

        public void Publish<TArg>(string eventName, TArg arg)
        {
            if (_namedSubscriptions.TryGetValue(eventName, out var d))
            {
                if (!_namedParamType.TryGetValue(eventName, out var t) || t != typeof(TArg))
                    throw new NotSupportedException($"##Event@ {eventName} Parameter type mismatch");
                ((Action<TArg>)d)?.Invoke(arg);
            }
        }

        public void Subscribe(string eventName, Action handler)
        {
            if (_namedParamType.TryGetValue(eventName, out var t) && t != typeof(void))
                throw new NotSupportedException($"##Event@ {eventName} Delegation type is not Action");
            _namedParamType[eventName] = typeof(void);
            if (_namedSubscriptions.TryGetValue(eventName, out var d))
                _namedSubscriptions[eventName] = Delegate.Combine(d, handler);
            else
                _namedSubscriptions[eventName] = handler;
        }

        public void Subscribe<TArg>(string eventName, Action<TArg> handler)
        {
            if (_namedParamType.TryGetValue(eventName, out var t) && t != typeof(TArg))
                throw new NotSupportedException($"##Event@ {eventName} Mismatch in delegation type");
            _namedParamType[eventName] = typeof(TArg);
            if (_namedSubscriptions.TryGetValue(eventName, out var d))
                _namedSubscriptions[eventName] = Delegate.Combine(d, handler);
            else
                _namedSubscriptions[eventName] = handler;
        }

        public void Unsubscribe(string eventName, Action handler)
        {
            if (_namedSubscriptions.TryGetValue(eventName, out var d))
            {
                var nd = Delegate.Remove(d, handler);
                if (nd == null)
                {
                    _namedSubscriptions.Remove(eventName);
                    _namedParamType.Remove(eventName);
                }
                else
                {
                    _namedSubscriptions[eventName] = nd;
                }
            }
        }

        public void Unsubscribe<TArg>(string eventName, Action<TArg> handler)
        {
            if (_namedSubscriptions.TryGetValue(eventName, out var d))
            {
                var nd = Delegate.Remove(d, handler);
                if (nd == null)
                {
                    _namedSubscriptions.Remove(eventName);
                    _namedParamType.Remove(eventName);
                }
                else
                {
                    _namedSubscriptions[eventName] = nd;
                }
            }
        }
    }
}

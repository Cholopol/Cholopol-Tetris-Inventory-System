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

namespace Cholopol.TIS
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent evt);
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Publish(string eventName);
        void Publish<TArg>(string eventName, TArg arg);
        void Subscribe(string eventName, Action handler);
        void Subscribe<TArg>(string eventName, Action<TArg> handler);
        void Unsubscribe(string eventName, Action handler);
        void Unsubscribe<TArg>(string eventName, Action<TArg> handler);
    }
}

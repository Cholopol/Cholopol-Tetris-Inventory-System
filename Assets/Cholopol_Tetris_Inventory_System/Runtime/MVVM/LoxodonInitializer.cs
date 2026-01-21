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
using Cholopol.TIS;
using Cholopol.TIS.MVVM;
using Cholopol.TIS.SaveLoadSystem;
using Cholopol.TIS.Utility;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Binding.Binders;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Views;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class LoxodonInitializer : MonoBehaviour
{
    private ApplicationContext context;
    private BindingServiceBundle bundle;
    private IUIViewLocator locator;

    void Awake()
    {
        context = Context.GetApplicationContext();
        var container = context.GetContainer();

        if (context.GetService<IBinder>() == null)
        {
            bundle = new BindingServiceBundle(container);
            bundle.Start();
        }

        locator = context.GetService<IUIViewLocator>();
        if (locator == null)
        {
            container.Register<IUIViewLocator>(new DefaultUIViewLocator());
        }

        if (PoolManager.Instance != null)
        {
            container.Register<IPoolService>(PoolManager.Instance);
        }
        if (SaveLoadManager.Instance != null)
        {
            container.Register<ISaveLoadService>(SaveLoadManager.Instance);
        }
        container.Register<IInventoryService>(new InventoryService());
        container.Register<IInventoryTreeCache>(new InventoryTreeCache());

        var wmInAwake = FindObjectOfType<GlobalWindowManager>();
        Transform parent = wmInAwake != null ? wmInAwake.transform : null;

        WindowContainer floatingContainer = WindowContainer.Create("FLOATING");
        if (floatingContainer != null && parent != null)
            floatingContainer.transform.SetParent(parent, false);

    }


}

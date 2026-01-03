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

using Cholopol.TIS.MVVM.Views;
using UnityEngine;

namespace Cholopol.TIS
{
    /// <summary>
    /// Use a stable GUID to identify each intrinsic grid and declare its classification and persistence policy.
    /// </summary>
    [RequireComponent(typeof(TetrisGridView))]
    [RequireComponent(typeof(DataGUID))]
    public class InventoryGridDescriptor : MonoBehaviour
    {
        [Header("Classification (for dimension types)")]
        public PersistentGridType category = PersistentGridType.None;

        [Header("Persistence strategy")]
        [Tooltip("Whether to keep the items in the grid when the character dies (such as the safe)")]
        public bool retainedOnDeath = false;

        /// <summary>
        /// The grid unique identifier (stable GUID) comes from the Data GUID component.
        /// </summary>
        public string GridGuid => GetComponent<DataGUID>()?.guid;

        private void OnValidate()
        {
            // The default safe type is set to death reserve, which can be manually modified in the Inspector.
            if (category == PersistentGridType.Coffer)
            {
                retainedOnDeath = true;
            }
        }
    }
}

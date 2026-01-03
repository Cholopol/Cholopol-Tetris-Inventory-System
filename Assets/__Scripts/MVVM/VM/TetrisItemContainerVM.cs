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
using Loxodon.Framework.ViewModels;
using System.Collections.Generic;

namespace Cholopol.TIS
{
    /// <summary>
    /// Abstract base class for unified management of container behavior for TetrisItem
    /// </summary>
    public abstract class TetrisItemContainerVM : ViewModelBase
    {
        /// <summary>
        /// The TetrisItem associated with the current container
        /// </summary>
        public virtual TetrisItemVM RelatedTetrisItem { get; set; }

        /// <summary>
        /// Dictionary of items owned by the container (optional, mainly used for Grid type containers)
        /// </summary>
        public virtual Dictionary<string, TetrisItemVM> OwnerItemsDic { get; set; } = new();

        /// <summary>
        /// Attempt to place TetrisItem in the specified location
        /// </summary>
        /// <param name="tetrisItem">Items to be placed</param>
        /// <param name="posX">X coordinate (can be ignored for Slot type)</param>
        /// <param name="posY">Y coordinate (can be ignored for Slot type)</param>
        /// <returns>Is it successfully placed</returns>
        public abstract bool TryPlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0);

        /// <summary>
        /// Place TetrisItem into the container
        /// </summary>
        /// <param name="tetrisItem">Items to be placed</param>
        /// <param name="posX">X coordinate (can be ignored for Slot type)</param>
        /// <param name="posY">Y coordinate (can be ignored for Slot type)</param>
        public abstract void PlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0);

        /// <summary>
        /// Remove TetrisItem from the container
        /// </summary>
        public abstract void RemoveTetrisItem();

        /// <summary>
        /// Check if there are any items in the container
        /// </summary>
        /// <returns>Whether has item</returns>
        public virtual bool HasItem()
        {
            return RelatedTetrisItem != null;
        }

        
    }
}
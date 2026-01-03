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
using Cholopol.TIS.Events;
using UnityEngine;
using UnityEngine.UI;
using static Cholopol.TIS.TetrisUtilities;

namespace Cholopol.TIS.SaveLoadSystem
{
    public class SaveSlot : MonoBehaviour
    {
        public Text saveTime;
        [SerializeField] private Button delletBtn, saveBtn, startBtn;
        private DataSlot currentData;
        private int Index => transform.GetSiblingIndex();

        private void Awake()
        {
            startBtn.onClick.AddListener(LoadGameData);
            saveBtn.onClick.AddListener(SaveGameData);
            delletBtn.onClick.AddListener(DeleteGameData);
        }

        private void Start()
        {
            SetupSlotUI();

        }

        private void SetupSlotUI()
        {
            currentData = SaveLoadManager.Instance.dataSlots[Index];

            if (currentData != null)
            {
                saveTime.text = "Save Time: " + (SaveLoadManager.Instance.GetSlotTimestamp(Index) ?? string.Empty);
                delletBtn.gameObject.SetActive(true);
            }
            else
            {
                saveTime.text = "NUll";
                delletBtn.gameObject.SetActive(false);
                saveBtn.gameObject.SetActive(true);
            }
        }

        private void LoadGameData()
        {
            if (currentData != null)
            {
                EventBus.Instance.Publish<int>(EventNames.StartGameEvent, Index);
            }
        }

        private void SaveGameData()
        {
            EventBus.Instance.Publish<int>(EventNames.SaveGameEvent, Index);
            SetupSlotUI();
        }

        private void DeleteGameData()
        {
            if (currentData != null)
            {
                EventBus.Instance.Publish<int>(EventNames.DeleteDataEvent, Index);
                EventBus.Instance.Publish(EventNames.DeleteObjectEvent);
                SetupSlotUI();
            }

            InventoryLogicHelper.TriggerPointerEnter(InventoryManager.Instance.depositoryGridView.gameObject);
        }

    }
}

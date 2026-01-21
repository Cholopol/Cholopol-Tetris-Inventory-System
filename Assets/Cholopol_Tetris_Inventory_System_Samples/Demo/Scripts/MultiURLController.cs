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

using UnityEngine;
using UnityEngine.UI;

public class MultiURLController : MonoBehaviour
{
    [System.Serializable]
    public class ButtonURLPair
    {
        public Button button;
        public string targetURL;
    }

    public ButtonURLPair[] buttonLinks = new ButtonURLPair[4];

    void Start()
    {
        if (buttonLinks.Length != 4)
        {
            Debug.LogError("Need to configure 4 Button-URL correspondences");
            return;
        }

        foreach (var pair in buttonLinks)
        {
            if (pair.button != null)
            {
                pair.button.onClick.AddListener(() => OpenURL(pair.targetURL));
            }
            else
            {
                Debug.LogWarning("Exist unconfigured button references");
            }
        }
    }

    void OpenURL(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
            Debug.Log($"Openning{url}");
        }
        else
        {
            Debug.LogWarning("URL is null, please check the configuration");
        }
    }
}

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
using Cholopol.TIS.MVVM.Views;
using Cholopol.TIS.MVVM.ViewModels;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Interactivity;
using Loxodon.Framework.Views;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class TetrisItemGhostView : UIView, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler
{
    private Image _ghostImage;
    private Coroutine _syncToItemCoroutine;
    public TetrisItemView SelectedTetrisItemView => ResolveCurrentPlacedItemView();

    [SerializeField] RightClickMenuPanel tetrisItemMenuPanel;
    [SerializeField] private UnityEvent OnUpdateEvent = new UnityEvent();
    [SerializeField] private UnityEvent OnBeginDragEvent = new UnityEvent();
    [SerializeField] private UnityEvent OnDragEvent = new UnityEvent();
    [SerializeField] private UnityEvent OnEndDragEvent = new UnityEvent();
    [SerializeField] private UnityEvent<PointerEventData> OnPointerDownEvent = new UnityEvent<PointerEventData>();

    private TetrisItemGhostVM _viewModel;
    public TetrisItemGhostVM ViewModel
    {
        get { return _viewModel; }
        set
        {
            if (_viewModel == value)
                return;
            _viewModel = value;
            if (_viewModel != null)
            {
                this.SetDataContext(_viewModel);
                Bind(_viewModel);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Initialize();
        ViewModel = new TetrisItemGhostVM();
    }

    private void Initialize()
    {
        var rootImage = GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = gameObject.AddComponent<Image>();
        }
        rootImage.color = new Color(1, 1, 1, 0);
        rootImage.raycastTarget = false;

        var contentTrans = transform.Find("Content");
        Image contentImage = null;
        if (contentTrans != null)
        {
            contentImage = contentTrans.GetComponent<Image>();
        }
        else
        {
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(this.transform, false);
            contentImage = contentObj.AddComponent<Image>();
        }

        var contentRect = contentImage.rectTransform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = Vector2.zero;
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;

        contentImage.raycastTarget = true;
        contentImage.preserveAspect = false;
        if (contentImage.GetComponent<SpriteMeshRaycastFilter>() == null)
            contentImage.gameObject.AddComponent<SpriteMeshRaycastFilter>();
        
        _ghostImage = contentImage;
    }

    void Update()
    {
        OnUpdateEvent.Invoke();
        UpdateGhostPreserveAspect();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (CanvasGroup != null) CanvasGroup.blocksRaycasts = false;
        ViewModel.OnDragging = true;
        OnBeginDragEvent.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Vector2 localPoint;
        var canvas = GetComponentInParent<Canvas>();
        var parentRect = canvas.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, cam, out localPoint))
            {
                RectTransform.localPosition = localPoint;
            }
        }
        OnDragEvent.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (CanvasGroup != null) CanvasGroup.blocksRaycasts = true;
        ViewModel.OnDragging = false;
        OnEndDragEvent.Invoke();
        StartSyncToPlacedItem();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            var contextView = ResolveCurrentPlacedItemView();
            if (contextView != null && contextView.ViewModel != null)
            {
                tetrisItemMenuPanel.RectTransform.position = RectTransform.position;
                tetrisItemMenuPanel.Show(true);
                tetrisItemMenuPanel.SetContext(contextView);
            }
        }
        OnPointerDownEvent.Invoke(eventData);
    }

    protected virtual void Bind(TetrisItemGhostVM viewModel)
    {
        var bindingSet = this.CreateBindingSet(viewModel);
        bindingSet.Bind(_ghostImage).For(v => v.sprite).To(vm => vm.Icon).OneWay();
        bindingSet.Bind(_ghostImage).For(v => v.color).To(vm => vm.DraggingGhostColor).OneWay();
        bindingSet.Bind(RectTransform).For(v => v.sizeDelta).To(vm => vm.Size).OneWay();
        bindingSet.Bind().For(v => v.OnInitializeFromItem).To(vm => vm.InitializeFromItemRequest);
        bindingSet.Bind().For(v => v.OnRotate).To(vm => vm.OnRotateRequest);
        bindingSet.Bind().For(v => v.OnUpdateEvent).To(vm => vm.OnUpdate);
        bindingSet.Bind().For(v => v.OnBeginDragEvent).To(vm => vm.OnBeginDrag);
        bindingSet.Bind().For(v => v.OnDragEvent).To(vm => vm.OnDrag);
        bindingSet.Bind().For(v => v.OnEndDragEvent).To(vm => vm.OnEndDrag);
        bindingSet.Bind().For(v => v.OnPointerDownEvent).To<PointerEventData>(vm => vm.OnPointerDown);
        bindingSet.Build();
    }

    public void OnInitializeFromItem(object sender, InteractionEventArgs args)
    {
        if (args == null) return;
        var data = (TetrisItemGhostVM.GhostInitData)args.Context;
        Transform.SetAsLastSibling();
        
        Transform.position = data.WorldPosition;
        RectTransform.pivot = data.Pivot;

        var angle = TetrisUtilities.RotationHelper.GetRotationAngle(data.Direction);
        RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);

        if (_ghostImage != null)
        {
            _ghostImage.rectTransform.localRotation = Quaternion.identity;
            _ghostImage.rectTransform.anchorMin = Vector2.zero;
            _ghostImage.rectTransform.anchorMax = Vector2.one;
            _ghostImage.rectTransform.sizeDelta = Vector2.zero;
            _ghostImage.rectTransform.anchoredPosition = Vector2.zero;
        }
        UpdateGhostPreserveAspect();
    }

    public void OnRotate(object sender, InteractionEventArgs args)
    {
        Dir dir = (Dir)args.Context;
        
        var angle = TetrisUtilities.RotationHelper.GetRotationAngle(dir);
        RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        
        UpdateGhostPreserveAspect();
    }

    private void UpdateGhostPreserveAspect()
    {
        if (_ghostImage == null || ViewModel == null) return;
        var container = ViewModel.TargetContaineOnDrop;
        if (container == null) container = ViewModel.SelectedItem != null ? ViewModel.SelectedItem.CurrentTetrisContainer : null;
        if (container == null) container = ViewModel.OriginContainerOnDrag;
        _ghostImage.preserveAspect = container is TetrisSlotVM;
    }

    private void StartSyncToPlacedItem()
    {
        if (_syncToItemCoroutine != null)
        {
            StopCoroutine(_syncToItemCoroutine);
            _syncToItemCoroutine = null;
        }

        _syncToItemCoroutine = StartCoroutine(SyncToPlacedItemNextFrame());
    }

    private IEnumerator SyncToPlacedItemNextFrame()
    {
        yield return null;

        var view = ResolveCurrentPlacedItemView();
        if (view == null) yield break;

        Transform.position = view.Transform.position;
        RectTransform.pivot = view.RectTransform.pivot;
        if (ViewModel != null && ViewModel.SelectedItem != null)
        {
            ViewModel.UpdateSizeForContainer(ViewModel.SelectedItem.CurrentTetrisContainer);
        }
    }

    private TetrisItemView ResolveCurrentPlacedItemView()
    {
        if (ViewModel == null || ViewModel.SelectedItem == null) return null;

        if (TetrisItemFactory.TryGetViews(ViewModel.SelectedItem.Guid, out var views) && views != null)
        {
            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                if (v == null) continue;
                if (!v.isActiveAndEnabled) continue;
                if (v.ViewModel == null) continue;
                if (v.ViewModel.Guid != ViewModel.SelectedItem.Guid) continue;
                return v;
            }
        }

        return null;
    }

}

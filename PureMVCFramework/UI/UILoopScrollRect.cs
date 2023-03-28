using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PureMVCFramework.Extensions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PureMVCFramework.UI
{
    public abstract class UILoopScrollItem : MonoBehaviour
    {
        public abstract void UpdateItem(object userdata);
        public abstract void ReleaseItem(object userdata);

#if UNITY_EDITOR
        public abstract void DebugInfo();
#endif
    }

    [System.Serializable]
    public class ItemWrap
    {
        public GameObject prefab;
        public object userdata;
    }

    [RequireComponent(typeof(ScrollRect), typeof(EventTrigger))]
    public class UILoopScrollRect : MonoBehaviour
    {
        #region members
        private ScrollRect scrollRect;
        private HorizontalOrVerticalLayoutGroup layoutGroup;

        private readonly List<ItemWrap> wraps = new List<ItemWrap>();
        private readonly Dictionary<ItemWrap, GameObject> items = new Dictionary<ItemWrap, GameObject>();

        private float spacing;
        private bool isInited;
        private bool isDragging;
        private Vector2 beginDragPosition;

        private bool toHead;
        private bool toTail;
        #endregion

        #region delegates
        public delegate void DragBeginDelegate(int head, int tail);
        public DragBeginDelegate DragBegin { get; set; }

        public delegate void PointerDownDelegate();
        public PointerDownDelegate PointerDown { get; set; }

        public delegate void OnCreateItemDelegate(GameObject item, object userdata);
        public OnCreateItemDelegate OnCreateItem { get; set; }

        public delegate void OnDeleteItemDelegate(GameObject item, object userdata);
        public OnDeleteItemDelegate OnDeleteItem { get; set; }

        public delegate void OnFetchHeadDelegate();
        public OnFetchHeadDelegate OnFetchHead { get; set; }

        public delegate void OnFetchTailDelegate();
        public OnFetchTailDelegate OnFetchTail { get; set; }
        #endregion

        public int HeadIndex { get; private set; } = -1;
        public int TailIndex { get; private set; } = -1;

        public ScrollRect ScrollRect => scrollRect;

        public HorizontalOrVerticalLayoutGroup LayoutGroup => layoutGroup;

        public bool ToHead
        {
            get => toHead;
            private set
            {
                toHead = value;
                toTail &= !toHead;
            }
        }
        public bool ToTail
        {
            get => toTail;
            private set
            {
                toTail = value;
                toHead &= !toTail;
            }
        }

        public float ViewportSize => scrollRect.vertical ? scrollRect.viewport.rect.size.y : scrollRect.viewport.rect.size.x;

        public float ContentPivot => scrollRect.vertical ? scrollRect.content.pivot.y : scrollRect.content.pivot.x;

        public float ContentSize
        {
            get
            {
                return scrollRect.vertical ? scrollRect.content.sizeDelta.y : scrollRect.content.sizeDelta.x;
            }
            set
            {
                scrollRect.content.sizeDelta = scrollRect.vertical ? new Vector2(scrollRect.content.sizeDelta.x, value) : new Vector2(value, scrollRect.content.sizeDelta.y);
            }
        }

        public float ContentPosition
        {
            get
            {
                return scrollRect.vertical ? scrollRect.content.anchoredPosition.y : scrollRect.content.anchoredPosition.x;
            }
            set
            {
                scrollRect.content.anchoredPosition = scrollRect.vertical ? new Vector2(scrollRect.content.anchoredPosition.x, value) : new Vector2(value, scrollRect.content.anchoredPosition.y);
            }
        }

        /// <summary>
        /// 超出Viewport上（左）部区域的高（宽）
        /// </summary>
        public float UpperGap
        {
            get
            {
                var gap = scrollRect.vertical ? ContentSize * (1 - ContentPivot) + ContentPosition : ContentSize * ContentPivot - ContentPosition;
                return gap > 0 ? gap : 0;
            }
        }

        /// <summary>
        /// 超出Viewport下（右）部区域的高（宽）
        /// </summary>
        public float LowerGap
        {
            get
            {
                var gap = ContentSize - UpperGap - ViewportSize;
                return gap > 0 ? gap : 0;
            }
        }

        private void Awake()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            if (scrollRect.vertical != scrollRect.horizontal)
            {
                if (scrollRect.vertical)
                {
                    layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                }
                else if (scrollRect.horizontal)
                {
                    layoutGroup = scrollRect.content.GetComponent<HorizontalLayoutGroup>();
                }

                if (layoutGroup == null)
                {
                    enabled = false;
                    throw new System.Exception("Only support HorizontalOrVerticalLayoutGroup in UILoopScrollRect component, you can use UILoopScrollGrid or other component to support your custom style.");
                }

                spacing = layoutGroup.spacing;
            }
            else
            {
                enabled = false;
                throw new System.Exception("Vertical or Horizontal must be different in UILoopScrollRect component for now");
            }
        }

        private void Update()
        {
            if (!isInited)
            {
                Initialize();
                return;
            }

            if (toTail && TailIndex < wraps.Count - 1)
            {
                CreateItem(wraps[++TailIndex]);
            }

            if (toHead && HeadIndex > 0)
            {
                CreateItem(wraps[--HeadIndex]).transform.SetAsFirstSibling();
            }

            if (toHead)
            {
                if (scrollRect.vertical)
                    scrollRect.verticalNormalizedPosition = 1;
                else if (scrollRect.horizontal)
                    scrollRect.horizontalNormalizedPosition = 0;
            }

            if (toTail)
            {
                if (scrollRect.vertical)
                    scrollRect.verticalNormalizedPosition = 0;
                else if (scrollRect.horizontal)
                    scrollRect.horizontalNormalizedPosition = 1;
            }

            if (ContentSize < ViewportSize)
            {
                if (HeadIndex > 0)
                {
                    CreateItem(wraps[--HeadIndex]).transform.SetAsFirstSibling();
                }
                else if (TailIndex < wraps.Count - 1)
                {
                    CreateItem(wraps[++TailIndex]);
                }
            }

            if (isInited && !isDragging && wraps.Count > 0)
            {
                float topSize = 0, bottomSize = 0;
                if (scrollRect.vertical)
                {
                    topSize = items[wraps[HeadIndex]].GetComponent<RectTransform>().sizeDelta.y;
                    bottomSize = items[wraps[TailIndex]].GetComponent<RectTransform>().sizeDelta.y;
                }
                else if (scrollRect.horizontal)
                {
                    topSize = items[wraps[HeadIndex]].GetComponent<RectTransform>().sizeDelta.x;
                    bottomSize = items[wraps[TailIndex]].GetComponent<RectTransform>().sizeDelta.x;
                }

                if (UpperGap > ViewportSize + topSize + spacing)
                {
                    DeleteItem(wraps[HeadIndex++]);
                    if (scrollRect.vertical && ContentPivot == 1)
                        ContentPosition -= topSize + spacing;
                    else if (scrollRect.horizontal && ContentPivot == 0)
                        ContentPosition += topSize + spacing;
                }
                else if (HeadIndex > 0 && UpperGap < ViewportSize)
                {
                    var go = CreateItem(wraps[--HeadIndex]);
                    go.transform.SetAsFirstSibling();
                    if (scrollRect.vertical && ContentPivot == 1)
                    {
                        var size = go.GetComponent<RectTransform>().sizeDelta.y;
                        ContentPosition += size + spacing;
                    }
                    else if (scrollRect.horizontal && ContentPivot == 0)
                    {
                        var size = go.GetComponent<RectTransform>().sizeDelta.x;
                        ContentPosition -= size + spacing;
                    }
                }

                if (LowerGap > ViewportSize + bottomSize + spacing)
                {
                    DeleteItem(wraps[TailIndex--]);
                    if (scrollRect.vertical && ContentPivot == 0)
                        ContentPosition += bottomSize + spacing;
                    else if (scrollRect.horizontal && ContentPivot == 1)
                        ContentPosition -= bottomSize + spacing;
                }
                else if (TailIndex < wraps.Count - 1 && LowerGap < ViewportSize)
                {
                    var go = CreateItem(wraps[++TailIndex]);
                    if (scrollRect.vertical && ContentPivot == 0)
                    {
                        var size = go.GetComponent<RectTransform>().sizeDelta.y;
                        ContentPosition -= size + spacing;
                    }
                    else if (scrollRect.horizontal && ContentPivot == 1)
                    {
                        var size = go.GetComponent<RectTransform>().sizeDelta.x;
                        ContentPosition += size + spacing;
                    }
                }
            }
        }

        #region EventTrigger function
        public void OnPointerDown(BaseEventData eventData)
        {
            PointerDown?.Invoke();
        }

        public void OnDragBegin(BaseEventData eventData)
        {
            isDragging = true;
            beginDragPosition = scrollRect.normalizedPosition;

            toTail = false;
            toHead = false;

            DragBegin?.Invoke(HeadIndex, TailIndex);
        }

        public void OnDragEnd(BaseEventData eventData)
        {
            isDragging = false;

            if (scrollRect.vertical)
            {
                if (scrollRect.normalizedPosition.y - beginDragPosition.y > 0.1f && scrollRect.normalizedPosition.y > 1 && HeadIndex == 0)
                {
                    OnFetchHead?.Invoke();
                }
                else if (beginDragPosition.y - scrollRect.normalizedPosition.y > 0.1f && scrollRect.normalizedPosition.y < 0 && TailIndex == wraps.Count - 1)
                {
                    OnFetchTail?.Invoke();
                }
            }
            else if (scrollRect.horizontal)
            {
                if (scrollRect.normalizedPosition.x - beginDragPosition.x > 0.1f && scrollRect.normalizedPosition.x > 1 && HeadIndex == 0)
                {
                    OnFetchHead?.Invoke();
                }
                else if (beginDragPosition.x - scrollRect.normalizedPosition.x > 0.1f && scrollRect.normalizedPosition.x < 0 && TailIndex == wraps.Count - 1)
                {
                    OnFetchTail?.Invoke();
                }
            }
        }
        #endregion

        private void Initialize()
        {
            if (ViewportSize == 0)
                return;

            if (HeadIndex == -1 && TailIndex == -1 && wraps.Count > 0)
            {
                HeadIndex = TailIndex = 0;
                CreateItem(wraps[HeadIndex]);

                isInited = true;
            }
        }

        public void Clear()
        {
            isInited = false;
            isDragging = false;
            HeadIndex = -1;
            TailIndex = -1;

            wraps.Clear();

            foreach (var v in items)
            {
                UILoopScrollItem item = v.Value.GetComponent<UILoopScrollItem>();
                if (item != null)
                {
                    item.ReleaseItem(v.Key.userdata);
                }
                else
                {
                    OnDeleteItem?.Invoke(v.Value, v.Key.userdata);
                }

                v.Value.Recycle();
            }
            items.Clear();
        }

        public int GetItemWrapCount()
        {
            return wraps.Count;
        }

        public ItemWrap GetItemWrap(object userdata)
        {
            return wraps.Find(p => p.userdata == userdata);
        }

        public ItemWrap GetItemWrap(int index)
        {
            if (index < 0 || index >= wraps.Count)
                return null;

            return wraps[index];
        }

        public ItemWrap RemoveItemWrap(object userdata)
        {
            ItemWrap item = GetItemWrap(userdata);
            if (item != null)
            {
                for (int index = 0; index < wraps.Count; ++index)
                {
                    if (wraps[index] == item)
                    {
                        RemoveItemWrap(index);
                        break;
                    }
                }
            }

            return item;
        }

        public void RemoveItemWrap(int index)
        {
            if (index < 0 || index >= wraps.Count)
                return;

            if (index < HeadIndex)
            {
                HeadIndex--;
                TailIndex--;
            }
            else if (index <= TailIndex)
            {
                TailIndex--;
                DeleteItem(wraps[index]);

                if (TailIndex == -1)
                    HeadIndex = -1;
            }

            wraps.RemoveAt(index);
        }

        public void AddItemWrap(GameObject prefab, object userdata)
        {
            prefab.CreatePool();
            wraps.Add(new ItemWrap { prefab = prefab, userdata = userdata });
        }

        public void InsertItemWrap(int index, GameObject prefab, object userdata)
        {
            if (wraps.Count > 0)
            {
                if (index < 0 || index >= wraps.Count)
                    return;

                prefab.CreatePool();
                wraps.Insert(index, new ItemWrap { prefab = prefab, userdata = userdata });

                if (index <= HeadIndex)
                {
                    HeadIndex++;
                    TailIndex++;
                }
                else if (index <= TailIndex)
                {
                    TailIndex++;
                    CreateItem(wraps[index]).transform.SetSiblingIndex(index - HeadIndex);
                }
            }
            else
            {
                AddItemWrap(prefab, userdata);
            }
        }

        public void AddItemToHead(GameObject prefab, object userdata)
        {
            InsertItemWrap(0, prefab, userdata);
            ToHead = true;
        }

        public void AddItemToTail(GameObject prefab, object userdata)
        {
            AddItemWrap(prefab, userdata);
            ToTail = true;
        }

        public GameObject GetItem(ItemWrap wrap)
        {
            if (items.TryGetValue(wrap, out GameObject go))
                return go;

            return null;
        }

        private GameObject CreateItem(ItemWrap wrap)
        {
            if (wrap.prefab != null)
            {
                GameObject go = wrap.prefab.Spawn(scrollRect.content);
                UILoopScrollItem item = go.GetComponent<UILoopScrollItem>();
                if (item != null)
                {
                    item.UpdateItem(wrap.userdata);
                }
                else
                {
                    OnCreateItem?.Invoke(go, wrap.userdata);
                }

                items[wrap] = go;
                return go;
            }
            else
            {
                return null;
            }

        }

        private void DeleteItem(ItemWrap wrap)
        {
            if (items.TryGetValue(wrap, out GameObject go))
            {
                UILoopScrollItem item = go.GetComponent<UILoopScrollItem>();
                if (item != null)
                {
                    item.ReleaseItem(wrap.userdata);
                }
                else
                {
                    OnDeleteItem?.Invoke(go, wrap.userdata);
                }

                go.Recycle();
                items.Remove(wrap);
            }
        }

        #region Editor function
#if UNITY_EDITOR
        [MenuItem("GameObject/UI/LoopScrollVertical", false, 3001)]
        public static void OnCreateLoopScrollVertical()
        {
            OnCreateLoopScroll(true);
        }

        [MenuItem("GameObject/UI/LoopScrollHorizontal", false, 3002)]
        public static void OnCreateLoopScrollHorizontal()
        {
            OnCreateLoopScroll(false);
        }

        private static void OnCreateLoopScroll(bool isVertical)
        {
            var view = new GameObject("LoopScrollView").GetOrAddComponent<RectTransform>();
#if UNITY_2021_2_OR_NEWER
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
#else
            if (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
#endif
            {
                if (Selection.activeGameObject != null)
                    view.SetParent(Selection.activeGameObject.transform, false);
            }
            else
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    canvas = new GameObject("Canvas").AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    canvas.GetOrAddComponent<CanvasScaler>();
                    canvas.GetOrAddComponent<GraphicRaycaster>();
                }

                var eventsystem = FindObjectOfType<EventSystem>();
                if (eventsystem == null)
                {
                    eventsystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                    eventsystem.GetOrAddComponent<StandaloneInputModule>();
                }

                view.SetParent(canvas.transform, false);
            }

            view.sizeDelta = new Vector2(200, 200);

            var background = view.GetOrAddComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            background.color = new Color32(255, 255, 255, 100);
            background.type = Image.Type.Sliced;

            var loopRect = view.GetOrAddComponent<UILoopScrollRect>();
            var evtTrigger = view.GetOrAddComponent<EventTrigger>();

            var beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener(loopRect.OnDragBegin);
            evtTrigger.triggers.Add(beginDragEntry);

            var endDragEntry = new EventTrigger.Entry();
            endDragEntry.eventID = EventTriggerType.EndDrag;
            endDragEntry.callback.AddListener(loopRect.OnDragEnd);
            evtTrigger.triggers.Add(endDragEntry);

            var scrollRect = view.GetOrAddComponent<ScrollRect>();
            scrollRect.viewport = scrollRect.transform.GetOrAddTransform("Viewport").GetOrAddComponent<RectTransform>();
            scrollRect.viewport.anchorMax = Vector2.one;
            scrollRect.viewport.anchorMin = Vector2.zero;
            scrollRect.viewport.pivot = Vector2.up;
            scrollRect.viewport.sizeDelta = Vector2.zero;

            var maskImage = scrollRect.viewport.GetOrAddComponent<Image>();
            maskImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            maskImage.type = Image.Type.Sliced;
            var mask = scrollRect.viewport.GetOrAddComponent<Mask>();
            mask.showMaskGraphic = false;

            scrollRect.content = scrollRect.transform.GetOrAddTransform("Viewport/Content").GetOrAddComponent<RectTransform>();

            if (isVertical)
            {
                scrollRect.horizontal = false;
                scrollRect.content.anchorMax = Vector2.one;
                scrollRect.content.anchorMin = Vector2.up;
                scrollRect.content.pivot = Vector2.up;
                scrollRect.content.sizeDelta = Vector2.zero;

                var layout = scrollRect.content.GetOrAddComponent<VerticalLayoutGroup>();
                layout.childForceExpandHeight = false;

                var fitter = layout.GetOrAddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                scrollRect.vertical = false;
                scrollRect.content.anchorMax = Vector2.up;
                scrollRect.content.anchorMin = Vector2.zero;
                scrollRect.content.pivot = Vector2.zero;
                scrollRect.content.sizeDelta = Vector2.zero;

                var layout = scrollRect.content.GetOrAddComponent<HorizontalLayoutGroup>();
                layout.childForceExpandWidth = false;

                var fitter = scrollRect.content.GetOrAddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
#endif
        #endregion
    }
}

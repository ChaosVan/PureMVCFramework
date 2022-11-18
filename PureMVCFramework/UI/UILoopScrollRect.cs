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

        public ScrollRect ScrollRect
        {
            get
            {
                return scrollRect;
            }
        }

        public HorizontalOrVerticalLayoutGroup LayoutGroup
        {
            get
            {
                return layoutGroup;
            }
        }

        public bool ToHead
        {
            set
            {
                toHead = value;
                toTail &= !toHead;
            }
        }
        public bool ToTail
        {
            set
            {
                toTail = value;
                toHead &= !toTail;
            }
        }

        public Vector2 ViewportSize
        {
            get
            {
                return scrollRect.viewport.rect.size;
            }
        }

        public Vector2 ContentSize
        {
            get
            {
                return scrollRect.content.rect.size;
            }
        }

        public Vector2 ContentPosition
        {
            get
            {
                return scrollRect.content.anchoredPosition;
            }
            set
            {
                scrollRect.content.anchoredPosition = value;
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
                    Debug.LogError("Only support HorizontalOrVerticalLayoutGroup in UILoopScrollRect component, you can use UILoopScrollGrid or other component to support your custom style.");
                    enabled = false;
                    return;
                }

                spacing = layoutGroup.spacing;
            }
            else
            {
                Debug.LogError("Vertical or Horizontal must be different in UILoopScrollRect component for now");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            scrollRect.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            scrollRect.onValueChanged.RemoveListener(OnValueChanged);
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
                GameObject go = CreateItem(wraps[--HeadIndex]);
                go?.transform.SetAsFirstSibling();
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

            if ((scrollRect.vertical && ContentSize.y < ViewportSize.y) || (scrollRect.horizontal && ContentSize.x < ViewportSize.x))
            {
                if (HeadIndex > 0)
                {
                    GameObject go = CreateItem(wraps[--HeadIndex]);
                    go?.transform.SetAsFirstSibling();
                }
                else if (TailIndex < wraps.Count - 1)
                {
                    CreateItem(wraps[++TailIndex]);
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
                else
                {
                    OnValueChanged(scrollRect.normalizedPosition);
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
                else
                {
                    OnValueChanged(scrollRect.normalizedPosition);
                }
            }
        }
        #endregion

        private void Initialize()
        {
            if (ViewportSize == Vector2.zero)
                return;

            if (scrollRect.vertical)
            {
                InitVertical();
            }
            else if (scrollRect.horizontal)
            {
                InitHorizontal();
            }

            isInited = true;
        }

        private void InitVertical()
        {
            Vector2 CalcContentSize = new Vector2(layoutGroup.padding.left + layoutGroup.padding.right, layoutGroup.padding.top + layoutGroup.padding.bottom);
            if (toTail)
            {
                TailIndex = wraps.Count - 1;
                for (int i = TailIndex; i >= 0; --i)
                {
                    if (CalcContentSize.y < ViewportSize.y)
                    {
                        HeadIndex = i;
                        GameObject go = CreateItem(wraps[i]);
                        if (go != null)
                        {
                            go.transform.SetAsFirstSibling();
                            CalcContentSize = new Vector2(CalcContentSize.x, CalcContentSize.y + go.GetComponent<RectTransform>().rect.size.y);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                HeadIndex = 0;
                for (int i = 0; i < wraps.Count; ++i)
                {
                    if (CalcContentSize.y <= ViewportSize.y)
                    {
                        TailIndex = i;
                        GameObject go = CreateItem(wraps[i]);
                        if (go != null)
                            CalcContentSize = new Vector2(CalcContentSize.x, CalcContentSize.y + go.GetComponent<RectTransform>().rect.size.y);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void InitHorizontal()
        {
            Vector2 CalcContentSize = new Vector2(layoutGroup.padding.left + layoutGroup.padding.right, layoutGroup.padding.top + layoutGroup.padding.bottom);
            if (toTail)
            {
                TailIndex = wraps.Count - 1;
                for (int i = TailIndex; i >= 0; --i)
                {
                    if (CalcContentSize.x < ViewportSize.x)
                    {
                        HeadIndex = i;
                        GameObject go = CreateItem(wraps[i]);
                        if (go != null)
                        {
                            go.transform.SetAsFirstSibling();
                            CalcContentSize = new Vector2(CalcContentSize.x + go.GetComponent<RectTransform>().rect.size.x, CalcContentSize.y);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                HeadIndex = 0;
                for (int i = 0; i < wraps.Count; ++i)
                {
                    if (CalcContentSize.x <= ViewportSize.x)
                    {
                        TailIndex = i;
                        GameObject go = CreateItem(wraps[i]);
                        if (go != null)
                            CalcContentSize = new Vector2(CalcContentSize.x + go.GetComponent<RectTransform>().rect.size.x, CalcContentSize.y);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void Clear()
        {
            isInited = false;
            isDragging = false;
            HeadIndex = 0;
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

            wraps.RemoveAt(index);

            if (index < HeadIndex)
                HeadIndex--;

            if (index <= TailIndex)
                TailIndex--;
        }

        public void AddItemWrap(GameObject prefab, object userdata)
        {
            prefab.CreatePool();
            wraps.Add(new ItemWrap { prefab = prefab, userdata = userdata });
        }

        public void InsertItemWrap(int index, GameObject prefab, object userdata)
        {
            if (index < 0 || index > wraps.Count)
                return;

            prefab.CreatePool();
            wraps.Insert(index, new ItemWrap { prefab = prefab, userdata = userdata });

            if (index <= HeadIndex)
                HeadIndex++;

            if (index <= TailIndex)
                TailIndex++;
        }

        public void AddItemToHead(GameObject prefab, object userdata)
        {
            InsertItemWrap(0, prefab, userdata);

            bool isHead = HeadIndex == 0;
            if (isHead)
                CreateItem(wraps[0]);

            ToHead = isHead;
        }

        public void AddItemToTail(GameObject prefab, object userdata)
        {
            AddItemWrap(prefab, userdata);

            var isTail = TailIndex == wraps.Count - 1;
            if (isTail)
                CreateItem(wraps[++TailIndex]);

            ToTail = isTail;
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

        void OnValueChanged(Vector2 vec)
        {
            if (!isInited)
                return;

            int count = scrollRect.content.childCount;
            if (count > 0 && wraps.Count > 0)
            {
                if (!isDragging)
                {
                    if (scrollRect.vertical)
                    {
                        float topHeight = items[wraps[HeadIndex]].GetComponent<RectTransform>().rect.height;
                        float bottomHeight = items[wraps[TailIndex]].GetComponent<RectTransform>().rect.height;

                        if (ContentPosition.y > ViewportSize.y + topHeight + spacing) // Drag to higher
                        {
                            DeleteItem(wraps[HeadIndex++]);
                            ContentPosition = new Vector2(ContentPosition.x, ContentPosition.y - topHeight - spacing);

                            if (TailIndex < wraps.Count - 1)
                                CreateItem(wraps[++TailIndex]);
                        }
                        else if (ContentPosition.y < ViewportSize.y) // Drag to lower
                        {
                            if (ContentSize.y - ContentPosition.y - ViewportSize.y > ViewportSize.y + bottomHeight + spacing)
                            {
                                DeleteItem(wraps[TailIndex--]);
                            }

                            if (HeadIndex > 0)
                            {
                                GameObject go = CreateItem(wraps[--HeadIndex]);
                                if (go != null)
                                {
                                    go.transform.SetAsFirstSibling();
#if UNITY_EDITOR
                                    go.GetComponent<UILoopScrollItem>()?.DebugInfo();
#endif
                                    float height = go.GetComponent<RectTransform>().rect.height;
                                    ContentPosition = new Vector2(ContentPosition.x, ContentPosition.y + height + spacing);
                                }
                            }
                            else if (ContentSize.y - ContentPosition.y - ViewportSize.y < ViewportSize.y && TailIndex < wraps.Count - 1)
                            {
                                CreateItem(wraps[++TailIndex]);
                            }
                        }
                    }
                    else if (scrollRect.horizontal)
                    {
                        float leftWidth = items[wraps[HeadIndex]].GetComponent<RectTransform>().rect.width;
                        float rightWidth = items[wraps[TailIndex]].GetComponent<RectTransform>().rect.width;

                        if (ContentPosition.x < -(ViewportSize.x + leftWidth + spacing)) // Drag to higher
                        {
                            DeleteItem(wraps[HeadIndex++]);
                            ContentPosition = new Vector2(ContentPosition.x + leftWidth + spacing, ContentPosition.y);

                            if (TailIndex < wraps.Count - 1)
                                CreateItem(wraps[++TailIndex]);
                        }
                        else if (ContentPosition.x > -ViewportSize.x) // Drag to lower
                        {
                            if (ContentSize.x + ContentPosition.x - ViewportSize.x > ViewportSize.x + rightWidth + spacing)
                            {
                                DeleteItem(wraps[TailIndex--]);
                            }

                            if (HeadIndex > 0)
                            {
                                GameObject go = CreateItem(wraps[--HeadIndex]);
                                if (go != null)
                                {
                                    go.transform.SetAsFirstSibling();
#if UNITY_EDITOR
                                    go.GetComponent<UILoopScrollItem>()?.DebugInfo();
#endif
                                    float width = go.GetComponent<RectTransform>().rect.width;
                                    ContentPosition = new Vector2(ContentPosition.x - width - spacing, ContentPosition.y);
                                }
                            }
                            else if (ContentSize.x + ContentPosition.x - ViewportSize.x < ViewportSize.x && TailIndex < wraps.Count - 1)
                            {
                                CreateItem(wraps[++TailIndex]);
                            }
                        }
                    }
                }
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

using ClipMate.Core.Models;
using ClipMate.Core.Search;
using ClipMate.Messages;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Presentation.Clipboard;
using ClipMate.Service.Clipboard;
using ClipMate.Service.Interfaces;
using ClipMate.Services;
using ClipMate.UI.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ClipMate.ViewModels;

public partial class ClipboardViewModel : ObservableObject
{
    /// <summary>
    /// 搜索框焦点请求事件
    /// </summary>
    public event EventHandler? SearchBoxFocusRequested;
    public event EventHandler? ScrollToSelectedRequested;

    [ObservableProperty]
    private ObservableCollection<IClipboardContent> _clipboardItems;

    [ObservableProperty]
    private IClipboardContent? _selectedItem;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isFavoriteFilterEnabled;

    [ObservableProperty]
    private VirtualKey _favoriteFilterHotKeyKey = VirtualKey.B;

    [ObservableProperty]
    private KeyModifiers _favoriteFilterHotKeyModifiers = KeyModifiers.Ctrl;

    [ObservableProperty]
    private int _clipboardItemMaxHeight = 56;

    [ObservableProperty]
    private int _clipboardItemMinHeight = 56;

    [ObservableProperty]
    private bool _imeHintsEnabled = true;

    private readonly List<IClipboardContent> _allClipboardItems = [];
    private readonly TimeSpan _searchDebounceInterval = TimeSpan.FromMilliseconds(180);
    private readonly TimeSpan _largeListDelay = TimeSpan.FromMilliseconds(50);
    private readonly int _largeListThreshold = 1000;
    private readonly int _searchInfoThresholdMs = 25;
    private readonly int _searchWarningThresholdMs;
    private readonly bool _enableSearchDiagnostics;
    private int _searchRefreshToken;
    private SearchQuerySnapshot _searchSnapshot = SearchQuerySnapshot.Empty;

    private readonly IClipboardService _clipboardService;
    private readonly IClipboardCaptureUseCase _clipboardCaptureUseCase;
    private readonly IClipboardHistoryUseCase _clipboardHistoryUseCase;
    private readonly ISettingsService _settingsService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger _logger;
    private Task? _initialLoadTask;
    private readonly TimeSpan _pasteDuplicateWaitTimeout = TimeSpan.FromMilliseconds(350);
    private PendingPaste? _pendingPaste;

    public ClipboardViewModel(
        IClipboardService clipboardService,
        IClipboardChangeSource clipboardChangeSource,
        IClipboardCaptureUseCase clipboardCaptureUseCase,
        IClipboardHistoryUseCase clipboardHistoryUseCase,
        ISettingsService settingsService,
        IUiDispatcher uiDispatcher,
        ILogger logger)
    {
        ClipboardItems = [];
        _clipboardService = clipboardService;
        _clipboardCaptureUseCase = clipboardCaptureUseCase;
        _clipboardHistoryUseCase = clipboardHistoryUseCase;
        _settingsService = settingsService;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
        _searchWarningThresholdMs = Math.Max(50,
            int.TryParse(Environment.GetEnvironmentVariable("CLIPMATE_SEARCH_WARN_MS"), out var warnMs) && warnMs > 0
                ? warnMs
                : 100);
        var diagnosticsEnv = Environment.GetEnvironmentVariable("CLIPMATE_SEARCH_DIAGNOSTICS");
        _enableSearchDiagnostics = !string.IsNullOrWhiteSpace(diagnosticsEnv) &&
            (diagnosticsEnv.Equals("1", StringComparison.OrdinalIgnoreCase) ||
             diagnosticsEnv.Equals("true", StringComparison.OrdinalIgnoreCase));

        clipboardChangeSource.ClipboardChanged += ClipboardChangedAsync;

        // 初始化剪贴项最大高度设置
        ClipboardItemMaxHeight = _settingsService.GetClipboardItemMaxHeight();
        ClipboardItemMinHeight = _settingsService.GetClipboardItemMinHeight();
        ImeHintsEnabled = _settingsService.GetImeHintsEnabled();
        UpdateFavoriteFilterHotkeyBinding(_settingsService.GetFavoriteFilterHotKey());

        // 订阅剪贴项高度设置变更消息
        WeakReferenceMessenger.Default.Register<ClipboardItemMaxHeightChangedMessage>(this, (recipient, message) =>
        {
            HandleClipboardItemMaxHeightChanged(message.Value);
        });

        WeakReferenceMessenger.Default.Register<ClipboardItemMinHeightChangedMessage>(this, (recipient, message) =>
        {
            HandleClipboardItemMinHeightChanged(message.Value);
        });

        WeakReferenceMessenger.Default.Register<ImeHintsEnabledChangedMessage>(this, (_, message) =>
        {
            _uiDispatcher.Invoke(() =>
            {
                ImeHintsEnabled = message.Value;
            });
        });

        WeakReferenceMessenger.Default.Register<FavoriteFilterHotKeyChangedMessage>(this, (_, message) =>
        {
            UpdateFavoriteFilterHotkeyBinding(message.Value);
        });

        WeakReferenceMessenger.Default.Register<FavoriteFilterHotKeyPressedMessage>(this, (_, _) =>
        {
            _uiDispatcher.Invoke(ToggleFavoriteFilter);
        });

        _initialLoadTask = LoadHistoryAsync();
    }

    /// <summary>
    /// 处理剪贴项最大高度设置变更消息
    /// </summary>
    /// <param name="newHeight">新的高度值</param>
    private void HandleClipboardItemMaxHeightChanged(int newHeight)
    {
        _uiDispatcher.Invoke(() =>
        {
            ClipboardItemMaxHeight = newHeight;
            _logger.Information("剪贴项最大高度已更新: {Height}", ClipboardItemMaxHeight);
        });
    }

    private void HandleClipboardItemMinHeightChanged(int newHeight)
    {
        _uiDispatcher.Invoke(() =>
        {
            ClipboardItemMinHeight = newHeight;
            _logger.Information("剪贴项最小高度已更新: {Height}", ClipboardItemMinHeight);
        });
    }

    private void UpdateFavoriteFilterHotkeyBinding(string? hotkey)
    {
        if (!HotkeyDescriptor.TryParse(hotkey, out var descriptor))
        {
            _logger.Warning("无法解析收藏筛选快捷键: {Hotkey}", hotkey);
            descriptor = new HotkeyDescriptor(VirtualKey.B, KeyModifiers.Ctrl);
        }

        _uiDispatcher.Invoke(() =>
        {
            FavoriteFilterHotKeyKey = descriptor.Value.Key;
            FavoriteFilterHotKeyModifiers = descriptor.Value.Modifiers;
        });
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            _logger.Information("开始加载剪贴板历史记录");

            var items = await _clipboardHistoryUseCase.GetAllDescAsync();

            if (_uiDispatcher.CheckAccess())
            {
                MergeHistory(items);
            }
            else
            {
                await _uiDispatcher.InvokeAsync(() => MergeHistory(items), UiDispatcherPriority.Background);
            }

            _logger.Information("加载剪贴板历史完成，共 {Count} 项", _allClipboardItems.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载剪贴板历史记录失败");
        }
    }

    private void MergeHistory(IReadOnlyList<ClipboardItem> items)
    {
        if (_allClipboardItems.Count == 0)
        {
            foreach (var item in items)
            {
                _allClipboardItems.Add(_clipboardService.Create(item));
            }

            ReplaceVisibleItems(BuildVisibleItems());
            return;
        }

        var existingIds = _allClipboardItems
            .Select(c => c.Value.Id)
            .Where(id => id > 0)
            .ToHashSet();

        foreach (var item in items)
        {
            if (item.Id > 0 && existingIds.Contains(item.Id))
            {
                continue;
            }

            var content = _clipboardService.Create(item);
            _allClipboardItems.Add(content);
            if (IsVisible(content))
            {
                ClipboardItems.Add(content);
            }
        }
    }

    private async void ClipboardChangedAsync(object? sender, ClipboardPayloadChangedEventArgs e)
    {
        try
        {
            var captureResult = await _clipboardCaptureUseCase.CaptureAsync(e.Payload);
            TryCompletePendingPaste(e.Payload, captureResult);

            // 检查是否是已存在的重复项（需要置顶）
            var existingItem = _allClipboardItems.FirstOrDefault(x => x.Value.Id == captureResult.Id);
            if (existingItem != null)
            {
                // 将已存在的重复项置顶
                await MoveItemToTopAsync(existingItem, "重复内容置顶");
                return;
            }

            if (captureResult.Id < 0 || captureResult.Item == null)
            {
                _logger.Debug("检测到空内容，跳过插入: {Payload}", DescribePayload(e.Payload));
                return;
            }

            // 新内容，插入到列表顶部
            var clipItem = _clipboardService.Create(captureResult.Item);
            _allClipboardItems.Insert(0, clipItem);
            if (IsVisible(clipItem))
            {
                ClipboardItems.Insert(0, clipItem);
            }
            _logger.Information("插入新项: {Content}", clipItem.Summary);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理剪贴板变化失败");
        }
    }

    private void TryCompletePendingPaste(ClipboardPayload payload, ClipboardCaptureResult captureResult)
    {
        var pending = _pendingPaste;
        if (pending == null)
        {
            return;
        }

        if (!IsPayloadMatchingItem(payload, pending.Item.Value))
        {
            return;
        }

        var outcome = captureResult.Id < 0 || captureResult.Item == null
            ? PasteCaptureOutcome.Duplicate
            : PasteCaptureOutcome.Inserted;
        pending.Completion.TrySetResult(outcome);
    }

    private static string DescribePayload(ClipboardPayload payload)
    {
        return payload.Type switch
        {
            ClipboardPayloadType.Text => $"Text(len={payload.Text?.Length ?? 0})",
            ClipboardPayloadType.ImagePng => $"ImagePng(bytes={payload.ImagePngBytes?.Length ?? 0})",
            ClipboardPayloadType.FileDropList => $"FileDropList(count={payload.FilePaths?.Count ?? 0})",
            _ => payload.Type.ToString()
        };
    }

    private bool IsVisible(IClipboardContent item)
    {
        if (IsFavoriteFilterEnabled && !item.IsFavorite)
        {
            return false;
        }

        return item.IsVisible(_searchSnapshot);
    }

    /// <summary>
    /// 当搜索查询更改时刷新过滤
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        ScheduleSearchRefresh("query-changed");
    }

    partial void OnIsFavoriteFilterEnabledChanged(bool value)
    {
        ScheduleSearchRefresh("favorite-filter-changed", force: true, immediate: true);
    }

    [RelayCommand]
    private void Search(string? queryText)
    {
        if (queryText is string text && !string.Equals(text, SearchQuery, StringComparison.Ordinal))
        {
            SearchQuery = text;
            return;
        }

        ScheduleSearchRefresh("search-command", force: true);
    }

    private void ScheduleSearchRefresh(string reason, bool force = false, bool immediate = false)
    {
        var snapshot = SearchQuerySnapshot.From(SearchQuery);
        if (!force && snapshot == _searchSnapshot)
        {
            return;
        }

        var refreshToken = Interlocked.Increment(ref _searchRefreshToken);
        var itemCountSnapshot = _allClipboardItems.Count;
        var dispatcherPriority = immediate ? UiDispatcherPriority.Normal : UiDispatcherPriority.Background;

        _ = Task.Run(async () =>
        {
            try
            {
                if (!immediate && itemCountSnapshot > _largeListThreshold)
                {
                    await Task.Delay(_largeListDelay);
                }

                if (!immediate)
                {
                    await Task.Delay(_searchDebounceInterval);
                }

                if (refreshToken != _searchRefreshToken)
                {
                    return;
                }

                await _uiDispatcher.InvokeAsync(
                    () =>
                    {
                        if (refreshToken != _searchRefreshToken)
                        {
                            return;
                        }

                        RefreshSearch(snapshot, reason);
                    },
                    dispatcherPriority);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "搜索刷新失败，原因: {Reason}", reason);
            }
        });
    }

    private void RefreshSearch(SearchQuerySnapshot snapshot, string reason)
    {
        _searchSnapshot = snapshot;

        var totalCount = _allClipboardItems.Count;
        var stopwatch = Stopwatch.StartNew();
        var visibleItems = BuildVisibleItems();
        ReplaceVisibleItems(visibleItems);
        stopwatch.Stop();

        if (SelectedItem != null && !ClipboardItems.Contains(SelectedItem))
        {
            SelectedItem = ClipboardItems.FirstOrDefault();
        }

        if (!_enableSearchDiagnostics)
        {
            return;
        }

        var hitCount = visibleItems.Count;
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var queryPreview = snapshot.HasQuery
            ? (snapshot.Normalized.Length > 60 ? string.Concat(snapshot.Normalized.AsSpan(0, 60), "...") : snapshot.Normalized)
            : "(empty)";

        if (elapsedMs >= _searchWarningThresholdMs)
        {
            _logger.Warning(
                "搜索刷新耗时 {Elapsed}ms，原因 {Reason}，查询长度 {QueryLength}，命中 {HitCount}/{TotalCount}，查询：\"{Query}\"",
                elapsedMs, reason, snapshot.Length, hitCount, totalCount, queryPreview);
        }
        else if (elapsedMs >= _searchInfoThresholdMs || snapshot.HasQuery)
        {
            _logger.Information(
                "搜索刷新耗时 {Elapsed}ms，原因 {Reason}，查询长度 {QueryLength}，命中 {HitCount}/{TotalCount}，查询：\"{Query}\"",
                elapsedMs, reason, snapshot.Length, hitCount, totalCount, queryPreview);
        }
    }

    private List<IClipboardContent> BuildVisibleItems()
    {
        List<IClipboardContent> visible = new();

        foreach (var item in _allClipboardItems)
        {
            if (IsVisible(item))
            {
                visible.Add(item);
            }
        }

        return visible;
    }

    private void ReplaceVisibleItems(IReadOnlyList<IClipboardContent> items)
    {
        ClipboardItems.Clear();
        foreach (var item in items)
        {
            ClipboardItems.Add(item);
        }
    }

    // 仅供测试注入初始数据，避免绕过内部集合状态
    internal void SeedItemsForTest(IEnumerable<IClipboardContent> items)
    {
        _allClipboardItems.Clear();
        foreach (var item in items)
        {
            _allClipboardItems.Add(item);
        }

        ReplaceVisibleItems(BuildVisibleItems());
    }

    /// <summary>
    /// 置顶
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task MoveToTopAsync(IClipboardContent? item)
    {
        if (item == null)
        {
            return;
        }

        try
        {
            await MoveItemToTopAsync(item, "置顶项");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "置顶项失败：{Content}", item.Summary);
            throw;
        }
    }

    /// <summary>
    /// 复制
    /// </summary>
    /// <param name="item"></param>
    [RelayCommand]
    private async Task CopyAsync(IClipboardContent? item)
    {
        if (item == null)
        {
            return;
        }

        try
        {
            await item.CopyAsync();
            _logger.Information("复制项：{Content}", item.Summary);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "复制项失败：{Content}", item.Summary);
            throw;
        }
    }

    /// <summary>
    /// 粘贴
    /// </summary>
    /// <param name="item"></param>
    [RelayCommand]
    private async Task PasteAsync(IClipboardContent? item)
    {
        if (item == null)
        {
            return;
        }

        PendingPaste? pending = null;
        try
        {
            pending = StartPendingPaste(item);
            await _clipboardService.PasteAsync(item);
            _logger.Information("粘贴项: {Content}", item.Summary);

            var outcome = await WaitForPasteCaptureAsync(pending);
            if (outcome == PasteCaptureOutcome.Duplicate)
            {
                await MoveItemToTopAsync(item, "重复粘贴项置顶");
                return;
            }

            // 不是第一个则删除
            if (ClipboardItems.IndexOf(item) > 0 && !item.IsFavorite)
            {
                await DeleteAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "粘贴项失败：{Content}", item.Summary);
        }
        finally
        {
            if (pending != null)
            {
                ClearPendingPaste(pending);
            }
        }
    }

    [RelayCommand]
    private void ToggleFavoriteFilter()
    {
        IsFavoriteFilterEnabled = !IsFavoriteFilterEnabled;
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(IClipboardContent? item)
    {
        if (item == null)
        {
            return;
        }

        var nextValue = !item.IsFavorite;
        item.IsFavorite = nextValue;
        await _clipboardHistoryUseCase.UpdateFavoriteAsync(item.Value.Id, nextValue);
        OnPropertyChanged(nameof(SelectedItem));
        ScheduleSearchRefresh("favorite-toggled", force: true, immediate: true);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task DeleteAsync(IClipboardContent? item)
    {
        if (item == null)
        {
            return;
        }

        await _clipboardHistoryUseCase.DeleteAsync(item.Value);
        var visibleIndex = ClipboardItems.IndexOf(item);
        RemoveFromCollections(item);
        _logger.Information("删除项: {Content}", item.Summary);

        // 设置新的选中项：删除后选中前一项，如果删除的是第一项则选中新的第一项
        if (ClipboardItems.Count > 0)
        {
            var newSelectedIndex = visibleIndex > 0 ? visibleIndex - 1 : 0;
            newSelectedIndex = Math.Clamp(newSelectedIndex, 0, ClipboardItems.Count - 1);
            SelectedItem = ClipboardItems[newSelectedIndex];
        }
    }

    private void RemoveFromCollections(IClipboardContent item)
    {
        var allIndex = _allClipboardItems.IndexOf(item);
        if (allIndex >= 0)
        {
            _allClipboardItems.RemoveAt(allIndex);
        }

        var visibleIndex = ClipboardItems.IndexOf(item);
        if (visibleIndex >= 0)
        {
            ClipboardItems.RemoveAt(visibleIndex);
        }
    }

    /// <summary>
    /// 当窗口显示时，总是选中第一个项目
    /// </summary>
    public void OnWindowShown()
    {
        if (ClipboardItems.Count > 0)
        {
            SelectedItem = ClipboardItems[0];
            ScrollToSelectedRequested?.Invoke(this, EventArgs.Empty);
            _logger.Debug("窗口已显示，已选中第一个项目");
        }
    }

    public void SelectRelative(int delta)
    {
        if (ClipboardItems.Count <= 0)
        {
            return;
        }

        var currentIndex = SelectedItem == null ? -1 : ClipboardItems.IndexOf(SelectedItem);
        if (currentIndex < 0)
        {
            SelectedItem = ClipboardItems[0];
            return;
        }

        var nextIndex = Math.Clamp(currentIndex + delta, 0, ClipboardItems.Count - 1);
        if (nextIndex == currentIndex)
        {
            return;
        }

        SelectedItem = ClipboardItems[nextIndex];
        ScrollToSelectedRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 仅触发搜索框聚焦，不修改搜索内容
    /// </summary>
    public void RequestSearchBoxFocus()
    {
        SearchBoxFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    public void BackspaceSearchText()
    {
        if (string.IsNullOrEmpty(SearchQuery))
        {
            return;
        }

        SearchQuery = SearchQuery.Length == 1 ? string.Empty : SearchQuery[..^1];
    }

    private PendingPaste StartPendingPaste(IClipboardContent item)
    {
        var pending = new PendingPaste(item);
        _pendingPaste = pending;
        return pending;
    }

    private async Task<PasteCaptureOutcome?> WaitForPasteCaptureAsync(PendingPaste pending)
    {
        try
        {
            return await pending.Completion.Task.WaitAsync(_pasteDuplicateWaitTimeout);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            ClearPendingPaste(pending);
        }
    }

    private void ClearPendingPaste(PendingPaste pending)
    {
        if (ReferenceEquals(_pendingPaste, pending))
        {
            _pendingPaste = null;
        }
    }

    private static bool IsPayloadMatchingItem(ClipboardPayload payload, ClipboardItem item)
    {
        switch (payload.Type)
        {
            case ClipboardPayloadType.Text:
                if (item.ContentType != ClipboardContentTypes.Text || payload.Text == null)
                {
                    return false;
                }
                return item.Content.SequenceEqual(Encoding.UTF8.GetBytes(payload.Text));

            case ClipboardPayloadType.FileDropList:
                if (item.ContentType != ClipboardContentTypes.FileDropList || payload.FilePaths == null)
                {
                    return false;
                }
                var json = JsonSerializer.Serialize(payload.FilePaths);
                return item.Content.SequenceEqual(Encoding.UTF8.GetBytes(json));

            case ClipboardPayloadType.ImagePng:
                if (item.ContentType != ClipboardContentTypes.Image || payload.ImagePngBytes == null)
                {
                    return false;
                }
                return item.Content.SequenceEqual(payload.ImagePngBytes);

            default:
                return false;
        }
    }

    private async Task MoveItemToTopAsync(IClipboardContent item, string logAction)
    {
        var allIndex = _allClipboardItems.IndexOf(item);
        if (allIndex < 0)
        {
            return;
        }

        if (allIndex == 0)
        {
            SelectedItem = item;
            _logger.Information("{Action}：{Content}", logAction, item.Summary);
            return;
        }

        _allClipboardItems.RemoveAt(allIndex);
        _allClipboardItems.Insert(0, item);

        if (IsVisible(item))
        {
            var visibleIndex = ClipboardItems.IndexOf(item);
            if (visibleIndex >= 0)
            {
                ClipboardItems.RemoveAt(visibleIndex);
            }
            ClipboardItems.Insert(0, item);
        }

        item.Value.CreatedAt = DateTime.Now;
        await _clipboardHistoryUseCase.UpdateAsync(item.Value);
        SelectedItem = item;
        _logger.Information("{Action}：{Content}", logAction, item.Summary);
    }

    private sealed record PendingPaste(IClipboardContent Item)
    {
        public TaskCompletionSource<PasteCaptureOutcome> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private enum PasteCaptureOutcome
    {
        Duplicate,
        Inserted
    }
}

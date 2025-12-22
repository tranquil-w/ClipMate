# Change: 空格键触发搜索框聚焦

## Why
当前按键输入会将空格直接追加到搜索框，导致仅想聚焦时出现多余空格。需要让空格键专用于“唤起搜索”。

## What Changes
- 搜索框未聚焦时，按下空格键触发聚焦但不写入空格
- 视图层与无焦点模式键盘钩子统一空格触发行为
- 补充文档说明空格键快速搜索

## Impact
- Affected specs: search-focus
- Affected code: src/ClipMate/Views/ClipboardView.xaml.cs, src/ClipMate/Services/MainWindowOverlayService.cs, src/ClipMate/ViewModels/ClipboardViewModel.cs

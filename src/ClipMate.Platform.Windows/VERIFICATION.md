# Windows 平台适配器最小验证清单（4.3）

本清单用于验证 `src/ClipMate.Platform.Windows` 迁移后的关键能力在 Windows 环境下仍可用。

## 必需：编译验证

- `dotnet build src/ClipMate.Platform.Windows/ClipMate.Platform.Windows.csproj -c Release`
- `dotnet build src/ClipMate/ClipMate.csproj -c Release`

## 建议：手动回归（轻量）

### 1) 剪贴板监听（`WindowsClipboardChangeSource`）

1. 启动应用
2. 在任意程序中复制：
   - 文本
   - 图片（截图工具或图片文件）
   - 文件（资源管理器复制多个文件）
3. 期望：
   - 列表新增对应条目（不崩溃）
   - 连续快速复制不应造成 UI 卡死

### 2) 选择条目并粘贴（`WindowsClipboardWriter` + `WindowsPasteTrigger`）

1. 打开一个可输入的目标窗口（例如记事本）
2. 调出 ClipMate（主快捷键）
3. 选择一条历史记录并触发粘贴（点击/回车）
4. 期望：
   - 目标窗口收到粘贴内容
   - ClipMate 先隐藏覆盖层（等待窗口不可见）再触发粘贴（无固定延迟）

> 若目标窗口以管理员身份运行，而 ClipMate 非管理员身份，输入模拟可能失败；此时以管理员身份运行 ClipMate 再验证。

### 3) 无焦点窗口行为（`NoActivateWindowController` + `WindowStyle`）

1. 让任意应用保持前台焦点
2. 调出 ClipMate
3. 不点击搜索框时，按上下键/回车操作列表
4. 期望：
   - 前台应用焦点不被抢走
   - ClipMate 能响应快捷键并执行粘贴

### 4) 窗口定位（`WindowPositionCalculator`）

1. 将窗口位置设置为“跟随光标/跟随鼠标/屏幕居中”
2. 多屏或不同 DPI 环境下分别调出窗口
3. 期望：
   - 窗口在可视工作区内，不越界

### 5) 目标窗口记录与切换（`WindowSwitchNative`）

1. 在 A 窗口输入一段文本后切到 B 窗口
2. 调出 ClipMate 并执行“切换到粘贴目标窗口”（如有功能入口）
3. 期望：
   - 能切回最近记录的粘贴目标窗口

### 6) Win+V 边界场景（`WindowsKeyboardHook`）

1. 将主快捷键设置为 Win+V
2. 逐项验证：
   - 先松 V 再松 Win
   - 先松 Win 再松 V
   - 两键几乎同时松开（<50ms）
   - 快速连按（间隔 <200ms）
   - 长按（含自动重复）
3. 期望：
   - 系统剪贴板面板不弹出
   - 开始菜单不因释放顺序/长按/连按误触发
   - 不出现卡键（例如 Shift 被卡住）

### 7) 外部点击关闭（事件驱动）

1. 调出 ClipMate
2. 在窗口外单击一次
3. 期望：窗口快速关闭（主观 <50ms，上限不超过 200ms）
4. 边界：
   - 按住鼠标左键不放 → 调出 ClipMate：不应立刻关闭
   - 在窗口内按住并拖动移动窗口：不应误关

### 8) IME 降级提示

1. 保持无焦点模式（不点击搜索框）
2. 尝试使用输入法进行组合输入（触发 `ImeProcessed`）
3. 期望：
   - 出现提示，引导进入输入模式（点击搜索框）
   - 点击搜索框后可正常使用 IME
4. 设置项：将 `ImeHintsEnabled=false` 后重复步骤 1–3，应不再显示提示


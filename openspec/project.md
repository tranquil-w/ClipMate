# Project Context

> **项目名称**: ClipMate
> **当前版本**: 0.4.0
> **平台**: Windows 10/11（主应用），核心库/测试可跨平台运行
> **目标框架**: .NET 10.0
> **发布方式**: GitHub Actions 自动化 CI/CD
> **解决方案**: `ClipMate.slnx`

## Purpose
ClipMate 是一款功能强大、轻量级的 Windows 剪贴板管理器，主要目标包括：

### 核心功能
- 自动跟踪和存储剪贴板历史记录（支持文本、图片、文件列表）
- 提供智能搜索和过滤功能（实时搜索，支持文件名/扩展名/路径搜索）
- 支持全局快捷键快速访问（可自定义，支持 Win+V）
- 持久化存储剪贴板数据到本地 SQLite 数据库
- 支持文本、图片和文件列表格式的剪贴板内容
- 提供深色/浅色/系统主题切换
- 系统托盘集成，后台运行
- 收藏常用剪贴板内容，永久保留
- 可自定义历史记录保留上限（默认 500 条）

### 高级特性
- **无焦点模式**: 窗口显示时不抢占焦点，保持原应用光标位置不变
- **智能粘贴**: 自动模拟 Ctrl+V 粘贴操作
- **窗口位置管理**: 三种显示模式（跟随光标、跟随鼠标、屏幕中心）
- **项目置顶**: 可将常用项移至列表顶部
- **自动去重**: 智能检测重复内容
- **粘贴后自动删除**: 非收藏项粘贴后可选自动删除
- **开机自启**: 支持注册表和任务计划程序两种方式
- **静默启动**: 启动时不显示窗口
- **管理员权限配置**: 可配置始终以管理员身份运行
- **可配置日志级别**: Error/Warning/Information/Debug

## Tech Stack
- **.NET 10.0** - 目标框架，使用 WPF 构建桌面应用
- **WPF (Windows Presentation Foundation)** - UI 框架
- **Prism 9.0.537** - MVVM 框架，支持模块化架构和依赖注入（Core + DryIoc + Wpf）
- **Microsoft.Data.Sqlite 9.0.0** - SQLite ADO.NET Provider
- **Dapper 2.1.66** - 轻量级 ORM，用于数据库操作
- **CommunityToolkit.Mvvm 8.4.0** - MVVM 工具包，简化 ViewModel 开发
- **HandyControl 3.5.1** - 现代 WPF 控件库，提供美观的 UI 组件
- **Microsoft.Xaml.Behaviors.Wpf 1.1.135** - WPF 行为扩展
- **Serilog 4.3.0** - 结构化日志记录（含 Sinks.Debug 3.0.0, Sinks.File 7.0.0）
- **NHotkey.Wpf 3.0.0** - 全局快捷键管理
- **SharpHook 7.1.0** - 系统级事件监听和键盘/鼠标模拟
- **Microsoft.Extensions.Configuration 9.0.0** - 配置管理（含 Json, Binder）
- **Microsoft.Extensions.DependencyInjection 9.0.0** - 依赖注入扩展

## Project Conventions

### Code Style
- **缩进**: 4 个空格，不使用 Tab
- **行尾**: CRLF（Windows 标准）
- **命名约定**:
  - 类名、方法名、属性名使用 PascalCase
  - 私有字段使用 `_camelCase` 前缀（例如：`_databaseService`）
  - 接口名以 "I" 开头（例如：`IClipboardService`）
  - 局部变量使用 camelCase
  - 常量使用 PascalCase
- **代码格式**: 严格遵循 .editorconfig 配置
- **访问修饰符**: 明确指定所有成员的访问级别
- **空行**: 在方法之间、逻辑块之间使用空行分隔
- **注释**: 使用中文注释，关键方法和复杂逻辑需要详细注释
- **大括号**: Allman 风格（新行开始）
- **var 使用**: 优先使用显式类型声明，仅在类型明显时使用 var
- **表达式体**: 属性、索引器和访问器使用表达式体，方法和构造函数不使用

### Architecture Patterns
- **MVVM 模式**: 使用 Prism 框架实现 Model-View-ViewModel 架构
- **依赖注入**: 使用 Prism 的 DryIoc 容器进行依赖注入，模块化注册（5个 Prism 模块）
- **分层架构**: 清晰的五层架构设计
  - **UI 层** (`ClipMate`): ViewModels + Services + Presentation (WPF/XAML)
  - **业务逻辑层** (`ClipMate.Service`): UseCases + Repositories + Infrastructure
  - **领域模型层** (`ClipMate.Core`): Domain Models + Business Rules
  - **平台抽象层** (`ClipMate.Platform.Abstractions`): 平台无关的接口契约
  - **平台实现层** (`ClipMate.Platform.Windows`): Windows 平台特定实现
- **用例模式**: 业务逻辑封装为独立用例（ClipboardCaptureUseCase, ClipboardHistoryUseCase, ClipboardPasteUseCase）
- **仓储模式**: 数据访问抽象为仓储接口（IClipboardItemRepository → DatabaseClipboardItemRepository）
- **工厂模式**: 剪贴板内容工厂（TextClipboardFactory, ImageClipboardFactory, FileDropListClipboardFactory）
- **适配器模式**: 服务适配器（HotkeyServiceAdapter）
- **接口隔离**: 所有服务、平台功能均通过接口定义契约
- **关注点分离**: 数据模型、业务逻辑、UI 逻辑、平台实现严格分离
- **多项目分层**: 可移植核心库 + 平台特定实现，支持跨平台测试

### Testing Strategy
- **单元测试**: 使用 xUnit 覆盖 Core/Service/Platform Abstractions，以及 UI 层的 ViewModel/服务
- **测试覆盖**: 重点测试核心业务逻辑和剪贴板操作
- **测试数据**: 使用内存数据库进行数据库相关测试
- **测试命名**: 遵循 Arrange-Act-Assert 模式

### Git Workflow
- **分支策略**: 主分支为 `master`，功能分支从 master 创建
- **分支命名**: 使用 `feature/功能名`、`fix/问题描述`、`refactor/重构内容` 等前缀
- **提交信息**: 使用中文提交信息，清晰描述变更内容
  - 格式：`类型: 简短描述`（例如：`feature: 增加设置页面`、`fix: 修复剪贴板监听问题`）
  - 支持的类型：feature（新功能）、fix（修复）、refactor（重构）、docs（文档）、test（测试）、chore（构建/工具）
- **提交频率**: 小步提交，每个提交完成一个完整功能
- **代码审查**: 通过 Pull Request 进行代码审查

### Development Workflow
1. **开发新功能**:
   - 从 master 创建功能分支
   - 实现功能并编写单元测试
   - 确保代码符合 .editorconfig 规范
   - 本地测试通过后提交代码
   - 创建 Pull Request 进行代码审查

2. **修复 Bug**:
   - 创建 fix 分支
   - 编写重现 bug 的测试用例
   - 修复问题并确保测试通过
   - 提交并创建 Pull Request

3. **发布流程**:
   - 更新版本号（在 `src/ClipMate/ClipMate.csproj` 中的 `<AssemblyVersion>` 和 `<FileVersion>`）
   - 运行完整测试套件确保通过
   - 提交版本更新到 master 分支
   - 创建并推送版本标签（格式：`v0.0.x`）
   - GitHub Actions 自动触发构建和发布流程
   - 自动创建 GitHub Release 并上传构建产物（ZIP 文件）

## Domain Context

### 核心概念
- **ClipboardItem**: 剪贴板项目数据模型，包含内容、类型（Text/Image/FileDropList）、创建时间（`CreatedAt`）与收藏状态
- **IClipboardContent**: 剪贴板内容接口，定义文本和图片内容的通用操作
- **剪贴板监听**: 通过 `IClipboardChangeSource`（Windows 实现为 `WindowsClipboardChangeSource`）持续监控剪贴板变化
- **全局快捷键**: 系统级快捷键注册，默认 Win+V 显示/隐藏剪贴板历史（可在设置中自定义）
- **智能粘贴**: 自动模拟 Ctrl+V 粘贴操作（基于 SharpHook）
- **主题系统**: 支持深色/浅色/系统主题切换
- **收藏功能**: 标记常用剪贴板内容为收藏，快速访问和永久保留
- **历史记录上限**: 可在设置中配置历史记录保留数量，自动清理超出部分（收藏项不受影响）
- **无焦点模式**: 窗口显示时不抢占焦点，保持原应用光标位置不变

### 数据流
1. 剪贴板变化 → `WindowsClipboardChangeSource` 触发事件并抽象为 `ClipboardPayload`
2. 业务用例 → `ClipboardCaptureUseCase` 将负载转换为 `ClipboardItem`
3. 数据存储 → `DatabaseService`（Dapper + SQLite）持久化数据并维护索引
4. UI 更新 → ViewModel 刷新列表/搜索/收藏状态
5. 用户操作 → `ClipboardPasteUseCase`/UI 服务写入剪贴板并触发粘贴

### 关键组件

#### 领域模型（ClipMate.Core）
- **ClipboardItem**: 剪贴板项目实体（Id, Content[], ContentType, CreatedAt, IsFavorite）
- **ClipboardContentTypes**: 内容类型常量（Text, Image, FileDropList）
- **WindowPosition**: 窗口位置枚举（FollowCaret, FollowMouse, ScreenCenter）
- **SearchQuerySnapshot**: 搜索查询快照（规范化、性能缓存）

#### 业务用例层（ClipMate.Service/Clipboard）
- **ClipboardCaptureUseCase**: 捕获剪贴板内容用例（去重检查、数据持久化）
- **ClipboardHistoryUseCase**: 历史记录管理用例（查询、更新、删除、清理）
- **ClipboardPasteUseCase**: 粘贴操作用例（写入剪贴板、关闭窗口、触发粘贴）
- **IClipboardItemRepository**: 剪贴板项仓储接口
- **IPasteTargetWindowService**: 粘贴目标窗口服务接口
- **PasteTargetWindowService**: 粘贴目标窗口服务实现

#### 服务接口层（ClipMate.Service/Interfaces）
- **IAdminService**: 管理员权限服务接口
- **IApplicationService**: 应用生命周期服务接口
- **IHotkeyService**: 快捷键服务接口
- **IMainWindowPositionService**: 主窗口位置服务接口
- **ISettingsService**: 设置服务接口
- **IThemeService**: 主题服务接口
- **IWindowSwitchService**: 窗口切换服务接口

#### 窗口服务层（ClipMate.Service/Windowing）
- **MainWindowPositionService**: 主窗口位置服务实现

#### 基础设施层（ClipMate.Service/Infrastructure）
- **DatabaseService**: 数据库初始化、迁移与 CRUD（Dapper + SQLite）
- **DatabaseClipboardItemRepository**: 剪贴板项仓储实现
- **SqliteConnectionFactory**: SQLite 连接工厂

#### ViewModels（ClipMate）
- **ClipboardViewModel**: 主视图模型（列表管理、搜索、收藏、粘贴、置顶）
- **MainWindowViewModel**: 主窗口视图模型（区域导航）
- **SettingsViewModel**: 设置页面视图模型（快捷键、主题、自启动、窗口位置、历史上限、日志级别等）

#### UI 层服务（ClipMate/Services）
- **ClipboardService**: 剪贴板内容工厂服务（创建 IClipboardContent 实例）
- **SettingsService**: 用户设置持久化（JSON 配置，LocalAppData）
- **ThemeService**: 主题应用和系统主题监听（注册表监听）
- **HotkeyServiceAdapter**: 快捷键服务适配器
- **WindowSwitchService**: 窗口焦点跟踪服务
- **ApplicationService**: 应用生命周期控制
- **AdminService**: 管理员权限管理
- **MainWindowOverlayService**: 窗口覆盖管理
- **NotifyIconCommandHandler**: 系统托盘操作

#### Presentation 层（ClipMate/Presentation/Clipboard）
- **ClipboardItemFactory**: 剪贴板内容展示工厂
  - **TextClipboardFactory**: 文本剪贴板工厂
  - **ImageClipboardFactory**: 图片剪贴板工厂
  - **FileDropListClipboardFactory**: 文件列表剪贴板工厂

#### 平台抽象层（ClipMate.Platform.Abstractions）
- **IClipboardChangeSource**: 剪贴板变化监听接口（输出 ClipboardPayload）
- **IClipboardWriter**: 剪贴板写入接口
- **IGlobalHotkeyService**: 全局快捷键注册接口
- **IKeyboardHook**: 全局键盘钩子接口
- **IPasteTrigger**: 粘贴触发接口（模拟 Ctrl+V）
- **IMainWindowController**: 主窗口控制接口
- **IForegroundWindowService**: 前景窗口服务接口
- **IForegroundWindowTracker**: 前景窗口跟踪接口
- **IWindowPositionProvider**: 窗口位置提供者接口
- **ITrayIcon**: 系统托盘图标接口
- **IAutoStartService**: 开机自启服务接口

#### Windows 平台实现（ClipMate.Platform.Windows）
- **WindowsClipboardChangeSource**: Windows 剪贴板监听（WM_DRAWCLIPBOARD, HWND_MESSAGE）
- **WindowsClipboardWriter**: Windows 剪贴板写入
- **WindowsGlobalHotkeyService**: NHotkey 实现的快捷键注册
- **WindowsKeyboardHook**: SharpHook 实现的键盘钩子
- **WindowsPasteTrigger**: Windows 粘贴触发（SendInput API）
- **WpfMainWindowController**: WPF 主窗口控制
- **WindowsForegroundWindowService**: Windows 前景窗口获取
- **WindowsForegroundWindowTracker**: Windows 前景窗口跟踪
- **WindowsWindowPositionProvider**: Windows 窗口位置计算
- **WindowPositionCalculator**: 窗口位置计算辅助类
- **NoActivateWindowController**: 无激活窗口控制器
- **WindowsTrayIcon**: Windows NotifyIcon 实现
- **WindowsAutoStartService**: 支持注册表和任务计划程序

## Important Constraints

### 技术约束
- **平台限制**: 仅支持 Windows 10/11 操作系统（64位）
- **.NET 版本**: 必须使用 .NET 10.0 或更高版本
- **剪贴板访问**: 需要适当的权限访问系统剪贴板
- **数据库**: 使用 SQLite 本地数据库，不支持远程数据库
- **UI 框架**: 使用 WPF，不支持跨平台 UI
- **发布格式**: 自包含应用（win-x64），不依赖系统安装的 .NET 运行时

### 业务约束
- **隐私保护**: 所有剪贴板数据仅存储在本地，不上传云端
- **性能要求**:
  - 剪贴板监控不能影响系统性能
  - UI 操作必须保持响应（使用异步操作处理耗时任务）
  - 数据库查询需要优化，避免阻塞 UI 线程
- **用户体验**:
  - 窗口激活时不抢占焦点，保持流畅操作
  - 支持键盘快捷操作
  - 错误处理要优雅，不能崩溃
- **数据管理**:
  - 支持自定义历史记录保留上限，自动清理旧记录
  - 收藏的项目不受历史记录上限限制，永久保留
  - 定期清理超出上限的普通剪贴板记录
  - 控制数据库大小，避免无限增长

### 安全考虑
- **敏感数据**: 避免记录敏感信息（密码、密钥等）- 可考虑添加黑名单功能
- **数据库安全**: 使用参数化查询防止 SQL 注入
- **权限管理**: 仅请求必要的系统权限
- **异常处理**: 所有异常都应被捕获并记录到日志，避免敏感信息泄露

## External Dependencies

### 核心依赖
- **Prism.Wpf**: MVVM 框架和依赖注入容器
- **HandyControl**: WPF 控件库，提供现代化 UI 组件
- **NHotkey.Wpf**: 全局快捷键注册和管理
- **SharpHook**: 系统级事件监听和键盘事件模拟
- **Microsoft.Data.Sqlite**: SQLite 数据库访问（ADO.NET Provider）
- **Dapper**: 轻量级 ORM，简化数据库操作

### 开发工具依赖
- **CommunityToolkit.Mvvm**: MVVM 工具包，简化属性通知
- **Serilog**: 结构化日志记录
- **xUnit**: 单元测试框架
- **Microsoft.Extensions.Hosting**: 应用程序生命周期管理

## Project Structure

```
ClipMate/
├── src/
│   ├── ClipMate/                         # WPF UI（Windows）
│   │   ├── Composition/                  # DI/模块注册
│   │   ├── Presentation/                 # UI 展示层（内容工厂等）
│   │   ├── Services/                     # UI 侧服务（主题/设置/快捷键等）
│   │   ├── ViewModels/ Views/ Windows/   # MVVM 与窗口
│   │   └── Themes/ Behaviors/ Converters/ Messages/
│   ├── ClipMate.Core/                    # 领域模型与可移植逻辑（搜索等）
│   ├── ClipMate.Service/                 # 用例、仓储与数据库等服务层
│   │   ├── Clipboard/                    # 剪贴板业务用例
│   │   ├── Infrastructure/               # 数据库等基础设施
│   │   ├── Interfaces/                   # 服务接口定义
│   │   └── Windowing/                    # 窗口服务
│   ├── ClipMate.Platform.Abstractions/   # 平台抽象（剪贴板/托盘/启动项等）
│   ├── ClipMate.Platform.Windows/        # Windows 平台实现
│   ├── ClipMate.Tests/                   # UI/ViewModel/集成相关测试
│   ├── ClipMate.Core.Tests/              # Core 单元测试
│   ├── ClipMate.Service.Tests/           # Service 单元测试
│   └── ClipMate.Platform.Abstractions.Tests/ # 平台抽象单元测试
├── openspec/                    # OpenSpec 文档和规范
└── bin/                         # 编译输出目录
```

### 关键文件位置
- **应用程序入口**: `src/ClipMate/App.xaml.cs`
- **主窗口**: `src/ClipMate/MainWindow.xaml`
- **剪贴板视图**: `src/ClipMate/Views/ClipboardView.xaml`
- **剪贴板服务**: `src/ClipMate/Services/ClipboardService.cs`
- **数据库服务**: `src/ClipMate.Service/Infrastructure/DatabaseService.cs`
- **剪贴板监听（Windows）**: `src/ClipMate.Platform.Windows/Clipboard/WindowsClipboardChangeSource.cs`
- **配置文件**: `src/ClipMate/appsettings.json`
- **代码风格配置**: `.editorconfig`
- **CI/CD 工作流**: `.github/workflows/publish.yml`
- **测试**: `src/*Tests/` 目录（例如 `src/ClipMate.Service.Tests/`）

## Configuration

### appsettings.json 示例
```json
{
    "ConnectionStrings": {
        "ClipMateDb": "Data Source=ClipMate.db"
    },
    "HotKey": "Win + V",
    "FavoriteFilterHotKey": "Win + B",
    "Theme": "System",
    "WindowPosition": "FollowCaret",
    "AutoStart": false,
    "SilentStart": false,
    "AlwaysRunAsAdmin": false,
    "HistoryLimit": 500,
    "ClipboardItemMaxHeight": 100,
    "EnableWinComboGuardInjection": true,
    "ImeHintsEnabled": true,
    "LogLevel": "Information"
}
```

**配置项说明**:
- `HotKey`: 显示/隐藏主窗口的快捷键
- `FavoriteFilterHotKey`: 切换收藏过滤的快捷键
- `Theme`: 主题设置（"Light", "Dark", "System"）
- `WindowPosition`: 窗口位置模式（"FollowCaret", "FollowMouse", "ScreenCenter"）
- `AutoStart`: 是否开机自启
- `SilentStart`: 是否静默启动（启动时不显示窗口）
- `AlwaysRunAsAdmin`: 是否始终以管理员身份运行
- `HistoryLimit`: 历史记录保留上限
- `ClipboardItemMaxHeight`: 剪贴板项最大显示高度
- `EnableWinComboGuardInjection`: 是否启用 Win 组合键防护
- `ImeHintsEnabled`: 是否启用输入法提示
- `LogLevel`: 日志级别（"Error", "Warning", "Information", "Debug"）

> 运行时会将 `ClipMate.db` 映射到用户 AppData 目录下的实际路径（见 `src/ClipMate/Composition/InfrastructureModule.cs`）。

### 数据库架构
```sql
CREATE TABLE ClipboardItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Content BLOB NOT NULL,        -- 二进制数据（文本/图片/文件列表）
    ContentType TEXT NOT NULL,    -- 'Text' / 'Image' / 'FileDropList'
    CreatedAt DATETIME NOT NULL,  -- 创建时间戳
    IsFavorite INTEGER NOT NULL DEFAULT 0,  -- 是否收藏 (0/1)
    ContentHash TEXT NULL         -- 内容哈希（用于去重检测）
);

-- 索引
CREATE INDEX IX_ClipboardItems_CreatedAt ON ClipboardItems(CreatedAt);
CREATE INDEX IX_ClipboardItems_ContentType_CreatedAt ON ClipboardItems(ContentType, CreatedAt);
CREATE INDEX IX_ClipboardItems_IsFavorite_CreatedAt ON ClipboardItems(IsFavorite, CreatedAt);
CREATE INDEX IX_ClipboardItems_ContentHash ON ClipboardItems(ContentHash);
```

> 数据库迁移：应用启动时会自动检测并添加 `ContentHash` 列（兼容旧版本数据）。

### 常用构建和发布命令

#### 本地开发
```bash
# 构建/测试解决方案
dotnet build ClipMate.slnx --configuration Debug
dotnet test ClipMate.slnx --verbosity normal

# 运行应用程序（仅 Windows）
dotnet run --project src/ClipMate/ClipMate.csproj
```

本地发布（Windows PowerShell / pwsh）：
```powershell
dotnet publish src/ClipMate/ClipMate.csproj `
  --configuration Release `
  --output Publish `
  --self-contained true `
  --runtime win-x64 `
  --property:PublishSingleFile=false `
  --property:PublishTrimmed=false
```

#### 正式发布流程
```bash
# 1. 更新版本号（编辑 src/ClipMate/ClipMate.csproj）
# 修改 <AssemblyVersion> 和 <FileVersion>

# 2. 提交版本更新
git add src/ClipMate/ClipMate.csproj
git commit -m "chore: bump version to 0.0.x"
git push origin master

# 3. 创建并推送版本标签
git tag v0.0.x
git push origin v0.0.x

# 4. GitHub Actions 自动执行以下操作：
#    - 构建解决方案
#    - 运行测试
#    - 发布自包含应用（win-x64）
#    - 创建 ZIP 压缩包
#    - 创建 GitHub Release 并上传构建产物
```

#### CI/CD 配置
- **工作流文件**: `.github/workflows/publish.yml`
- **触发条件**:
  - 推送版本标签 `v*`（完整的构建、测试、发布流程）
- **构建环境**: Windows Latest + .NET 10.0（构建/发布），Ubuntu/macOS + .NET 10.0（可移植测试）
- **发布格式**: 自包含应用（win-x64）、ZIP 压缩包

## CI/CD Pipeline

### GitHub Actions 工作流详解

项目使用 GitHub Actions 实现自动化构建、测试和发布流程。

#### 工作流触发条件
1. **推送版本标签 (v*)**: 执行可移植测试（ubuntu/macOS）、Windows 构建与测试、打包与 Release 创建

#### 构建流程 (build job)
1. **检出代码**: 使用 `actions/checkout@v4`
2. **设置 .NET 环境**: 安装 .NET 10.0 SDK
3. **恢复依赖**: `dotnet restore ClipMate.slnx`
4. **构建解决方案**: Release 配置编译所有项目
5. **运行测试**: 执行 ClipMate.Tests 中的所有单元测试
6. **发布应用**: （仅标签触发）创建自包含的 win-x64 应用
7. **创建压缩包**: 打包成带版本号和时间戳的 ZIP 文件
8. **上传构建产物**: 保存到 GitHub Artifacts（保留 30 天）

#### 可移植测试流程 (portable job)
1. **检出代码**: 使用 `actions/checkout@v4`
2. **设置 .NET 环境**: 安装 .NET 10.0 SDK
3. **恢复/构建/测试**: 在 ubuntu/macOS 上运行 Core/Service/Platform Abstractions 的测试项目

#### 发布流程 (release job)
1. **下载构建产物**: 获取 build job 生成的 ZIP 文件
2. **创建 GitHub Release**: 使用 `softprops/action-gh-release@v2`
   - 上传 ZIP 文件作为发布资产
   - 自动生成 Release Notes（基于提交记录）
   - 公开发布（非草稿、非预发布）

#### 版本管理最佳实践
- **版本号格式**: 使用语义化版本 `v主版本.次版本.修订号`（例如：v0.0.7）
- **同步版本号**:
  - `.csproj` 文件中的 `<AssemblyVersion>` 和 `<FileVersion>`
  - Git 标签的版本号应与 `.csproj` 中的版本号一致
- **发布前检查清单**:
  - [ ] 所有测试通过
  - [ ] 代码已合并到 master
  - [ ] 版本号已更新
  - [ ] CHANGELOG 或提交信息清晰描述了变更内容

#### 故障排查
- **构建失败**: 检查 GitHub Actions 日志，查看具体错误信息
- **测试失败**: 工作流会自动停止，不会创建发布
- **Release 创建失败**: 确保仓库有 `contents: write` 权限
- **手动重新运行**: 可在 GitHub Actions 页面重新运行失败的工作流

## Common Issues & Solutions

### 开发常见问题

1. **剪贴板监听不工作**
   - 检查 `IClipboardChangeSource`（Windows 为 `WindowsClipboardChangeSource`）是否正确注册到 DI 容器（见 `src/ClipMate/Composition/PlatformWindowsModule.cs`）
   - 确认窗口句柄已正确创建
   - 查看日志文件（Serilog）获取详细错误信息

2. **全局快捷键冲突**
   - 使用 SettingsViewModel 允许用户自定义快捷键
   - 检查是否有其他应用占用了相同的快捷键组合
   - NHotkey.Wpf 会在注册失败时抛出异常

3. **数据库锁定问题**
   - 确保所有数据库操作使用异步方法
   - 使用 `using` 语句正确释放数据库连接
   - 避免在 UI 线程上执行长时间的数据库操作

4. **图片剪贴板显示问题**
   - 检查图片格式是否支持（BMP、PNG、JPEG）
   - 确认内存中正确存储了图片数据
   - 验证 XAML 绑定是否正确

5. **窗口焦点问题**
   - 使用 ShowActivated="False" 防止窗口抢占焦点
   - WindowSwitchService 负责管理窗口激活逻辑
   - 检查 WPF 窗口的 Topmost 属性设置

6. **历史记录自动清理不生效**
   - 确认在设置中已正确配置历史记录上限
   - 检查收藏的项目不会被清理
   - 查看日志确认清理任务是否正常执行

7. **GitHub Actions 构建失败**
   - 检查解决方案文件路径是否正确（`ClipMate.slnx`）
   - 确认所有项目引用的 NuGet 包都可以正常恢复
   - 查看 Actions 日志中的详细错误信息
   - 本地测试发布命令是否能成功执行

### 调试技巧

- **日志查看**: 检查应用程序目录下的日志文件（Serilog 输出）
- **断点调试**: 在 Visual Studio 中设置断点，特别关注服务层和 ViewModel
- **数据库检查**: 使用 SQLite 客户端工具（如 DB Browser for SQLite）查看数据库内容
- **性能分析**: 使用 Visual Studio Profiler 检查性能瓶颈
- **内存泄漏**: 注意事件订阅和取消订阅，避免内存泄漏

## Useful References

### 官方文档
- [.NET 10.0 文档](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
- [WPF 官方文档](https://learn.microsoft.com/dotnet/desktop/wpf/)
- [Prism 框架文档](https://prismlibrary.com/docs/)
- [Dapper 文档](https://github.com/DapperLib/Dapper)
- [GitHub Actions 文档](https://docs.github.com/actions)

### 第三方库
- [HandyControl](https://github.com/HandyOrg/HandyControl) - WPF UI 控件库
- [NHotkey.Wpf](https://github.com/thomaslevesque/NHotkey) - 全局快捷键
- [SharpHook](https://github.com/TolikPylypchuk/SharpHook) - 键盘/鼠标钩子
- [Serilog](https://serilog.net/) - 结构化日志记录

### 相关资源
- [MVVM 模式最佳实践](https://learn.microsoft.com/dotnet/architecture/maui/mvvm)
- [SQLite 最佳实践](https://www.sqlite.org/bestpractice.html)
- [WPF 性能优化指南](https://learn.microsoft.com/dotnet/desktop/wpf/advanced/optimizing-performance-application-resources)

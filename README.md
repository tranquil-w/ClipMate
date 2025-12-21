# ClipMate - Windows 剪贴板管理器

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows%20Presentation%20Foundation-0078D4)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

一款轻量、顺手的 Windows 剪贴板管理器，自动记录历史、快捷唤起、即点即贴。

## 功能特性

- **自动剪贴板监控** - 后台持续跟踪剪贴板变化
- **智能搜索** - 输入即刻过滤历史记录
- **全局快捷键** - `Win+V` 唤出/隐藏（可自定义）
- **智能粘贴** - 点击项目直接粘贴，无需手动 Ctrl+V
- **多格式支持** - 文字、图片、文件列表都能保存
- **收藏功能** - 常用内容标记收藏，永不丢失
- **历史记录管理** - 可自定义保留数量上限
- **深色主题** - 夜间观感更舒适
- **系统托盘集成** - 安静驻留，不打扰
- **无焦点模式** - 调用时不打断当前窗口光标

## 快速开始

- **系统要求**: Windows 10/11
- **安装**:
  1) 前往 [Releases](../../releases) 下载最新包  
  2) 解压到任意位置  
  3) 运行 `ClipMate.exe`，程序会驻留在托盘
- **使用**:
  - 按 `Win+V` 显示历史列表
  - 输入关键词快速搜索
  - 点击任意条目即可粘贴
  - 按 `Esc` 或点击窗口外关闭

## 个性化

- 在设置里可调整快捷键、开机自启、主题模式
- 数据仅保存在本地，方便自行备份或迁移

## 调试环境变量（可选）

- `CLIPMATE_SEARCH_WARN_MS`：搜索过滤慢操作警告阈值（毫秒）
- `CLIPMATE_SEARCH_DIAGNOSTICS`：启用搜索诊断日志（`1/true` 开启）
- `CLIPMATE_QUERY_WARN_MS`：数据库慢查询警告阈值（默认 `100`，毫秒）
- `CLIPMATE_QUERY_DIAGNOSTICS`：启用数据库查询诊断日志（`1/true` 开启）

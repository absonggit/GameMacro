# Windows 安装程序设计

## 目标

使用 Inno Setup 将自包含的 win-x64 发布结果打包为单个 `GameMacro-Setup.exe`，方便普通用户安装和卸载。

## 安装行为

- 当前用户安装，默认目录为 `%LOCALAPPDATA%\Programs\GameMacro`，不请求管理员权限。
- 创建开始菜单快捷方式，并提供可选桌面快捷方式。
- 安装完成后可直接启动程序。
- Windows“已安装的应用”中提供标准卸载入口。
- 升级覆盖程序文件，不删除 `%LOCALAPPDATA%\GameMacro\Profiles`。
- 安装包不包含开发者本机方案；方案通过程序内单方案导入导出传递。

## 构建

- `scripts/build-installer.ps1` 先发布 self-contained win-x64 文件到临时发布目录。
- 再调用 Inno Setup 命令行编译器 `ISCC.exe`。
- 最终文件输出为 `artifacts\installer\GameMacro-Setup.exe`。
- 初始版本号为 `1.0.0`。

## 限制

- 安装包未进行数字签名，可能出现 SmartScreen 提示。
- 只支持 x64 Windows。

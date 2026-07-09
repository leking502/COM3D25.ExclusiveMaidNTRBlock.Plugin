# COM3D25 Exclusive Maid NTR Block Plugin

[English](README.md)

【自制插件】屏蔽专属女仆的 NTR 内容

这是一个用于 COM3D2.5 的 BepInEx 插件。

当女仆的契约状态是 `MaidStatus.Contract.Exclusive` 时，插件会屏蔽她相关的 NTR 内容。普通女仆不受影响。

## 下载

从 GitHub Releases 下载最新 DLL：

https://github.com/leking502/COM3D25.ExclusiveMaidNTRBlock.Plugin/releases

插件文件：

`COM3D25.ExclusiveMaidNTRBlock.Plugin.dll`

## 主要功能

- 屏蔽专属女仆的剧情 NTR 事件
- 屏蔽自由模式日常 NTR 事件
- 屏蔽私有模式 NTR 事件
- 屏蔽 EmpireLife 相关 NTR 内容
- 屏蔽日程与设施里的 NTR 任务
- 屏蔽傅き模式中专属女仆接待主人公以外顾客
- 屏蔽 Honeymoon 中的 NTR 事件
- 屏蔽夜伽职业、夜伽技能列表、技能选择和结果页里的 NTR 项目
- 普通女仆不受影响

## 安装方法

1. 从 Releases 页面下载 `COM3D25.ExclusiveMaidNTRBlock.Plugin.dll`
2. 把 DLL 放入游戏的 `BepInEx/plugins/` 目录
3. 启动游戏

## 配置

游戏内按 `F10` 可以打开或关闭插件配置窗口。

可以单独启用或关闭这些模块：

- 剧情事件
- 自由模式日常事件
- 私有模式事件
- EmpireLife
- 日程与设施
- 傅き模式
- Honeymoon
- 夜伽职业列表
- 夜伽技能列表
- 正常夜伽技能选择
- 夜伽结果页
- 自由夜伽技能选择

## 说明

插件不会修改玩家存档里的全局 NTR 开关，也不会长期改变游戏的 `lockNTRPlay` 状态。

它只在能判断当前女仆是专属女仆时生效，尽量避免影响普通女仆和全局菜单。

当前版本属于初期版本，Release DLL 已编译通过。游戏入口很多，如果遇到遗漏场景，欢迎在 Issues 或论坛回帖反馈。

## 从源码构建

项目需要引用本机 COM3D2.5 和 BepInEx 的程序集。

构建前任选一种方式设置游戏目录：

- 把 `Directory.Build.props.example` 复制为 `Directory.Build.props`，然后把 `COM3D25GameDir` 改成你的游戏目录。
- 设置环境变量 `COM3D25_GAME_DIR`，值为你的游戏目录。

然后构建：

```powershell
dotnet build .\ExclusiveMaidNTRBlock.csproj -c Release
```

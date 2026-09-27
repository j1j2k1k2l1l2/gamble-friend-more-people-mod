# Gamble With Your Friends — 无限联机人数 Mod

解除游戏《Gamble With Your Friends》1–6 人联机上限的 BepInEx 插件,默认上限 **32 人**(2–250 可调)。

**只有房主(创建房间的人)需要安装本 mod**,加入房间的玩家无需安装。

## 安装(直接使用)

1. 下载 [`dist/UnlimitedPlayers-1.0.0.zip`](dist/UnlimitedPlayers-1.0.0.zip)
2. 把压缩包内的**所有文件**解压到游戏根目录(即 `Gamble With Your Friends.exe` 所在目录),解压后游戏目录下应出现 `winhttp.dll` 和 `BepInEx` 文件夹
3. 通过 Steam 正常启动游戏

Steam 默认路径示例:

```
D:\steam\steamapps\common\Gamble With Your Friends\
```

## 修改人数上限

编辑 `BepInEx\config\local.gwyf.unlimitedplayers.cfg`(首次运行游戏后生成):

```ini
[General]
MaxPlayers = 32   # 范围 2–250
```

保存后重启游戏生效。

## 验证

查看 `BepInEx\LogOutput.log`,应有:

```
Loading [Unlimited Players 1.0.0]
LobbySettings.maxPlayers: 6 -> 32
NetworkManager.maxConnections: 12 -> 32
```

点击"创建游戏/联机"后还应出现:

```
Steam CreateLobby: 6 -> 32
NetworkServer.Listen: 12 -> 32
```

邀请第 7 位好友加入即验证成功。

## 已知限制

- 部分**小游戏/场景的座位、桌子、UI 布局是按 6 人设计的**,人数远超 6 时可能出现玩家重叠、没有座位、撞色等现象(这是美术/关卡资源层面的限制,网络层已无限制)
- 人数很多时(>10)网络流量和性能取决于房主机型

## 原理

游戏的人数限制分布在 4 处,mod 用 Harmony 补丁全部处理:

| 限制点 | 原值 | 处理方式 |
|---|---|---|
| Steam 大厅人数上限 `SteamMatchmaking.CreateLobby` | 6 | Harmony 前缀补丁,替换参数 |
| Mirror 网络层 `NetworkServer.Listen` / `ServerAccept` | 12 | Harmony 前缀补丁 + 场景加载时同步字段 |
| 传输层 FizzySteamworks 内部拒绝 | 读 `NetworkManager.maxConnections` | 场景加载时 + `NetworkManager.SetupServer` 前缀同步字段 |
| `LobbySettings.maxPlayers`(ScriptableObject 资产) | 6 | 场景加载时运行时改写(Steam 好友列表/RichPresence 也读它) |

## 从源码构建

需要:安装了本游戏的 Windows 机器、[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

1. 先按上文安装 zip 包(构建需要引用游戏目录下的 `BepInEx\core\` 与 `Gamble With Your Friends_Data\Managed\`)
2. 克隆本仓库到游戏目录的 `_ModDev\gamble-friend-more-people-mod`(默认路径即按此布局解析)
3. 构建:

```
cd _ModDev\gamble-friend-more-people-mod\src
dotnet build -c Release
```

若仓库不在游戏目录下,指定游戏根目录再构建:

```
set GWYF_GAME_DIR=D:\steam\steamapps\common\Gamble With Your Friends\
dotnet build -c Release
```

产物 `bin/Release/netstandard2.1/UnlimitedPlayers.dll` 复制到游戏目录 `BepInEx/plugins/` 即可。

> 注意:如果游戏本体与 Doorstop 老版本不兼容(启动即崩溃 0xc0000005),请使用本仓库 `dist` 包内附带的 Doorstop 4.5.0 `winhttp.dll`(BepInEx 5.4.23.2 自带的版本在该游戏(Unity 6000.3.6)上会崩溃)。

## 卸载

删除游戏根目录下的:`winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`BepInEx/` 文件夹。

## 许可

本 mod 源码以 MIT 许可发布。

发布包内捆绑的第三方组件:
- [BepInEx 5.4.23.2](https://github.com/BepInEx/BepInEx) — LGPL-2.1
- [Unity Doorstop 4.5.0](https://github.com/NeighTools/UnityDoorstop) — MIT(附带修改版 `doorstop_config.ini`)

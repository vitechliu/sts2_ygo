# 主菜单音乐管线

主菜单事件为 `event:/vygo/music/main_menu`：

- 音源：`Assets/BGM_MENU_01.ogg`
- 播放方式：单曲循环
- Mixer Routing：`music`，因此跟随游戏 BGM 音量
- Bank：`VYgo`

Windows 下在仓库根目录执行：

```powershell
./FMOD/build_vygo_audio.ps1
```

脚本会构建 FMOD 工程、导出 GUID，并将 `VYgo.bank` 与 GUID 映射同步到 `VYgo/banks/`。Master Bank 只是 FMOD 构建依赖，不会复制或打包到模组中。若 FMOD Studio 安装位置不同，可用 `-FmodStudioCli` 指定 `fmodstudiocl.exe`。

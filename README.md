# Screenshot Pin

一个 Windows 截图锚定小工具。启动后常驻系统托盘，当你使用 `Win + Shift + S` 或 `PrintScreen` 截图并把图片放入剪贴板后，程序会自动生成一个锚定窗口，方便拖到旁边参考或直接标注。

## 当前功能

- 自动监听截图动作后的剪贴板图片
- 自动锚定只响应 `Win + Shift + S` / `PrintScreen` 后产生的图片；普通复制网页图片、PPT 图片等不会触发
- 只在剪贴板内容看起来是独立图片时读取；复制文字、HTML/RTF、文件、Office/PPT 对象不会触发锚定
- 每张截图生成一个独立锚定窗口
- 可在托盘菜单里选择新截图窗口是否默认置于最上层
- 可在单个截图窗口中用 `Top` 按钮切换当前窗口是否置顶
- 可用 `Min` 按钮最小化单个截图窗口
- 托盘菜单支持恢复最小化窗口
- Move 模式下拖动窗口，双击恢复接近原图大小
- 鼠标滚轮缩放，右下角拖拽缩放
- Pen 模式下红色画笔标注
- Erase 模式下擦除标注
- Undo 撤销，Clear 清空标注
- Copy 复制合成后的图片
- Save 保存为 PNG
- 托盘菜单支持开关自动锚定、手动锚定当前剪贴板、关闭全部窗口、退出

## 编译

本项目使用 `.NET Framework 4.7.2 + WPF`，不需要 .NET SDK。可以直接用你本机的 Visual Studio 2022 或 MSBuild 编译：

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\ScreenshotPin.sln /p:Configuration=Release
```

编译后运行：

```powershell
.\bin\Release\ScreenshotPin.exe
```

## 使用

1. 启动 `ScreenshotPin.exe`
2. 按 `Win + Shift + S` 或 `PrintScreen` 截图
3. 截图完成后会自动锚定在屏幕上
4. 在截图窗口左上角切换 Move / Pen / Erase / Top 等工具
5. 点击 `Min` 可最小化当前截图窗口，需要时从任务栏或托盘菜单恢复
6. 如果想临时锚定普通复制图片，可以从托盘菜单点击“锚定当前剪贴板图片”

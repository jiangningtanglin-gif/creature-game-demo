# 精灵收集 · Unity Demo（Android APK）

原创精灵收集游戏 Demo：动漫风 3D 草原环境 + 三只精灵（苔铃兽/烬尾貘/浮鳐灵）
游荡待机 + 点击查看图鉴 + 精灵蛋孵化。竖屏，手机点开即玩。

## 目录

- `Assets/Models/`：三只精灵 GLB（Tripo API 精细贴图版，约 50 万面/只）
- `Assets/Scripts/Runtime/`：游戏逻辑
  - `GameManager.cs`：精灵定义、出生、镜头焦点
  - `AnimeEnvironment.cs`：程序化动漫风环境（地形/天空/树/草/远山/云）
  - `CreatureWander.cs`：游荡 AI + 呼吸浮动（无骨骼，程序化表现）
  - `DexUI.cs`：uGUI 界面（标题/切换按钮/图鉴卡/孵化按钮）
  - `EggHatch.cs`：精灵蛋孵化流程 + **支付钩子**
  - `TouchOrbit.cs`：触屏旋转/缩放相机
- `Assets/Scripts/Editor/`
  - `ProjectSetup.cs`：菜单「精灵收集 / 一键搭建场景」——整个场景由代码生成
  - `BuildGame.cs`：菜单「精灵收集 / 打包 Windows」（主）/「精灵收集 / 打包 APK」（顺带）+ 命令行 headless 打包
- `Assets/Fonts/NotoSansSC.ttf`：开源中文字体（OFL），解决 Unity 默认字体不显示中文

## 在 Unity 编辑器里跑

1. Unity Hub 打开本项目（建议 2022.3 LTS）。
2. 等右下角导入完成（glTFast 导入 3 个 GLB + 字体）。
3. 菜单「精灵收集 / 一键搭建场景」→ 生成 `Assets/Scenes/Main.unity`。
4. 点 Play 直接在编辑器里玩。

## 打包（Windows 为主，Android 顺带）

- 编辑器：菜单「精灵收集 / 打包 Windows」（产物 `Builds/Windows/creature-game.exe`）
  或「精灵收集 / 打包 APK」（产物 `Builds/creature-game.apk`）。
- 命令行（需 Unity 已激活 license）：
  `Unity -batchmode -quit -projectPath . -executeMethod BuildGame.BuildWindows`
- 包名 `com.bo.creaturegame`，Windows 64 位 / Android ARM64，竖屏。
  Demo 包用 Mono 后端（构建快、内存省），正式 release 再切 IL2CPP。

## 商业化钩子

`EggHatch.requirePayment`：默认 false（Demo 直接孵化）。
正式上线改为 true，并在 `PaymentGranted()` 里接入微信/支付宝支付 SDK，
支付成功才调 `StartHatch()`。未来“定制精灵”在此处支付成功后调 Tripo API
按次生成（API key 走服务端，别进客户端）。

## 待办（Demo 之后）

- 模型减面：现在每只 50 万面，手机实战建议减到 5~10 万面。
- 骨骼动画：现在是无骨骼程序化位移；要走路/攻击动作再绑骨（Tripo 网页版一键绑定约 20 点/只）。

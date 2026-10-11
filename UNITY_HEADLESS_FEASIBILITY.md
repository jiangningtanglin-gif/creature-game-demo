# Unity Headless 安装可行性报告（Android APK 命令行构建）

调研日期：2026-10-11 ｜ 目标：在本 VM（Ubuntu 24.04, 无 GPU）上无界面安装 Unity Editor，用命令行打出 Android APK
**结论：CONDITIONAL GO（条件通过）** —— 路径已验证可行，但有 3 个需要处理的坑（内存、/tmp、License 续期）。

## 1. 机器现状

| 项目 | 实测值 | 结论 |
|---|---|---|
| 磁盘 /home/hatch | 100G 总量，可用 **92G** | ✅ 充足（Unity 全套约需 25–30G，见 §4） |
| /tmp | tmpfs **512M** | ⚠️ 太小，必须设 `TMPDIR=~/workspace/tmp`（见 §6） |
| 内存 | 7.7G 总量，当前可用仅 ~0.36G | ⚠️ **最大风险**，见 §6 |
| CPU | 2 核 | ⚠️ 构建慢但可接受（IL2CPP 首包预计 15–40 分钟） |
| 系统 | Ubuntu 24.04.5 LTS, glibc 2.39, x86_64 | ✅ |
| GPU | 无 | ✅ batchmode 构建不需要 GPU |
| 网络 | egress 代理可达 `download.unity3d.com`（HTTP 404=主机通）与 `public-cdn.cloud.unity3d.com`（HTTP 200，2026-10-11 实测） | ✅ 下载没问题 |

## 2. 下载地址（官方）

**推荐路径：Unity 官方 Hub CLI（beta，原生 headless，无需登录即可下载编辑器）**
- CLI 安装脚本（2026-10-11 实测 HTTP 200）：`https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh`
- 安装：`curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash` → 装到 `~/.local/bin/unity`
- 一条命令装编辑器 + Android 模块（含子模块 JDK/SDK/NDK）：
  `unity install 2022.3.62f1 -m android --cm -y --accept-eula`
- 已有编辑器后补模块：`unity install-modules -e 2022.3.62f1 -m android --cm --accept-eula --non-interactive -y`
- 依据：2026-10-09 有人在 **Ubuntu 24.04 容器**（与本机同系统）用此 CLI 全程 headless 装好 Unity 并打出构建包（dev.to 场记报告）；另有多个第三方 skill 文档确认 `-m android --cm` 用法。

**手动 tarball 路径（备用，不推荐）**：URL 形如
- `https://download.unity3d.com/download_unity/<CHANGESET>/LinuxEditorInstaller/Unity.tar.xz`
- `https://download.unity3d.com/download_unity/<CHANGESET>/TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-2022.3.62f1.tar.xz`

其中 `<CHANGESET>` 是版本哈希，必须去该版本的 release notes 页（unity.com → releases → 2022.3.62f1，"Changeset:" 一行）查，**不能猜**。本次调研未拿到经验证的哈希值——用 CLI 路径可完全绕开这个问题。

**不推荐**：旧 Unity Hub AppImage 的 `--headless install`（Hub 3.18 已标记废弃，且有报告称 Android 模块会卡在 "queued for install" 永远不动）。

## 3. License 激活流程（Unity Personal，headless）

**主流程（CLI OAuth，2026-10 验证可行）：**
1. `unity auth login` —— 打印一次性授权 URL 并轮询（**有效窗口约 5 分钟**）
2. **Bo 的操作**：在自己浏览器打开该 URL，点 "Allow Login Request"（需先有免费 Unity ID，没有就花 2 分钟注册一个）
3. `unity license activate --personal --accept-eula`
4. `unity license status` 确认 `License: active (Unity Personal)`

**Bo 需要做的就这两步**：① 有个免费 Unity ID；② 在 5 分钟内点一次授权链接。之后全是命令行的事。

**备选（经典手动激活，.alf→.ulf）——目前不推荐：**
`Unity -batchmode -createManualActivationFile` 生成 `.alf` → 传到 `license.unity3d.com/manual-activation`（需 Bo 登录）→ 下载 `.ulf` → `Unity -batchmode -manualLicenseFile xxx.ulf`。
⚠️ 2026-10-09 场记报告：Personal 账号在该页面陷入登录重定向死循环，拿不到 `.ulf`。Plus/Pro 未验证。先走 CLI OAuth，手动流程只当备胎。

**License 持久化注意**：`~/.config/unityhub/`（accounts.db 等）必须保留（它在 `~` 下，VM 替换不丢）；有报告称 Personal 授权约 24 小时后失效——**每次构建前先跑 `unity license status`，失效就重跑 activate**（重激活不需要重新走浏览器）。

## 4. 磁盘占用估算

| 组件 | 大小 |
|---|---|
| Hub CLI | 可忽略 |
| Editor 2022.3（tarball ~4G，解压后 ~8G） | ~8G |
| Android Build Support + SDK & NDK Tools + OpenJDK（`--cm` 子模块） | ~8–12G |
| Unity 项目 + Library + 构建产物 | ~5–10G |
| **合计** | **约 25–30G**（可用 92G，充足） |

## 5. Android 构建依赖（Unity 2022.3 LTS）

`--cm` 子模块会自动装好以下**官方推荐版本**，无需手动装：
- **OpenJDK 11**（Unity 2022.x Android 要求 JDK 11，JDK 8 已不支持）
- **Android SDK**：min API 24，target API 33（platform android-33、build-tools 33.0.x、platform-tools、cmdline-tools）
- **Android NDK r23c**（23.1.7779620，2022.3 LTS 官方指定版本）
- **Gradle**：Unity 自带发行版，无需手动安装

构建命令示例（Editor C# 脚本 `BuildPipeline.BuildPlayer(..., BuildTarget.Android, ...)`，经 `Unity -batchmode -nographics -projectPath ... -executeMethod BuildGame.Build -logFile -` 调用）。debug APK 无需 keystore（Unity 自动用 debug 签名），直接装到 Bo 手机上测。

## 6. 预见的 blocker 与对策

1. **内存（最严重）**：7.7G 总量、当前可用仅 0.36G。Editor batchmode + Android IL2CPP（clang）在 2 核小内存上可能 OOM。
   对策：① 建 8G swapfile（root 身份、磁盘充足，一条命令）；② 首包用 **Mono** scripting backend 打 debug 版（比 IL2CPP 省很多内存，2022.3 仍可选，构建时验证）；③ 在机器空闲时构建；④ IL2CPP 留到 release 包再用。
2. **/tmp 只有 512M**：安装/构建前 `export TMPDIR=~/workspace/tmp`（并 `mkdir -p`）。
3. **Ubuntu 24.04 vs Unity 2022.3**：2022.3 官方支持的是 20.04/22.04，24.04 上大概率能跑但可能缺个别旧库。**建议**：直接上 **Unity 6（6000.x）LTS**——它是当前 LTS，且 2026-10-09 场记在 Ubuntu 24.04.5 上全程验证过；除非项目有必须 2022.3 的理由。
4. **License 续期**：见 §3，每次构建前检查状态。
5. **下载速度**：代理通，但 4–10G 下载的实际速度未知，首次安装时实测；断点续传由 CLI 处理。
6. **VM 整机替换**：只有 `~` 持久化——所有东西（CLI、编辑器、项目、License 状态）默认都在 `~` 下（`~/.local/bin`、`~/Unity/Hub`、`~/.config/unityhub`、`~/workspace/creature-game-unity`），天然合规；不要装到 /opt 或 /usr/local。

## 7. Bo 需要做的事（清单）

1. 注册免费 Unity ID（unity.com，2 分钟；已有则跳过）。
2. 安装当天：我跑 `unity auth login` 后把一次性 URL 发他，他在**5 分钟内**用浏览器打开并点 "Allow Login Request"。
3. 之后：无。License 续期、构建、传 APK 下载链接都在我这边。

## 8. 实施命令序列（给后续执行用）

```bash
export TMPDIR=~/workspace/tmp && mkdir -p $TMPDIR
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
export PATH="$HOME/.local/bin:$PATH"
unity install 2022.3.62f1 -m android --cm -y --accept-eula   # 或 6000.x LTS（推荐，见§6.3）
unity auth login            # → 把 URL 发 Bo，5 分钟内点授权
unity license activate --personal --accept-eula
unity license status        # 确认 active
# 建项目 / 放模型 / 写 BuildGame.cs 后：
~/Unity/Hub/Editor/2022.3.62f1/Editor/Unity -batchmode -nographics \
  -projectPath ~/workspace/creature-game-unity/<project> \
  -executeMethod BuildGame.BuildAndroid -logFile -
```

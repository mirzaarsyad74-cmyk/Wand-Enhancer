# AGENT Guidelines & Build Rules: Wand Enhancer

## 1. Local-Only Builds by Default (Strictly No GitHub Upload Without Approval)
* **Default Mode**: All code changes, compilation, and testing must strictly be performed **locally**.
* **Do NOT push to GitHub, create GitHub releases, or upload release assets** unless the user explicitly asks or gives permission to upload/release to GitHub.
* **Build Command**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\build.ps1
  ```
  *(Or `build.cmd` in cmd)*. This verifies web-panel linting, TypeScript types, Vitest suites, MSBuild compilation, ILRepack assembly merge, desktop patch state, and patch locator integrity.
* **Local Output Target**:
  Deploy the compiled standalone executable to:
  `C:\Users\admin\Downloads\WandEnhancer.exe`

---

## 2. Auto-Increment Version on Every GitHub Upload / Release
Whenever the user explicitly authorizes or instructs an upload/release to GitHub:
* **Always automatically increment the version number** (e.g., `2.2.0.0` -> `2.2.1.0` -> `2.2.2.0`) across all required files so that existing installations can detect the new release via GitHub API:

1. **`WandEnhancer/Properties/AssemblyInfo.cs`**:
   - `[assembly: AssemblyVersion("X.Y.Z.0")]`
   - `[assembly: AssemblyFileVersion("X.Y.Z.0")]`
   *(This also updates `Constants.Version` automatically)*
2. **`CHANGELOG.md`**:
   - Document new changes under `## [X.Y.Z.0] - YYYY-MM-DD`

* Always run a clean local build (`build.ps1`) after bumping the version before pushing or publishing the release.

---

## 3. Public GitHub Release & Auto-Update Architecture
* **Repository**: `https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer` (Public - **no GitHub token needed**).
* **API Endpoint**: `https://api.github.com/repos/mirzaarsyad74-cmyk/Wand-Enhancer/releases/latest`
* **Version Check**:
  - The app automatically queries GitHub on startup.
  - When `latestVer > currVer`, it automatically prompts the user via `UpdatePopup`.
  - Manual check is also available via *"Check for Updates Now"* in Settings.
* **One-Click Hot-Swap**:
  - Streams the release `.exe` asset directly from `browser_download_url` with live 0–100% progress reporting.
  - Verifies downloaded file size (>100KB).
  - Spawns a background self-updater script (`wand_self_update_*.cmd`) with a 30-attempt retry loop that hot-swaps `WandEnhancer.exe` and restarts the new binary.
* **Single-Instance Enforcement**:
  - On launch, `WandEnhancer` automatically terminates any stale or duplicate instances so only the newest instance runs.

---

## 4. Batch Launcher & Elevated Game Rules
* **Batch Launcher Matching**:
  - `WandEnhancer/Patches/batch-process-match.js` matches the running game executable even if the launcher `.bat` was executed from `Downloads`, `Desktop`, or a custom directory, while ignoring bypass utilities (`kvc.exe`, `drvloader.exe`, `launcher.exe`, etc.).
* **Administrator Privileges**:
  - Games launched via elevated/hypervisor bypass batch files run with Administrator privileges.
  - When modifying or troubleshooting, remind the user that **Wand / WeMod must also be run as Administrator** so Windows allows memory access and DLL injection into elevated game processes.

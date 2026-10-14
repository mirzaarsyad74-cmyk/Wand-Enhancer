<div align="center">

![logo](./assets/icon.svg)

# WandEnhancer

**A modern, open-source automation & interoperability suite designed to enhance the Wand desktop experience.**

[![Build executable](https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer/actions/workflows/build.yml/badge.svg)](https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer/actions/workflows/build.yml)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE.md)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows)](https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer)

</div>

---

> [!CAUTION]
> ### 🚨 ANTI-SCAM ALERT: 100% FREE & OPEN SOURCE
> **This is an open source project. If you paid anyone for this software, you were scammed!**
> - This software is **100% free** and always will be.
> - There are **NO paid tiers, NO activation keys, and NO subscriptions**.
> - Do not download this tool from YouTube links, Telegram channels, TikTok, or third-party file hosters. Download only from the official GitHub repository:  
>   👉 **[https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer](https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer)**

---

## ⚡ Key Features

| Feature | Description |
| :--- | :--- |
| **⚡ 1-Click Full Auto** | Automatically locates Wand, downloads the latest official client directly from CDN if missing, installs silently, and applies optimal patches with zero friction. |
| **🛠️ Auto Repair & Reinstall** | Self-healing mechanism that detects corrupted installations, restores backups, or re-installs a fresh clean copy in one click. |
| **💻 Mini Terminal Live Logs** | Integrated dark developer terminal featuring timestamps (`[HH:mm:ss]`), colored status badges (`[SECURITY]`, `[INFO]`, `[SUCCESS]`, `[WARN]`, `[ERROR]`), live activity indicators, auto-scroll toggle, buffer clear, copy, and export. |
| **🔄 GitHub In-App Auto-Update** | Directly checks and installs updates from GitHub releases without needing manual redownloads. |
| **🌐 Remote Web Panel** | Control cheats and trainers directly from your smartphone or tablet over local Wi-Fi by scanning a QR code. |
| **⌨️ DevTools on F12** | Inspect and debug the Electron renderer directly with developer tools. |
| **👑 Pro Enhancement** | Activates offline client-side Pro features cleanly through non-destructive memory/ASAR patches. |
| **🧩 Custom JS Injector** | Inject custom client-side JavaScript scripts directly into Wand's renderer window. |
| **🔒 SHA-256 Verified** | Releases include cryptographic `SHA256SUMS.txt` hashes for integrity verification. |

---

## 🚀 Getting Started

### Option A: Download from Releases (Recommended)
1. Go to the [**Releases Page**](https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer/releases).
2. Download `WandEnhancer.exe` (or `WandEnhancer-AutoUpdate.exe` for GitHub auto-updates).
3. Optional: Verify file integrity using the matching `SHA256SUMS.txt`.
4. Launch `WandEnhancer.exe` and click **⚡ Full Auto**!

### Option B: Download from GitHub Actions CI
1. Go to the [**Actions Tab**](https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer/actions/workflows/build.yml).
2. Click the latest successful run of **Build executable**.
3. Scroll down to the **Artifacts** section at the bottom.
4. Download and extract **`WandEnhancer-unsigned.zip`**.

---

## 🌐 Remote Web Panel (Mobile Trainer Control)

WandEnhancer includes a high-performance **Remote Web Panel** allowing you to control cheats from any smartphone on your local network:

1. Connect your PC and mobile device to the **same Wi-Fi network**.
2. Hover over the **Connect** button in the WandEnhancer header.
3. Scan the generated **QR code** with your phone's camera.
4. Your mobile browser opens the responsive remote controller!

> [!NOTE]
> If the page does not load, ensure Windows Firewall allows inbound connections on TCP port `3223` on Private networks. Do not expose port `3223` to the public internet.

---

## 🧩 Custom Script Injection

Extend Wand's UI or behavior by loading your own client-side JavaScript scripts:

* In the patch vector settings dialog, choose **Add .js scripts**, or
* Place `.js` files into a `renderer-scripts/` directory next to `WandEnhancer.exe`.

```javascript
// Example: renderer-scripts/my-addon.js
if (!globalThis.__customScriptLoaded) {
  globalThis.__customScriptLoaded = true;
  WandEnhancer.log("Custom script active!", WandEnhancer.remoteUrl);
}
```

---

## 🛠️ Building from Source

### Prerequisites
* Windows 10 / 11
* .NET Framework 4.8 Developer Pack
* Visual Studio 2022 (with .NET desktop development & MSBuild)
* [Node.js](https://nodejs.org/) (v20+) & [pnpm](https://pnpm.io/)

### Build Steps
```powershell
# Clone the repository
git clone https://github.com/mirzaarsyad74-cmyk/Wand-Enhancer.git
cd Wand-Enhancer

# Run build script
.\build.ps1 -Configuration Release

# Optional: compile with update notifications enabled
.\build.ps1 -Configuration Release -EnableUpdateNotifications
```

Output binary is bundled and ready at `WandEnhancer\bin\Release\WandEnhancer.exe`.

---

## ❓ Frequently Asked Questions (FAQ)

<details>
<summary><b>Why does Windows Defender / SmartScreen flag the executable?</b></summary>
<p>
Open-source game modification tools and unsigned executables frequently trigger generic heuristic detections (e.g. <i>Win32/Wacatac</i> or SmartScreen warnings) because the binary is not signed with an expensive enterprise certificate. You can inspect the complete source code, review the GitHub Actions CI workflow logs, or compile it yourself from source.
</p>
</details>

<details>
<summary><b>Does WandEnhancer send any personal data or credentials?</b></summary>
<p>
No. WandEnhancer contains zero telemetry, zero analytics, and no third-party tracking. All patches modify local files on your machine.
</p>
</details>

<details>
<summary><b>How do I undo all changes?</b></summary>
<p>
WandEnhancer creates a clean backup before applying any patches. Click the <b>Restore</b> button in the main window at any time to return Wand to its original state, or use <b>⚡ Auto Repair & Reinstall</b>.
</p>
</details>

---

## 📜 License & Legal Disclaimer

* **License**: Licensed under the [Apache 2.0 License](LICENSE.md).
* **Disclaimer**: This project is an independent open-source interoperability tool designed for educational, research, and local customization purposes. All trademarks, service marks, and company names are the property of their respective owners.

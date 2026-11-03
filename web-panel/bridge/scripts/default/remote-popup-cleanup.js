import { getWebpackRequire } from './installed-apps-sync/runtime.js';
import {
    resolveQrRenderer as findWandQrRenderer,
    hookWandQrRenderer,
} from './remote-popup-cleanup/qr-renderer.js';

(function installRemotePopupCleanup(WandEnhancer) {
    if (globalThis.__wandRemotePopupCleanupInstalled) {
        return;
    }

    globalThis.__wandRemotePopupCleanupInstalled = true;

    const style = document.createElement('style');
    style.id = 'wand-remote-popup-cleanup-style';
    style.textContent = `
    article.pro-onboarding-card--remote {
      display: none !important;
    }

    remote-tooltip .remote-tooltip .top-wrapper,
    remote-tooltip .remote-tooltip .remote-tooltip-section-divider,
    remote-tooltip .remote-tooltip .instructions .header,
    remote-tooltip .remote-tooltip .instructions .platforms {
      display: none !important;
    }

    remote-tooltip .remote-tooltip .instructions-wrapper {
      margin: 0 !important;
      padding: 18px !important;
      text-align: center !important;
    }

    remote-tooltip .remote-tooltip .instructions,
    remote-tooltip .remote-tooltip .instructions .content {
      display: flex !important;
      flex-direction: column !important;
      align-items: center !important;
      justify-content: center !important;
      padding: 0 !important;
      gap: 12px !important;
    }

    remote-tooltip .remote-tooltip .instructions remote-qr-code {
      --wand-qr-size: clamp(180px, 70vw, 240px);
      width: var(--wand-qr-size) !important;
      height: var(--wand-qr-size) !important;
      min-width: var(--wand-qr-size) !important;
      min-height: var(--wand-qr-size) !important;
      max-width: var(--wand-qr-size) !important;
      max-height: var(--wand-qr-size) !important;
      flex: 0 0 var(--wand-qr-size) !important;
      aspect-ratio: 1 / 1 !important;
      display: block !important;
      border-radius: 12px !important;
      overflow: hidden !important;
      transform: none !important;
      box-shadow: 0 18px 48px rgba(0, 0, 0, 0.35) !important;
      cursor: pointer !important;
    }

    remote-tooltip .remote-tooltip .instructions .content .text {
      display: block !important;
      max-width: 250px !important;
      overflow-wrap: anywhere !important;
    }

    remote-tooltip .remote-tooltip .instructions remote-qr-code canvas {
      width: 100% !important;
      height: 100% !important;
      aspect-ratio: 1 / 1 !important;
      display: block !important;
      object-fit: contain !important;
      image-rendering: pixelated !important;
      border-radius: 12px !important;
      transform: none !important;
    }

    .wand-reset-tunnel-btn {
      display: inline-flex !important;
      align-items: center !important;
      justify-content: center !important;
      gap: 6px !important;
      margin-top: 4px !important;
      padding: 6px 14px !important;
      background: rgba(255, 255, 255, 0.08) !important;
      border: 1px solid rgba(255, 255, 255, 0.16) !important;
      border-radius: 9999px !important;
      color: #e2e8f0 !important;
      font-family: inherit !important;
      font-size: 11px !important;
      font-weight: 600 !important;
      letter-spacing: 0.02em !important;
      cursor: pointer !important;
      transition: all 0.15s ease !important;
      outline: none !important;
      user-select: none !important;
    }

    .wand-reset-tunnel-btn:hover {
      background: rgba(255, 255, 255, 0.15) !important;
      border-color: rgba(255, 255, 255, 0.3) !important;
      color: #ffffff !important;
      transform: translateY(-1px) !important;
    }

    .wand-reset-tunnel-btn:active {
      transform: translateY(0) scale(0.98) !important;
    }

    .wand-reset-tunnel-btn.loading {
      opacity: 0.7 !important;
      cursor: wait !important;
      pointer-events: none !important;
    }

    .wand-reset-tunnel-btn.loading .wand-reset-icon {
      animation: wand-spin 1s linear infinite !important;
    }

    @keyframes wand-spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }
  `;
    let qrRenderer = null;
    let refreshScheduled = false;
    let queryScheduled = false;

    const getIpcRenderer = () => {
        try {
            const req =
                globalThis.require || (typeof window !== 'undefined' ? window.require : null);
            return req?.('electron')?.ipcRenderer || null;
        } catch (_) {
            return null;
        }
    };

    const ipc = getIpcRenderer();
    if (ipc && !globalThis.__wandRemoteIpcListenerAttached) {
        globalThis.__wandRemoteIpcListenerAttached = true;
        ipc.on('wand-remote-url-changed', (_e, newUrl) => {
            if (newUrl && newUrl !== globalThis.__wandRemoteBridgeUrl) {
                globalThis.__wandRemoteBridgeUrl = newUrl;
                refresh();
            }
        });
    }

    const requestResetTunnel = async () => {
        const ipcRenderer = getIpcRenderer();
        if (ipcRenderer) {
            try {
                const url = await ipcRenderer.invoke('wand-remote-reset-tunnel');
                if (url) return url;
            } catch (_) {}
        }

        try {
            const res = await fetch('http://127.0.0.1:3223/remote/api/tunnel/reset', {
                method: 'POST',
            });
            const data = await res.json();
            return data.url || null;
        } catch (_) {
            return null;
        }
    };

    const installStyle = () => {
        if (!document.getElementById(style.id)) {
            document.head.appendChild(style);
        }
    };

    const getRemoteUrl = () => globalThis.__wandRemoteBridgeUrl || WandEnhancer?.remoteUrl;

    const resolveQrRenderer = () => {
        const webpackRequire = getWebpackRequire();
        hookWandQrRenderer(webpackRequire, getRemoteUrl);

        if (qrRenderer) {
            return qrRenderer;
        }

        qrRenderer = findWandQrRenderer(webpackRequire);
        return qrRenderer;
    };

    const updateLinks = (remoteUrl) => {
        if (!remoteUrl) {
            return;
        }

        for (const anchor of document.querySelectorAll('remote-tooltip a[href]')) {
            anchor.setAttribute('href', remoteUrl);
            anchor.textContent = remoteUrl.replace(/\/$/, '');
        }

        for (const contentElem of document.querySelectorAll(
            'remote-tooltip .remote-tooltip .instructions .content',
        )) {
            if (!contentElem.querySelector('.wand-reset-tunnel-btn')) {
                const btn = document.createElement('button');
                btn.className = 'wand-reset-tunnel-btn';
                btn.type = 'button';
                btn.title = 'Generate a fresh Cloudflare tunnel address';
                btn.innerHTML = `
                    <svg class="wand-reset-icon" viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2"/>
                    </svg>
                    <span class="wand-reset-text">Reset Address</span>
                `;
                btn.addEventListener('click', async (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (btn.classList.contains('loading')) {
                        return;
                    }
                    btn.classList.add('loading');
                    const textSpan = btn.querySelector('.wand-reset-text');
                    if (textSpan) textSpan.textContent = 'Changing address...';

                    try {
                        const newUrl = await requestResetTunnel();
                        if (newUrl) {
                            globalThis.__wandRemoteBridgeUrl = newUrl;
                            refresh();
                            if (textSpan) textSpan.textContent = 'Address Changed!';
                        } else {
                            if (textSpan) textSpan.textContent = 'Reset Failed';
                        }
                    } catch (_) {
                        if (textSpan) textSpan.textContent = 'Reset Failed';
                    } finally {
                        setTimeout(() => {
                            btn.classList.remove('loading');
                            if (textSpan) textSpan.textContent = 'Reset Address';
                        }, 2000);
                    }
                });
                contentElem.appendChild(btn);
            }
        }

        for (const qrElem of document.querySelectorAll('remote-qr-code')) {
            if (!qrElem.dataset.wandClickBound) {
                qrElem.dataset.wandClickBound = 'true';
                qrElem.setAttribute('title', 'Open Wand Remote in browser');
                qrElem.addEventListener('click', (e) => {
                    e.preventDefault();
                    const url = getRemoteUrl();
                    if (url) {
                        try {
                            const electron = globalThis.require?.('electron');
                            electron?.shell?.openExternal?.(url) || window.open(url, '_blank');
                        } catch (_) {
                            window.open(url, '_blank');
                        }
                    }
                });
            }
        }
    };

    const updateQrCodes = async (remoteUrl) => {
        const renderQr = remoteUrl && resolveQrRenderer();
        if (!renderQr) {
            return;
        }

        for (const canvas of document.querySelectorAll('remote-qr-code canvas')) {
            if (canvas.dataset.wandRemoteUrl === remoteUrl) {
                continue;
            }

            try {
                await renderQr(canvas, remoteUrl);
                canvas.dataset.wandRemoteUrl = remoteUrl;
            } catch (error) {
                WandEnhancer?.log('Failed to render local remote QR code', error);
            }
        }
    };

    const refresh = () => {
        const remoteUrl = getRemoteUrl();
        installStyle();
        updateLinks(remoteUrl);
        void updateQrCodes(remoteUrl);

        if (!remoteUrl?.includes('trycloudflare.com') && !queryScheduled) {
            const ipcRenderer = getIpcRenderer();
            if (ipcRenderer) {
                queryScheduled = true;
                ipcRenderer
                    .invoke('wand-remote-url')
                    .then((url) => {
                        queryScheduled = false;
                        if (url && url !== globalThis.__wandRemoteBridgeUrl) {
                            globalThis.__wandRemoteBridgeUrl = url;
                            refresh();
                        }
                    })
                    .catch(() => {
                        queryScheduled = false;
                    });
            }
        }
    };

    const scheduleRefresh = () => {
        if (refreshScheduled) {
            return;
        }

        refreshScheduled = true;
        setTimeout(() => {
            refreshScheduled = false;
            refresh();
        }, 0);
    };

    refresh();

    const observer = new MutationObserver(scheduleRefresh);

    observer.observe(document.documentElement, {
        childList: true,
        subtree: true,
    });
})(globalThis.WandEnhancer);

import { isRecord } from '../installed-apps-sync/runtime.js';

const WAND_QR_RENDERER_EXPORT = 'mo';

export function resolveQrRenderer(webpackRequire) {
    for (const record of Object.values(webpackRequire?.c || {})) {
        const exports = record?.exports;
        if (isRecord(exports) && typeof exports[WAND_QR_RENDERER_EXPORT] === 'function') {
            return exports[WAND_QR_RENDERER_EXPORT];
        }
    }

    return null;
}

export function hookWandQrRenderer(webpackRequire, getRemoteUrl) {
    if (!webpackRequire?.c) {
        return null;
    }

    let hookedRenderer = null;

    for (const record of Object.values(webpackRequire.c)) {
        const exports = record?.exports;
        if (!isRecord(exports)) {
            continue;
        }

        // Hook the QR renderer module function (e.g. exports.mo)
        if (
            typeof exports[WAND_QR_RENDERER_EXPORT] === 'function' &&
            !exports[WAND_QR_RENDERER_EXPORT].__wandHooked
        ) {
            const originalRenderer = exports[WAND_QR_RENDERER_EXPORT];
            const wrappedRenderer = function (canvas, text, options) {
                const url = typeof getRemoteUrl === 'function' ? getRemoteUrl() : null;
                const finalUrl = url || text;
                return originalRenderer.call(this, canvas, finalUrl, options);
            };
            wrappedRenderer.__wandHooked = true;
            wrappedRenderer.__original = originalRenderer;
            exports[WAND_QR_RENDERER_EXPORT] = wrappedRenderer;
            hookedRenderer = wrappedRenderer;
        }

        // Hook the RemoteQrCode Aurelia component if present
        if (exports.RemoteQrCode?.prototype && !exports.RemoteQrCode.prototype.__wandHooked) {
            const origAttached = exports.RemoteQrCode.prototype.attached;
            exports.RemoteQrCode.prototype.attached = function (...args) {
                const result = origAttached ? origAttached.apply(this, args) : undefined;
                const url = typeof getRemoteUrl === 'function' ? getRemoteUrl() : null;
                if (this.canvasElement && url) {
                    const renderer = resolveQrRenderer(webpackRequire);
                    if (renderer) {
                        try {
                            renderer(this.canvasElement, url, this.options);
                            this.canvasElement.dataset.wandRemoteUrl = url;
                        } catch (_) {}
                    }
                }
                return result;
            };
            exports.RemoteQrCode.prototype.__wandHooked = true;
        }
    }

    return hookedRenderer;
}

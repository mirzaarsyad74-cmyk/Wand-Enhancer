const childProcess = require('node:child_process');
const fs = require('node:fs');
const https = require('node:https');
const os = require('node:os');
const path = require('node:path');

const { REMOTE_BASE_PATH } = require('./constants');

const CLOUDFLARED_DOWNLOAD_URL =
    'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe';

export type TunnelOptions = {
    port: number | (() => number);
    log?: (level: 'info' | 'warn' | 'error', message: string, detail?: unknown) => void;
    onUrlChanged?: (url: string) => void;
};

export type TunnelManager = {
    getUrl: () => string | null;
    getStatus: () => { active: boolean; url: string | null; starting: boolean };
    start: () => Promise<string | null>;
    reset: () => Promise<string | null>;
    stop: () => void;
};

export function createTunnelManager(options: TunnelOptions): TunnelManager {
    const log = options.log || (() => {});
    let child: any = null;
    let currentUrl: string | null = null;
    let isStarting = false;
    let startPromise: Promise<string | null> | null = null;

    function getCandidatePaths(): string[] {
        const homeDir = os.homedir();
        const localAppData = process.env.LOCALAPPDATA || path.join(homeDir, 'AppData', 'Local');

        return [
            path.join(homeDir, '.wand-remote', 'cloudflared.exe'),
            path.join(localAppData, 'WandEnhancer', 'cloudflared.exe'),
            path.join(process.cwd(), 'scripts', 'tools', 'cloudflared.exe'),
            path.join(__dirname, '..', '..', '..', 'scripts', 'tools', 'cloudflared.exe'),
            path.join((process as any).resourcesPath || '', 'tools', 'cloudflared.exe'),
            path.join((process as any).resourcesPath || '', 'cloudflared.exe'),
            'cloudflared.exe',
            'cloudflared',
        ];
    }

    async function findOrDownloadBinary(): Promise<string | null> {
        for (const candidate of getCandidatePaths()) {
            if (fs.existsSync(candidate)) {
                return candidate;
            }
        }

        // Check if on system PATH
        try {
            const check = childProcess.spawnSync('where', ['cloudflared'], {
                encoding: 'utf8',
                stdio: ['ignore', 'pipe', 'ignore'],
            });
            if (check.status === 0 && check.stdout?.trim()) {
                const firstLine = check.stdout.trim().split(/\r?\n/)[0];
                if (firstLine && fs.existsSync(firstLine)) {
                    return firstLine;
                }
            }
        } catch {
            // Ignore
        }

        // Auto-download to ~/.wand-remote/cloudflared.exe
        const targetDir = path.join(os.homedir(), '.wand-remote');
        const targetPath = path.join(targetDir, 'cloudflared.exe');

        try {
            if (!fs.existsSync(targetDir)) {
                fs.mkdirSync(targetDir, { recursive: true });
            }

            log('info', 'Downloading Cloudflare Quick Tunnel binary (zero-config public HTTPS)...');
            await downloadFile(CLOUDFLARED_DOWNLOAD_URL, targetPath);
            log('info', 'Cloudflare Quick Tunnel downloaded successfully.');
            return targetPath;
        } catch (error) {
            log('warn', 'Failed to auto-download cloudflared binary.', error);
            return null;
        }
    }

    function stop() {
        if (child) {
            try {
                child.kill('SIGTERM');
                child.kill('SIGKILL');
            } catch {
                // Ignore kill errors
            }
            child = null;
        }
        currentUrl = null;
    }

    async function launch(): Promise<string | null> {
        stop();
        const binary = await findOrDownloadBinary();
        if (!binary) {
            log('warn', 'cloudflared binary not available; public tunnel cannot be established.');
            return null;
        }

        return new Promise<string | null>((resolve) => {
            const targetPort = typeof options.port === 'function' ? options.port() : options.port;
            const args = ['tunnel', '--url', `http://127.0.0.1:${targetPort}`];
            log('info', `Starting Cloudflare Quick Tunnel to port ${targetPort}...`);

            try {
                child = childProcess.spawn(binary, args, {
                    stdio: ['ignore', 'pipe', 'pipe'],
                    windowsHide: true,
                });
            } catch (err) {
                log('warn', 'Failed to spawn cloudflared process.', err);
                resolve(null);
                return;
            }

            let resolved = false;
            const timeoutTimer = setTimeout(() => {
                if (!resolved) {
                    resolved = true;
                    log('warn', 'Timed out waiting for Cloudflare Tunnel URL.');
                    resolve(null);
                }
            }, 25000);

            const handleOutput = (data: Buffer) => {
                const text = data.toString('utf8');
                const match = text.match(/https:\/\/[a-zA-Z0-9-]+\.trycloudflare\.com/);
                if (match && !resolved) {
                    resolved = true;
                    clearTimeout(timeoutTimer);
                    const cleanUrl = `${match[0]}${REMOTE_BASE_PATH}`;
                    currentUrl = cleanUrl;
                    log('info', `Cloudflare Quick Tunnel active: ${cleanUrl}`);
                    options.onUrlChanged?.(cleanUrl);
                    resolve(cleanUrl);
                }
            };

            child.stdout?.on('data', handleOutput);
            child.stderr?.on('data', handleOutput);

            child.on('error', (err: unknown) => {
                log('warn', 'Cloudflare Tunnel process error.', err);
                if (!resolved) {
                    resolved = true;
                    clearTimeout(timeoutTimer);
                    resolve(null);
                }
            });

            child.on('exit', (code: number | null) => {
                log('info', `Cloudflare Tunnel process exited with code ${code}.`);
                if (!resolved) {
                    resolved = true;
                    clearTimeout(timeoutTimer);
                    resolve(null);
                }
            });
        });
    }

    async function start(): Promise<string | null> {
        if (currentUrl) {
            return currentUrl;
        }
        if (isStarting && startPromise) {
            return startPromise;
        }

        isStarting = true;
        startPromise = launch().finally(() => {
            isStarting = false;
            startPromise = null;
        });

        return startPromise;
    }

    async function reset(): Promise<string | null> {
        log('info', 'Resetting Cloudflare Tunnel address...');
        currentUrl = null;
        return launch();
    }

    return {
        getUrl: () => currentUrl,
        getStatus: () => ({
            active: Boolean(currentUrl),
            url: currentUrl,
            starting: isStarting,
        }),
        start,
        reset,
        stop,
    };
}

function downloadFile(url: string, dest: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(dest);
        const get = (targetUrl: string) => {
            https
                .get(targetUrl, { headers: { 'User-Agent': 'Mozilla/5.0' } }, (res: any) => {
                    if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                        get(res.headers.location);
                        return;
                    }
                    if (res.statusCode !== 200) {
                        file.close();
                        fs.unlink(dest, () => {});
                        reject(new Error(`Download failed with status ${res.statusCode}`));
                        return;
                    }
                    res.pipe(file);
                    file.on('finish', () => file.close(() => resolve()));
                })
                .on('error', (err: unknown) => {
                    file.close();
                    fs.unlink(dest, () => {});
                    reject(err);
                });
        };
        get(url);
    });
}

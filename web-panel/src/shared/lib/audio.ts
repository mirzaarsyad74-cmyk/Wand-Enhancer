let audioCtx: AudioContext | null = null;
let soundEnabled = true;

const SOUND_STORAGE_KEY = 'wand-remote.sound-effects.v1';

if (typeof window !== 'undefined') {
    try {
        const stored = localStorage.getItem(SOUND_STORAGE_KEY);
        if (stored !== null) {
            soundEnabled = stored === 'true';
        }
    } catch (_) {}
}

export function isSoundEnabled(): boolean {
    return soundEnabled;
}

export function setSoundEnabled(enabled: boolean): void {
    soundEnabled = enabled;
    try {
        localStorage.setItem(SOUND_STORAGE_KEY, String(enabled));
    } catch (_) {}
}

export function playToggleSound(active?: boolean): void {
    if (!soundEnabled || typeof window === 'undefined') {
        return;
    }

    try {
        const AudioCtor =
            window.AudioContext ||
            (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
        if (!AudioCtor) {
            return;
        }

        if (!audioCtx) {
            audioCtx = new AudioCtor();
        }

        if (audioCtx.state === 'suspended') {
            void audioCtx.resume();
        }

        const now = audioCtx.currentTime;
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();

        osc.type = 'sine';
        // Ascending tone for ON, descending for OFF
        if (active) {
            osc.frequency.setValueAtTime(440, now);
            osc.frequency.exponentialRampToValueAtTime(880, now + 0.04);
        } else {
            osc.frequency.setValueAtTime(660, now);
            osc.frequency.exponentialRampToValueAtTime(330, now + 0.04);
        }

        gain.gain.setValueAtTime(0.04, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.05);

        osc.connect(gain);
        gain.connect(audioCtx.destination);

        osc.start(now);
        osc.stop(now + 0.05);
    } catch (_) {}
}

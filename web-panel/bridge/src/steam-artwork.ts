const artworkCache = new Map<string, string>();

/**
 * Resolves high-resolution Steam poster / capsule artwork directly from Steam store API.
 * Caches successful results in memory to avoid redundant network requests.
 */
export async function resolveSteamArtwork(
    appId?: string | null,
    title?: string | null,
): Promise<string | null> {
    const safeAppId = appId?.trim() || '';
    const safeTitle = title?.trim() || '';
    const cacheKey = `${safeAppId}:${safeTitle}`;

    if (artworkCache.has(cacheKey)) {
        return artworkCache.get(cacheKey) ?? null;
    }

    if (safeAppId && /^\d+$/.test(safeAppId)) {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 4000);
            const response = await fetch(
                `https://store.steampowered.com/api/appdetails?appids=${safeAppId}`,
                {
                    signal: controller.signal,
                    headers: { 'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)' },
                },
            );
            clearTimeout(timeoutId);

            if (response.ok) {
                const data = (await response.json()) as Record<
                    string,
                    { success?: boolean; data?: { header_image?: string; capsule_image?: string } }
                >;
                const entry = data[safeAppId];
                if (entry?.success && entry.data) {
                    const imageUrl = entry.data.header_image || entry.data.capsule_image;
                    if (imageUrl) {
                        artworkCache.set(cacheKey, imageUrl);
                        return imageUrl;
                    }
                }
            }
        } catch {
            // Fall through to search fallback
        }
    }

    if (safeTitle) {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 4000);
            const response = await fetch(
                `https://store.steampowered.com/api/storesearch/?term=${encodeURIComponent(safeTitle)}&l=english&cc=US`,
                {
                    signal: controller.signal,
                    headers: { 'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)' },
                },
            );
            clearTimeout(timeoutId);

            if (response.ok) {
                const data = (await response.json()) as { items?: { tiny_image?: string }[] };
                const imageUrl = data?.items?.[0]?.tiny_image;
                if (imageUrl) {
                    artworkCache.set(cacheKey, imageUrl);
                    return imageUrl;
                }
            }
        } catch {
            // Ignore search errors
        }
    }

    return null;
}

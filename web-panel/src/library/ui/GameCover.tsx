import { useState } from 'react';

import { getGameCoverLabel, type LibraryGame } from '../model/games';

type GameCoverProps = {
    game: LibraryGame;
    size?: 'sm' | 'lg';
};

const SIZE_CLASSES: Record<NonNullable<GameCoverProps['size']>, string> = {
    lg: 'size-16 rounded-[10px] text-[8px]',
    sm: 'size-11 rounded-[9px] text-[7px]',
};

export const GameCover = ({ game, size = 'sm' }: GameCoverProps) => {
    const [failedUrls, setFailedUrls] = useState<Record<string, true>>({});
    const sizeClass = SIZE_CLASSES[size];

    const isSteam =
        game.platform.toLowerCase().includes('steam') &&
        Boolean(game.app.sku && /^\d+$/.test(game.app.sku));
    const steamProxyUrl = isSteam
        ? `/remote/api/steam-artwork?appId=${game.app.sku}&title=${encodeURIComponent(game.title)}`
        : `/remote/api/steam-artwork?title=${encodeURIComponent(game.title)}`;

    const imageUrl =
        (game.imageUrl && !failedUrls[game.imageUrl] ? game.imageUrl : null) ||
        (steamProxyUrl && !failedUrls[steamProxyUrl] ? steamProxyUrl : null);

    const handleImageError = () => {
        if (imageUrl) {
            setFailedUrls((prev) => ({ ...prev, [imageUrl]: true }));
        }
    };

    return (
        <div
            className={`relative shrink-0 overflow-hidden rounded-[9px] border border-white/10 bg-[linear-gradient(135deg,#1c172e,#0a0c12)] shadow-[inset_0_1px_0_rgba(255,255,255,.08),0_4px_12px_rgba(0,0,0,.45)] ${sizeClass}`}
        >
            {imageUrl ? (
                <img
                    alt={game.title}
                    className="absolute inset-0 size-full object-cover transition-opacity duration-300"
                    src={imageUrl}
                    loading="lazy"
                    onError={handleImageError}
                />
            ) : (
                <div className="absolute inset-0 flex flex-col justify-between p-1.5 bg-[radial-gradient(ellipse_at_top_left,rgba(0,255,213,0.12),transparent_70%)]">
                    <span className="font-mono text-[7px] font-bold uppercase tracking-wider text-(--deck-accent) opacity-80">
                        {game.platform.slice(0, 3).toUpperCase()}
                    </span>
                    <div className="truncate font-mono font-bold uppercase tracking-wider text-white/90 drop-shadow">
                        {getGameCoverLabel(game)}
                    </div>
                </div>
            )}
            {game.running ? (
                <span className="absolute right-1 top-1 size-1.5 rounded-full bg-(--deck-accent) shadow-[0_0_6px_var(--deck-accent)]" />
            ) : null}
        </div>
    );
};

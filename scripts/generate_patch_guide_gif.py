import os
import math
from PIL import Image, ImageDraw, ImageFont

WIDTH, HEIGHT = 840, 540
FONTS_DIR = os.path.join(os.environ.get('WINDIR', 'C:\\Windows'), 'Fonts')

# Fonts
font_title = ImageFont.truetype(os.path.join(FONTS_DIR, 'seguisb.ttf'), 15)
font_badge = ImageFont.truetype(os.path.join(FONTS_DIR, 'consola.ttf'), 10)
font_version = ImageFont.truetype(os.path.join(FONTS_DIR, 'consola.ttf'), 10)
font_term_title = ImageFont.truetype(os.path.join(FONTS_DIR, 'consola.ttf'), 11)
font_term = ImageFont.truetype(os.path.join(FONTS_DIR, 'consola.ttf'), 11)
font_term_bold = ImageFont.truetype(os.path.join(FONTS_DIR, 'consolab.ttf'), 11)
font_btn = ImageFont.truetype(os.path.join(FONTS_DIR, 'seguisb.ttf'), 12)
font_status = ImageFont.truetype(os.path.join(FONTS_DIR, 'segoeui.ttf'), 11)
font_guide = ImageFont.truetype(os.path.join(FONTS_DIR, 'seguisb.ttf'), 13)

# Load app icon
icon_img = Image.open('assets/appicon.png').convert('RGBA')
icon_img = icon_img.resize((24, 24), Image.Resampling.LANCZOS)

def draw_lightning(draw, x, y, color=(250, 204, 21, 255), scale=1.0):
    # Vector lightning bolt
    pts = [
        (x + 4 * scale, y),
        (x + 1 * scale, y + 6 * scale),
        (x + 4 * scale, y + 6 * scale),
        (x + 2 * scale, y + 12 * scale),
        (x + 7 * scale, y + 5 * scale),
        (x + 4 * scale, y + 5 * scale),
        (x + 6 * scale, y)
    ]
    draw.polygon(pts, fill=color)

def draw_checkmark(draw, x, y, color=(34, 197, 94, 255)):
    # Vector checkmark
    draw.line([(x, y + 5), (x + 4, y + 9), (x + 11, y)], fill=color, width=2)

def draw_gear(draw, cx, cy, color=(148, 163, 184, 255)):
    # Draw simple crisp gear
    draw.ellipse([cx - 4, cy - 4, cx + 4, cy + 4], outline=color, width=2)
    for angle in [0, 45, 90, 135]:
        rad = math.radians(angle)
        dx, dy = int(6 * math.cos(rad)), int(6 * math.sin(rad))
        draw.line([(cx - dx, cy - dy), (cx + dx, cy + dy)], fill=color, width=2)
    draw.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=(30, 36, 52, 255))

def draw_folder(draw, x, y, color=(148, 163, 184, 255)):
    # Draw simple crisp folder
    draw.polygon([(x, y + 2), (x + 4, y + 2), (x + 6, y + 4), (x + 12, y + 4), (x + 12, y + 10), (x, y + 10)], fill=color)

def draw_cursor(draw, x, y, click_anim=0.0):
    if click_anim > 0:
        r = int(click_anim * 22)
        alpha = int((1.0 - click_anim) * 200)
        draw.ellipse([x - r, y - r, x + r, y + r], outline=(168, 85, 247, alpha), width=2)
    
    pts = [
        (x, y),
        (x, y + 16),
        (x + 4, y + 12),
        (x + 8, y + 20),
        (x + 11, y + 19),
        (x + 7, y + 11),
        (x + 12, y + 11),
    ]
    shadow_pts = [(px + 1, py + 1) for px, py in pts]
    draw.polygon(shadow_pts, fill=(0, 0, 0, 160))
    draw.polygon(pts, fill=(255, 255, 255, 255), outline=(15, 23, 42, 255))

def create_base_window():
    base = Image.new('RGBA', (WIDTH, HEIGHT), (11, 13, 20, 255))
    draw = ImageDraw.Draw(base)

    wx0, wy0, wx1, wy1 = 20, 16, WIDTH - 20, HEIGHT - 16
    
    # Outer glow
    for i in range(8, 0, -1):
        alpha = int(12 * (1.0 - i / 8.0))
        draw.rounded_rectangle([wx0 - i, wy0 - i, wx1 + i, wy1 + i], radius=14, outline=(99, 102, 241, alpha), width=1)
    
    # Window surface
    draw.rounded_rectangle([wx0, wy0, wx1, wy1], radius=10, fill=(15, 18, 28, 255), outline=(40, 48, 70, 255), width=1)

    # Title Bar
    base.paste(icon_img, (wx0 + 16, wy0 + 14), icon_img)
    draw.text((wx0 + 48, wy0 + 16), "Wand Pro Patcher", font=font_title, fill=(241, 245, 249, 255))
    
    # [PRO] badge
    draw.rounded_rectangle([wx0 + 184, wy0 + 17, wx0 + 218, wy0 + 33], radius=4, fill=(30, 27, 75, 255), outline=(99, 102, 241, 255), width=1)
    draw.text((wx0 + 191, wy0 + 18), "PRO", font=font_badge, fill=(165, 180, 252, 255))

    # Version label
    draw.text((wx0 + 226, wy0 + 19), "v2.1.1.0", font=font_version, fill=(148, 163, 184, 180))

    # Window buttons (right side)
    bx = wx1 - 28
    # Close ✕
    draw.rounded_rectangle([bx, wy0 + 15, bx + 18, wy0 + 33], radius=4, fill=(30, 36, 52, 255))
    draw.line([(bx + 5, wy0 + 20), (bx + 13, wy0 + 28)], fill=(148, 163, 184, 255), width=1)
    draw.line([(bx + 13, wy0 + 20), (bx + 5, wy0 + 28)], fill=(148, 163, 184, 255), width=1)

    # Minimize —
    bx -= 24
    draw.rounded_rectangle([bx, wy0 + 15, bx + 18, wy0 + 33], radius=4, fill=(30, 36, 52, 255))
    draw.line([(bx + 4, wy0 + 25), (bx + 14, wy0 + 25)], fill=(148, 163, 184, 255), width=1)

    # Settings ⚙
    bx -= 24
    draw.rounded_rectangle([bx, wy0 + 15, bx + 18, wy0 + 33], radius=4, fill=(30, 36, 52, 255))
    draw_gear(draw, bx + 9, wy0 + 24)

    # Folder 📁
    bx -= 24
    draw.rounded_rectangle([bx, wy0 + 15, bx + 18, wy0 + 33], radius=4, fill=(30, 36, 52, 255))
    draw_folder(draw, bx + 3, wy0 + 19)

    return base

def render_frame(log_lines, button_state='idle', cursor_pos=None, click_progress=0.0, step_banner=None, auto_patch_text="1-Click Auto Patch", is_success=False):
    frame = create_base_window()
    draw = ImageDraw.Draw(frame)

    wx0, wy0, wx1, wy1 = 20, 16, WIDTH - 20, HEIGHT - 16

    # Terminal container
    tx0, ty0, tx1, ty1 = wx0 + 16, wy0 + 52, wx1 - 16, wy1 - 66
    draw.rounded_rectangle([tx0, ty0, tx1, ty1], radius=8, fill=(8, 10, 15, 255), outline=(30, 38, 56, 255), width=1)

    # Terminal header
    draw.ellipse([tx0 + 14, ty0 + 12, tx0 + 22, ty0 + 20], fill=(239, 68, 68, 255))
    draw.ellipse([tx0 + 27, ty0 + 12, tx0 + 35, ty0 + 20], fill=(245, 158, 11, 255))
    draw.ellipse([tx0 + 40, ty0 + 12, tx0 + 48, ty0 + 20], fill=(16, 185, 129, 255))

    draw.text((tx0 + 58, ty0 + 10), "wand-terminal:~$ live logs", font=font_term_title, fill=(148, 163, 184, 220))
    
    # LIVE indicator
    draw.ellipse([tx0 + 250, ty0 + 14, tx0 + 256, ty0 + 20], fill=(34, 197, 94, 255))
    draw.text((tx0 + 261, ty0 + 10), "LIVE", font=font_term_bold, fill=(34, 197, 94, 255))

    # Terminal controls
    rcx = tx1 - 16
    draw.rounded_rectangle([rcx - 70, ty0 + 7, rcx, ty0 + 25], radius=4, fill=(24, 30, 44, 255))
    draw.text((rcx - 64, ty0 + 9), "Auto-Scroll", font=font_term, fill=(148, 163, 184, 255))

    rcx -= 78
    draw.rounded_rectangle([rcx - 45, ty0 + 7, rcx, ty0 + 25], radius=4, fill=(24, 30, 44, 255))
    draw.text((rcx - 36, ty0 + 9), "Clear", font=font_term, fill=(148, 163, 184, 255))

    # Divider line
    draw.line([tx0, ty0 + 32, tx1, ty0 + 32], fill=(24, 30, 44, 255), width=1)

    # Terminal log lines
    ly = ty0 + 40
    for line in log_lines:
        time_str, tag_str, tag_color, msg_str = line
        draw.text((tx0 + 14, ly), time_str, font=font_term, fill=(100, 116, 139, 255))
        
        tag_w = draw.textlength(tag_str, font=font_term_bold) + 10
        draw.rounded_rectangle([tx0 + 82, ly - 1, tx0 + 82 + tag_w, ly + 14], radius=3, fill=(tag_color[0]//4, tag_color[1]//4, tag_color[2]//4, 255), outline=tag_color, width=1)
        draw.text((tx0 + 87, ly), tag_str, font=font_term_bold, fill=tag_color)
        
        draw.text((tx0 + 98 + tag_w, ly), msg_str, font=font_term, fill=(226, 232, 240, 255))
        ly += 22

    # Bottom action bar
    by = wy1 - 52
    
    # Status pill (left): Wand Directory Found
    draw.rounded_rectangle([wx0 + 16, by + 4, wx0 + 225, by + 36], radius=6, fill=(18, 24, 36, 255), outline=(34, 197, 94, 140), width=1)
    draw.ellipse([wx0 + 26, by + 17, wx0 + 34, by + 25], fill=(34, 197, 94, 255))
    draw.text((wx0 + 40, by + 13), "Wand Found (app-12.64.0)", font=font_status, fill=(226, 232, 240, 255))

    # Auto Repair
    bx_repair = wx1 - 425
    draw.rounded_rectangle([bx_repair, by + 3, bx_repair + 140, by + 37], radius=6, fill=(22, 27, 40, 255), outline=(45, 55, 78, 255), width=1)
    draw_lightning(draw, bx_repair + 14, by + 14, color=(148, 163, 184, 255), scale=0.9)
    draw.text((bx_repair + 26, by + 12), "Auto Repair", font=font_btn, fill=(148, 163, 184, 255))

    # Enhance (Manual Options)
    bx_enhance = wx1 - 275
    draw.rounded_rectangle([bx_enhance, by + 3, bx_enhance + 76, by + 37], radius=6, fill=(22, 27, 40, 255), outline=(45, 55, 78, 255), width=1)
    draw.text((bx_enhance + 14, by + 12), "Enhance", font=font_btn, fill=(148, 163, 184, 255))

    # Hero Button: 1-Click Auto Patch
    bx_auto = wx1 - 190
    bw_auto = 174
    
    if button_state == 'idle':
        btn_bg = (124, 58, 237, 255)
        btn_border = (168, 85, 247, 255)
    elif button_state == 'hover':
        btn_bg = (139, 92, 246, 255)
        btn_border = (192, 132, 252, 255)
    elif button_state == 'active':
        btn_bg = (99, 102, 241, 255)
        btn_border = (129, 140, 248, 255)
    elif button_state == 'success':
        btn_bg = (16, 185, 129, 255)
        btn_border = (52, 211, 153, 255)

    if button_state in ('hover', 'active'):
        for g in range(6, 0, -1):
            draw.rounded_rectangle([bx_auto - g, by + 3 - g, bx_auto + bw_auto + g, by + 37 + g], radius=8, outline=(168, 85, 247, int(22 * (1 - g/6))), width=1)
    elif button_state == 'success':
        for g in range(6, 0, -1):
            draw.rounded_rectangle([bx_auto - g, by + 3 - g, bx_auto + bw_auto + g, by + 37 + g], radius=8, outline=(16, 185, 129, int(22 * (1 - g/6))), width=1)
    
    draw.rounded_rectangle([bx_auto, by + 3, bx_auto + bw_auto, by + 37], radius=6, fill=btn_bg, outline=btn_border, width=1)
    
    # Text & Icon inside button
    tw = draw.textlength(auto_patch_text, font=font_btn)
    total_w = tw + 18
    start_tx = bx_auto + (bw_auto - total_w) // 2
    
    if is_success:
        draw_checkmark(draw, start_tx, by + 15, color=(255, 255, 255, 255))
    else:
        draw_lightning(draw, start_tx, by + 13, color=(255, 255, 255, 255), scale=1.0)
    
    draw.text((start_tx + 16, by + 12), auto_patch_text, font=font_btn, fill=(255, 255, 255, 255))

    # Step guide toast banner
    if step_banner:
        guide_text, banner_type = step_banner
        sw = draw.textlength(guide_text, font=font_guide) + 48
        sx0 = (WIDTH - sw) // 2
        sy0 = wy0 + 64
        
        banner_bg = (17, 24, 39, 245)
        border_color = (34, 197, 94, 255) if banner_type == 'success' else (124, 58, 237, 255)
        
        draw.rounded_rectangle([sx0, sy0, sx0 + sw, sy0 + 36], radius=8, fill=banner_bg, outline=border_color, width=2)
        
        if banner_type == 'success':
            draw_checkmark(draw, sx0 + 16, sy0 + 13, color=(34, 197, 94, 255))
        elif banner_type == 'action':
            draw_lightning(draw, sx0 + 16, sy0 + 11, color=(250, 204, 21, 255), scale=1.1)
        else:
            draw.ellipse([sx0 + 16, sy0 + 14, sx0 + 24, sy0 + 22], fill=(99, 102, 241, 255))

        draw.text((sx0 + 34, sy0 + 9), guide_text, font=font_guide, fill=(248, 250, 252, 255))

    # Cursor
    if cursor_pos:
        draw_cursor(draw, cursor_pos[0], cursor_pos[1], click_progress)

    return frame.convert('RGB')

def build_animation():
    print("Generating animated GIF frames...")
    frames = []
    durations = []

    initial_logs = [
        ("[12:30:00]", "SECURITY", (245, 158, 11), "This is an open source project. If you paid, you got scammed!"),
        ("[12:30:01]", "INFO", (59, 130, 246), "WeMod/Wand directory found at C:\\Users\\admin\\AppData\\Local\\Wand (12.64.0)"),
        ("[12:30:01]", "INFO", (59, 130, 246), "Ready for patching.")
    ]

    target_x, target_y = 717, 492
    start_x, start_y = 400, 270

    # Phase 1: Intro / Step 1
    for _ in range(8):
        f = render_frame(initial_logs, button_state='idle', cursor_pos=(start_x, start_y), step_banner=("Step 1: Open Wand Pro Patcher", "info"))
        frames.append(f)
        durations.append(120)

    # Phase 2: Glide cursor to 1-Click Auto Patch
    glide_steps = 12
    for i in range(glide_steps):
        t = (i + 1) / glide_steps
        ease = 0.5 - 0.5 * math.cos(t * math.pi)
        cx = int(start_x + (target_x - start_x) * ease)
        cy = int(start_y + (target_y - start_y) * ease)
        state = 'hover' if ease > 0.8 else 'idle'
        banner = ("Step 2: Hover over 1-Click Auto Patch", "action") if ease > 0.5 else ("Step 1: Open Wand Pro Patcher", "info")
        f = render_frame(initial_logs, button_state=state, cursor_pos=(cx, cy), step_banner=banner)
        frames.append(f)
        durations.append(70)

    # Phase 3: Hover pause
    for _ in range(3):
        f = render_frame(initial_logs, button_state='hover', cursor_pos=(target_x, target_y), step_banner=("Step 2: Click 1-Click Auto Patch", "action"))
        frames.append(f)
        durations.append(100)

    # Phase 4: Click down with expanding ripple
    for k in range(4):
        f = render_frame(initial_logs, button_state='active', cursor_pos=(target_x, target_y), click_progress=(k + 1) / 4.0, step_banner=("Clicking 1-Click Auto Patch...", "action"))
        frames.append(f)
        durations.append(80)

    # Cursor moves away slightly
    cursor_away_x, cursor_away_y = target_x + 25, target_y + 12

    # Patch progress logs
    patch_steps = [
        ("[12:30:02]", "FULL-AUTO", (168, 85, 247), "Applying full automated patch configuration...", "Applying patches...", "action"),
        ("[12:30:02]", "ENHANCER", (147, 51, 234), "Injecting Pro subscription into store reducer & API services...", "Unlocking WeMod Pro features...", "action"),
        ("[12:30:03]", "ENHANCER", (147, 51, 234), "Attaching DevTools hook on F12 keydown...", "Enabling DevTools on F12...", "action"),
        ("[12:30:03]", "ENHANCER", (147, 51, 234), "Injecting remote web panel & mobile QR code bridge...", "Enabling Mobile Web Remote...", "action"),
        ("[12:30:04]", "ENHANCER", (147, 51, 234), "Neutralizing auxiliary integrity verification gates...", "Bypassing integrity checks...", "action"),
        ("[12:30:04]", "SUCCESS", (34, 197, 94), "Auto-patching completed successfully! Wand is fully enhanced.", "Patch Complete!", "success"),
        ("[12:30:05]", "LAUNCH", (59, 130, 246), "Wand launched with Pro features active! Enjoy.", "Wand Launched with Pro!", "success"),
    ]

    current_logs = list(initial_logs)
    for step_time, tag, color, msg, banner_text, banner_type in patch_steps:
        current_logs.append((step_time, tag, color, msg))
        is_done = "SUCCESS" in tag or "LAUNCH" in tag
        btn_state = 'success' if is_done else 'active'
        btn_text = "Enjoy Pro!" if is_done else "Patching..."
        for _ in range(4):
            f = render_frame(current_logs, button_state=btn_state, cursor_pos=(cursor_away_x, cursor_away_y), step_banner=(banner_text, banner_type), auto_patch_text=btn_text, is_success=is_done)
            frames.append(f)
            durations.append(110)

    # Phase 6: Final victory state (hold for ~3 seconds)
    final_banner = ("Successfully Patched! Ready to Play with Pro Features", "success")
    for _ in range(12):
        f = render_frame(current_logs, button_state='success', cursor_pos=(cursor_away_x, cursor_away_y), step_banner=final_banner, auto_patch_text="Enjoy Pro!", is_success=True)
        frames.append(f)
        durations.append(220)

    out_path = 'assets/how-to-apply-patch.gif'
    print(f"Saving {len(frames)} frames to {out_path}...")
    frames[0].save(
        out_path,
        save_all=True,
        append_images=frames[1:],
        duration=durations,
        loop=0
    )
    print("Done! File size:", os.path.getsize(out_path), "bytes")

if __name__ == '__main__':
    build_animation()

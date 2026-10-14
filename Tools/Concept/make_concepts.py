"""Generate the first-pass concept sheets (SVG) into Docs/Concept/.

These are design-intent sheets (shape language, colour, proportions, layout),
not final art. Run:  python Tools/Concept/make_concepts.py
"""
import math
import random
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / "Docs" / "Concept"

INK = "#2A2438"
SNOW_LIGHT = "#F7F4EE"
SNOW_SHADOW = "#AFC2E8"
DEEP_SHADOW = "#6E7FB8"
SKY = "#CFE3F5"
GLOW = "#FFC766"
BRICK = "#B5543C"
PINE = "#2F6B57"
EMBER = "#F28C28"
PLUM = "#7A4FD6"
COCOA = "#6B3F2A"
CHEEK = "#F07A8A"

FONT = "font-family=\"'Baloo 2','Fredoka','Trebuchet MS',Arial,sans-serif\""


def ol(w=3):
    return f'stroke="{INK}" stroke-width="{w}" stroke-linejoin="round" stroke-linecap="round"'


def limb(x1, y1, x2, y2, colour, w=24):
    return (f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{INK}" stroke-width="{w + 6}" stroke-linecap="round"/>'
            f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{colour}" stroke-width="{w}" stroke-linecap="round"/>')


def snowball(x, y, r=12):
    return (f'<circle cx="{x}" cy="{y}" r="{r}" fill="{SNOW_LIGHT}" {ol(2.5)}/>'
            f'<path d="M{x - r * 0.6},{y + r * 0.2} Q{x},{y + r * 0.9} {x + r * 0.6},{y + r * 0.2}" fill="none" stroke="{SNOW_SHADOW}" stroke-width="3"/>')


def kid(x, y, s=1.0, *, skin, coat, legs, boots, team=EMBER, hat="bobble", hat_col=None,
        hair=INK, hair_style="fringe", pose="stand", mitten=None, mitten_r=14, body_w=1.0,
        face="smile", extras="", extras_back="", glasses=False, freckles=False, pom_r=14,
        scarf_long=False, onesie=False, stripe=None, flip=False):
    """A ~3.5-heads-tall kid, feet centred at (x, y). Returns an SVG <g>."""
    hat_col = hat_col or team
    mitten = mitten or team
    bw = body_w
    p = []
    p.append(f'<ellipse cx="0" cy="0" rx="{58 * bw}" ry="10" fill="{DEEP_SHADOW}" opacity="0.35"/>')
    p.append(extras_back)
    # boots
    for sx in (-1, 1):
        bx = -34 * bw if sx < 0 else 4 * bw
        p.append(f'<rect x="{bx}" y="-34" width="{30 * bw}" height="34" rx="9" fill="{boots}" {ol()}/>')
    # legs
    for sx in (-1, 1):
        lx = -30 * bw if sx < 0 else 6 * bw
        p.append(f'<rect x="{lx}" y="-80" width="{24 * bw}" height="50" rx="6" fill="{legs}" {ol()}/>')
    # arms behind body for the 'stand' side
    sh = 42 * bw
    arm_l = limb(-sh, -158, -sh - 18, -104, coat)
    hand_l = (-sh - 20, -98)
    if pose == "throw":
        arm_r = limb(sh, -160, sh + 30, -220, coat)
        hand_r = (sh + 34, -228)
    elif pose == "wave":
        arm_r = limb(sh, -160, sh + 26, -214, coat)
        hand_r = (sh + 30, -222)
    elif pose == "hold":
        arm_r = limb(sh, -158, sh + 4, -118, coat)
        hand_r = (sh - 4, -112)
    elif pose == "hips":
        arm_r = limb(sh, -160, sh + 22, -126, coat) + limb(sh + 22, -126, sh - 2, -100, coat)
        hand_r = (sh - 4, -98)
        arm_l = limb(-sh, -160, -sh - 22, -126, coat) + limb(-sh - 22, -126, -sh + 2, -100, coat)
        hand_l = (-sh + 4, -98)
    elif pose == "out":
        arm_r = limb(sh, -158, sh + 34, -132, coat)
        hand_r = (sh + 40, -128)
        arm_l = limb(-sh, -158, -sh - 34, -132, coat)
        hand_l = (-sh - 40, -128)
    else:
        arm_r = limb(sh, -158, sh + 18, -104, coat)
        hand_r = (sh + 20, -98)
    # body (puffy coat)
    w1, w2 = 40 * bw, 58 * bw
    hem = -64 if not onesie else -70
    p.append(f'<path d="M{-w1},-174 Q{-w2},-150 {-w2 + 2},{hem} Q0,{hem + 12} {w2 - 2},{hem} Q{w2},-150 {w1},-174 Q0,-186 {-w1},-174 Z" fill="{coat}" {ol()}/>')
    # quilting lines
    p.append(f'<path d="M{-w2 + 6},-118 Q0,-108 {w2 - 6},-118" fill="none" stroke="{INK}" stroke-width="1.5" opacity="0.35"/>')
    p.append(f'<path d="M0,-176 L0,{hem + 8}" fill="none" stroke="{INK}" stroke-width="1.5" opacity="0.35"/>')
    if stripe:
        p.append(stripe)
    p.append(arm_l)
    p.append(arm_r)
    p.append(f'<circle cx="{hand_l[0]}" cy="{hand_l[1]}" r="{mitten_r}" fill="{mitten}" {ol()}/>')
    p.append(f'<circle cx="{hand_r[0]}" cy="{hand_r[1]}" r="{mitten_r}" fill="{mitten}" {ol()}/>')
    if pose == "throw":
        p.append(snowball(hand_r[0] + 6, hand_r[1] - mitten_r - 8, 13))
    # scarf
    p.append(f'<rect x="{-38 * bw}" y="-184" width="{76 * bw}" height="20" rx="10" fill="{team}" {ol()}/>')
    tail_h = 78 if scarf_long else 46
    p.append(f'<rect x="{12 * bw}" y="-172" width="18" height="{tail_h}" rx="6" fill="{team}" {ol()} transform="rotate(-8 {12 * bw} -172)"/>')
    p.append(f'<path d="M{14 * bw},{-172 + tail_h - 6} l18,0" stroke="{INK}" stroke-width="1.5" opacity="0.5" transform="rotate(-8 {12 * bw} -172)"/>')
    # head
    hy = -228
    if hair_style == "bunches":
        p.append(f'<ellipse cx="-50" cy="{hy + 8}" rx="13" ry="18" fill="{hair}" {ol()}/>')
        p.append(f'<ellipse cx="50" cy="{hy + 8}" rx="13" ry="18" fill="{hair}" {ol()}/>')
    if hair_style == "braids":
        p.append(f'<rect x="-50" y="{hy}" width="14" height="62" rx="7" fill="{hair}" {ol()}/>')
        p.append(f'<rect x="36" y="{hy}" width="14" height="62" rx="7" fill="{hair}" {ol()}/>')
    if hair_style == "puffs":
        p.append(f'<circle cx="-44" cy="{hy - 4}" r="17" fill="{hair}" {ol()}/>')
        p.append(f'<circle cx="44" cy="{hy - 4}" r="17" fill="{hair}" {ol()}/>')
    p.append(f'<circle cx="0" cy="{hy}" r="46" fill="{skin}" {ol()}/>')
    if hair_style in ("fringe", "bunches", "braids"):
        p.append(f'<path d="M-40,{hy - 18} Q-30,{hy - 2} -16,{hy - 14} Q-4,{hy - 2} 8,{hy - 14} Q22,{hy - 2} 40,{hy - 18} L36,{hy - 30} L-36,{hy - 30} Z" fill="{hair}"/>')
    if hair_style == "short":
        p.append(f'<path d="M-44,{hy - 6} Q-46,{hy - 50} 0,{hy - 48} Q46,{hy - 50} 44,{hy - 6} Q30,{hy - 26} 10,{hy - 22} Q-10,{hy - 30} -26,{hy - 20} Q-38,{hy - 18} -44,{hy - 6} Z" fill="{hair}" {ol()}/>')
    # face
    ex = 16
    if face == "wink":
        p.append(f'<ellipse cx="{-ex}" cy="{hy + 2}" rx="5" ry="7" fill="{INK}"/><circle cx="{-ex + 2}" cy="{hy - 1}" r="1.8" fill="white"/>')
        p.append(f'<path d="M{ex - 7},{hy + 2} Q{ex},{hy - 5} {ex + 7},{hy + 2}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>')
    else:
        for e in (-ex, ex):
            p.append(f'<ellipse cx="{e}" cy="{hy + 2}" rx="5" ry="7" fill="{INK}"/><circle cx="{e + 2}" cy="{hy - 1}" r="1.8" fill="white"/>')
    if face == "determined":
        p.append(f'<path d="M-24,{hy - 12} L-9,{hy - 7} M24,{hy - 12} L9,{hy - 7}" stroke="{INK}" stroke-width="3.5" stroke-linecap="round"/>')
    if glasses:
        p.append(f'<circle cx="{-ex}" cy="{hy + 2}" r="12" fill="none" stroke="{INK}" stroke-width="3"/><circle cx="{ex}" cy="{hy + 2}" r="12" fill="none" stroke="{INK}" stroke-width="3"/><line x1="-4" y1="{hy}" x2="4" y2="{hy}" stroke="{INK}" stroke-width="3"/>')
    p.append(f'<circle cx="-27" cy="{hy + 16}" r="8" fill="{CHEEK}" opacity="0.55"/><circle cx="27" cy="{hy + 16}" r="8" fill="{CHEEK}" opacity="0.55"/>')
    if freckles:
        for fx, fy in ((-30, 10), (-24, 13), (-33, 15), (30, 10), (24, 13), (33, 15)):
            p.append(f'<circle cx="{fx}" cy="{hy + fy}" r="1.6" fill="{COCOA}"/>')
    if face in ("grin", "wink"):
        p.append(f'<path d="M-14,{hy + 18} Q0,{hy + 36} 14,{hy + 18} Z" fill="{INK}"/><path d="M-8,{hy + 26} Q0,{hy + 32} 8,{hy + 26}" fill="#E5677A"/>')
    elif face == "determined":
        p.append(f'<path d="M-10,{hy + 22} Q0,{hy + 26} 12,{hy + 19}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>')
    else:
        p.append(f'<path d="M-11,{hy + 19} Q0,{hy + 29} 11,{hy + 19}" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>')
    # hat
    if hat in ("bobble", "striped"):
        p.append(f'<path d="M-45,{hy - 14} Q-46,{hy - 66} 0,{hy - 66} Q46,{hy - 66} 45,{hy - 14} Z" fill="{hat_col}" {ol()}/>')
        if hat == "striped":
            for k in range(3):
                yy = hy - 26 - k * 13
                half = math.sqrt(max(0, 45 ** 2 - (yy - (hy - 14)) ** 2 * 0.55))
                p.append(f'<path d="M{-half + 2},{yy} L{half - 2},{yy}" stroke="white" stroke-width="6"/>')
        for kx in range(-36, 40, 9):
            p.append(f'<line x1="{kx}" y1="{hy - 30}" x2="{kx}" y2="{hy - 50}" stroke="{INK}" stroke-width="1.2" opacity="0.25"/>')
        p.append(f'<rect x="-49" y="{hy - 26}" width="98" height="18" rx="9" fill="{hat_col}" {ol()}/>')
        for kx in range(-40, 44, 8):
            p.append(f'<line x1="{kx}" y1="{hy - 24}" x2="{kx}" y2="{hy - 10}" stroke="{INK}" stroke-width="1.2" opacity="0.3"/>')
        p.append(f'<circle cx="0" cy="{hy - 66 - pom_r * 0.6}" r="{pom_r}" fill="{team if hat_col != team else "white"}" {ol()}/>')
    p.append(extras)
    tf = f'translate({x},{y}) scale({-s if flip else s},{s})'
    return f'<g transform="{tf}">' + "".join(p) + '</g>'


def biscuit(x, y, s=1.0, flip=False):
    tf = f'translate({x},{y}) scale({-s if flip else s},{s})'
    return f'''<g transform="{tf}">
<ellipse cx="0" cy="0" rx="46" ry="8" fill="{DEEP_SHADOW}" opacity="0.35"/>
<path d="M34,-52 Q58,-78 50,-90" fill="none" stroke="{INK}" stroke-width="11" stroke-linecap="round"/>
<path d="M34,-52 Q58,-78 50,-90" fill="none" stroke="#C9A46A" stroke-width="6" stroke-linecap="round"/>
<rect x="-30" y="-32" width="12" height="32" rx="5" fill="#C9A46A" {ol()}/><rect x="18" y="-32" width="12" height="32" rx="5" fill="#C9A46A" {ol()}/>
<ellipse cx="0" cy="-46" rx="42" ry="24" fill="#C9A46A" {ol()}/>
<path d="M-26,-66 Q0,-74 28,-64 L30,-36 Q0,-28 -28,-38 Z" fill="#B2342C" {ol(2.5)}/>
<path d="M-18,-70 L-20,-34 M0,-72 L0,-30 M18,-68 L19,-34" stroke="#2E5E3E" stroke-width="3"/>
<path d="M-28,-54 L30,-50" stroke="#F2C14E" stroke-width="2"/>
<circle cx="-44" cy="-74" r="24" fill="#C9A46A" {ol()}/>
<path d="M-60,-92 Q-70,-80 -62,-66 Q-56,-78 -52,-90 Z" fill="#8A6A3E" {ol(2.5)}/>
<path d="M-30,-94 Q-18,-84 -26,-70 Q-32,-82 -36,-92 Z" fill="#8A6A3E" {ol(2.5)}/>
<ellipse cx="-62" cy="-68" rx="10" ry="8" fill="#E4CFA8" {ol(2)}/>
<circle cx="-70" cy="-71" r="4" fill="{INK}"/>
<circle cx="-46" cy="-80" r="4" fill="{INK}"/><circle cx="-45" cy="-81" r="1.4" fill="white"/>
<path d="M-66,-60 Q-62,-52 -56,-58" fill="#E5677A" stroke="{INK}" stroke-width="1.5"/>
</g>'''


def flakes(w, h, n, seed, rmin=1.5, rmax=4.0, opacity=0.9):
    rnd = random.Random(seed)
    return "".join(f'<circle cx="{rnd.uniform(0, w):.1f}" cy="{rnd.uniform(0, h):.1f}" r="{rnd.uniform(rmin, rmax):.1f}" fill="white" opacity="{rnd.uniform(0.5, opacity):.2f}"/>' for _ in range(n))


def sparkle(w, y0, y1, n, seed):
    rnd = random.Random(seed)
    out = []
    for _ in range(n):
        x, y, r = rnd.uniform(0, w), rnd.uniform(y0, y1), rnd.uniform(2, 5)
        out.append(f'<path d="M{x:.1f},{y - r:.1f} L{x + r * 0.25:.1f},{y:.1f} L{x:.1f},{y + r:.1f} L{x - r * 0.25:.1f},{y:.1f} Z M{x - r:.1f},{y:.1f} L{x:.1f},{y + r * 0.25:.1f} L{x + r:.1f},{y:.1f} L{x:.1f},{y - r * 0.25:.1f} Z" fill="white" opacity="0.85"/>')
    return "".join(out)


def svg(w, h, body, title):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">'
            f'<title>{title}</title>{body}</svg>\n')


# ---------------------------------------------------------------- the roster
def roster(team=EMBER):
    t = team
    clip = (f'<rect x="-92" y="-112" width="38" height="50" rx="4" fill="#A8774A" {ol()} transform="rotate(-10 -74 -88)"/>'
            f'<rect x="-86" y="-104" width="28" height="36" fill="white" stroke="{INK}" stroke-width="1.5" transform="rotate(-10 -74 -88)"/>'
            f'<path d="M-82,-96 h18 M-82,-88 h20 M-82,-80 h14" stroke="{DEEP_SHADOW}" stroke-width="2" transform="rotate(-10 -74 -88)"/>')
    vest = (f'<path d="M-36,-172 L-50,-72 L-18,-68 L-14,-170 Z M36,-172 L50,-72 L18,-68 L14,-170 Z" fill="#DDEB3A" {ol(2.5)}/>'
            f'<path d="M-48,-106 L-17,-104 M48,-106 L17,-104 M-46,-90 L-17,-88 M46,-90 L17,-88" stroke="#D9DEE6" stroke-width="5"/>')
    string = f'<path d="M-70,-128 Q0,-90 70,-128" fill="none" stroke="{t}" stroke-width="2.5"/>'
    toggles = "".join(f'<rect x="-6" y="{yy}" width="12" height="5" rx="2" fill="#E9D7B0" {ol(1.5)}/>' for yy in (-150, -126, -102))
    thermos = (f'<rect x="38" y="-150" width="22" height="48" rx="6" fill="#B8C4D6" {ol()}/>'
               f'<rect x="36" y="-158" width="26" height="12" rx="4" fill="{COCOA}" {ol(2)}/>'
               f'<path d="M44,-166 q-6,-10 0,-20 q6,-10 0,-20 M54,-166 q-6,-10 0,-20" fill="none" stroke="white" stroke-width="3" opacity="0.9" stroke-linecap="round"/>')
    hoops = "".join(f'<path d="M-66,{yy} Q0,{yy + 10} 66,{yy}" fill="none" stroke="#F4F1E8" stroke-width="12"/>' for yy in (-150, -122, -94))
    boulder = (f'<circle cx="104" cy="-46" r="46" fill="{SNOW_LIGHT}" {ol()}/>'
               f'<path d="M70,-30 Q104,-2 140,-34" fill="none" stroke="{SNOW_SHADOW}" stroke-width="7"/>'
               f'<path d="M84,-70 q8,-6 16,0" fill="none" stroke="white" stroke-width="5" stroke-linecap="round"/>')
    goggles = (f'<rect x="-40" y="-276" width="80" height="12" rx="6" fill="{INK}"/>'
               f'<circle cx="-16" cy="-270" r="13" fill="#2BA6A0" {ol()}/><circle cx="16" cy="-270" r="13" fill="#2BA6A0" {ol()}/>'
               f'<circle cx="-19" cy="-273" r="4" fill="white" opacity="0.8"/><circle cx="13" cy="-273" r="4" fill="white" opacity="0.8"/>')
    stick = (f'<line x1="78" y1="-110" x2="104" y2="-262" stroke="{INK}" stroke-width="11" stroke-linecap="round"/>'
             f'<line x1="78" y1="-110" x2="104" y2="-262" stroke="#D9A45B" stroke-width="6" stroke-linecap="round"/>'
             f'<path d="M86,-286 L126,-282 L120,-252 L94,-254 Z" fill="#3B82C4" {ol()}/>'
             f'<rect x="92" y="-252" width="22" height="10" fill="#9AA3AD" {ol(2)}/>'
             + snowball(106, -292, 11))
    carrot = (f'<path d="M-66,-104 L-110,-120 L-64,-90 Z" fill="#F07E26" {ol(2.5)}/>'
              f'<path d="M-66,-104 q6,-12 2,-20 M-64,-98 q12,-6 16,-14" fill="none" stroke="#3C8D4A" stroke-width="3.5" stroke-linecap="round"/>')
    pockets = f'<rect x="-44" y="-106" width="26" height="18" rx="3" fill="#6B7631" {ol(2)}/><rect x="18" y="-106" width="26" height="18" rx="3" fill="#6B7631" {ol(2)}/>'
    return [
        dict(name="Maisie “Fastball” Okafor", role="Thrower", age=10, k=dict(skin="#7A4A2E", coat="#2BA6A0", legs="#33405E", boots="#C8382E", hair="#1E1A22", hair_style="puffs", pose="throw", mitten="#B5D33D", mitten_r=18, face="determined")),
        dict(name="Bernard “Bunker” Lindqvist", role="Builder", age=11, k=dict(skin="#F2D3B8", coat="#6D7A8C", legs="#4A5568", boots="#5A3A26", hair="#C8642E", glasses=True, pose="stand", stripe=vest, extras=clip, freckles=True)),
        dict(name="Pip Tanaka", role="Scout", age=7, s=0.74, k=dict(skin="#EBC29A", coat="#FFD23F", legs="#FFD23F", boots="#3B6FB6", hair="#1E1A22", pose="out", face="grin", onesie=True, extras=string)),
        dict(name="Rosa Delgado", role="Support", age=9, s=0.9, k=dict(skin="#C98E64", coat="#C8382E", legs="#3A2E3F", boots="#6B3F2A", hair="#3A2318", hair_style="braids", pose="hold", stripe=toggles, extras=thermos, scarf_long=True)),
        dict(name="Tobias “Tank” Brennan", role="Heavy", age=12, s=1.1, k=dict(skin="#E9C3A0", coat="#2E7D4F", legs="#3A3F4A", boots="#2A2A2A", hair="#6B4A2E", hair_style="short", hat=None, pose="hips", body_w=1.25, stripe=hoops, extras_back=boulder, face="smile")),
        dict(name="Ada Voss", role="Thrower · Inventor", age=11, k=dict(skin="#F0CBA8", coat="#E0A93A", legs="#4A3B5C", boots="#7A2E2E", hair="#F2D27A", hair_style="braids", pose="hold", extras=goggles + stick, face="determined")),
        dict(name="Felix Moreau", role="Trickster", age=10, k=dict(skin="#D9A77C", coat="#3B6FB6", legs="#2F3A4E", boots="#B5543C", hair="#2A1C14", hat="striped", pom_r=26, pose="wave", face="wink", extras=carrot)),
        dict(name="Wren Whitaker & Biscuit", role="Support · Dog", age=8, s=0.84, dog=True, k=dict(skin="#F5D7C3", coat="#7C8A3A", legs="#4A3A2E", boots="#3C8D4A", hair="#8A5A34", hair_style="bunches", pose="stand", freckles=True, face="grin", stripe=pockets)),
    ]


def role_icon(role, x, y, col):
    r = role.split(" ")[0]
    g = f'<circle cx="{x}" cy="{y}" r="15" fill="{col}" {ol(2.5)}/>'
    if r == "Thrower":
        g += f'<circle cx="{x}" cy="{y}" r="7" fill="white" {ol(2)}/>'
    elif r == "Builder":
        g += f'<path d="M{x - 7},{y - 6} L{x + 7},{y - 6} L{x + 5},{y + 7} L{x - 5},{y + 7} Z" fill="white" {ol(2)}/>'
    elif r == "Scout":
        g += f'<circle cx="{x - 4}" cy="{y + 1}" r="4.5" fill="white" {ol(2)}/><circle cx="{x + 4}" cy="{y + 1}" r="4.5" fill="white" {ol(2)}/>'
    elif r == "Heavy":
        g += f'<circle cx="{x}" cy="{y}" r="9" fill="white" {ol(2)}/>'
    elif r == "Support":
        g += f'<rect x="{x - 4}" y="{y - 8}" width="8" height="16" rx="2" fill="white" {ol(2)}/>'
    elif r == "Trickster":
        g += f'<rect x="{x - 6}" y="{y - 8}" width="12" height="10" fill="{INK}"/><rect x="{x - 9}" y="{y + 1}" width="18" height="3" fill="{INK}"/>'
    return g


def make_lineup():
    W, H = 1900, 860
    b = [f'<defs><linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#BFD8F2"/><stop offset="1" stop-color="#EAF2FB"/></linearGradient></defs>',
         f'<rect width="{W}" height="{H}" fill="url(#sky)"/>',
         f'<path d="M0,560 Q480,520 950,552 Q1420,584 {W},540 L{W},{H} L0,{H} Z" fill="{SNOW_LIGHT}"/>',
         f'<path d="M0,640 Q600,610 1200,640 Q1600,660 {W},630 L{W},{H} L0,{H} Z" fill="#E9EEF8"/>',
         flakes(W, 540, 140, 3), sparkle(W, 570, 700, 40, 4),
         f'<text x="60" y="78" {FONT} font-size="44" font-weight="800" fill="{INK}">The Snowball Game — Kids of Hollybrook</text>',
         f'<text x="60" y="116" {FONT} font-size="22" fill="{INK}" opacity="0.75">Character line-up v0.1 · shape &amp; colour intent · ~3.5 heads tall · team colour lives in the scarf and hat (Maple Close Crew = Ember Orange)</text>']
    slot = (W - 120) / 8
    for i, c in enumerate(roster()):
        cx = 60 + slot * i + slot / 2
        s = c.get("s", 1.0)
        if c.get("dog"):
            b.append(kid(cx - 30, 640, s, **c["k"]))
            b.append(biscuit(cx + 62, 646, 0.82))
        else:
            b.append(kid(cx, 640, s, **c["k"]))
        b.append(role_icon(c["role"], cx, 706, EMBER))
        words = c["name"].split(" ")
        if "“" in c["name"]:
            n1, n2 = " ".join(words[:2]), words[-1]
        elif "&" in c["name"]:
            n1, n2 = " ".join(words[:2]), "&amp; " + words[-1]
        else:
            n1, n2 = words[0], " ".join(words[1:])
        b.append(f'<text x="{cx}" y="750" text-anchor="middle" {FONT} font-size="22" font-weight="800" fill="{INK}">{n1}</text>')
        b.append(f'<text x="{cx}" y="776" text-anchor="middle" {FONT} font-size="17" fill="{INK}" opacity="0.8">{n2}</text>')
        b.append(f'<text x="{cx}" y="806" text-anchor="middle" {FONT} font-size="17" fill="{COCOA}">{c["role"]} · age {c["age"]}</text>')
    b.append(f'<text x="{W - 60}" y="{H - 18}" text-anchor="end" {FONT} font-size="15" fill="{INK}" opacity="0.6">Concept placeholder — generated by Tools/Concept/make_concepts.py</text>')
    (OUT / "CharacterLineup_v01.svg").write_text(svg(W, H, "".join(b), "Character line-up v0.1"), encoding="utf-8")


# ----------------------------------------------------------------- key art
def house(x, base, w, h, wall, door, roof="#5B4A5E"):
    r = []
    r.append(f'<rect x="{x}" y="{base - h}" width="{w}" height="{h}" fill="{wall}" {ol()}/>')
    r.append(f'<path d="M{x - 14},{base - h} L{x + w / 2},{base - h - w * 0.42} L{x + w + 14},{base - h} Z" fill="{roof}" {ol()}/>')
    r.append(f'<path d="M{x - 18},{base - h + 4} Q{x - 18},{base - h - 14} {x + w / 2},{base - h - w * 0.42 - 12} Q{x + w + 18},{base - h - 14} {x + w + 18},{base - h + 4} Q{x + w * 0.8},{base - h - 4} {x + w * 0.6},{base - h + 8} Q{x + w * 0.35},{base - h - 4} {x + w * 0.2},{base - h + 10} Q{x},{base - h - 2} {x - 18},{base - h + 4} Z" fill="{SNOW_LIGHT}" {ol(2.5)}/>')
    r.append(f'<rect x="{x + w * 0.62}" y="{base - h - w * 0.42 + 10}" width="18" height="34" fill="{BRICK}" {ol(2.5)}/><rect x="{x + w * 0.62 - 3}" y="{base - h - w * 0.42 + 4}" width="24" height="10" rx="5" fill="{SNOW_LIGHT}" {ol(2)}/>')
    for wx in (x + w * 0.14, x + w * 0.62):
        r.append(f'<rect x="{wx}" y="{base - h + 22}" width="{w * 0.24}" height="{h * 0.26}" rx="4" fill="{GLOW}" {ol(2.5)}/><path d="M{wx + w * 0.12},{base - h + 22} v{h * 0.26} M{wx},{base - h + 22 + h * 0.13} h{w * 0.24}" stroke="{INK}" stroke-width="2"/>')
        r.append(f'<rect x="{wx - 3}" y="{base - h + 18}" width="{w * 0.24 + 6}" height="7" rx="3" fill="{SNOW_LIGHT}" {ol(1.5)}/>')
    r.append(f'<rect x="{x + w * 0.4}" y="{base - h * 0.42}" width="{w * 0.2}" height="{h * 0.42}" rx="4" fill="{door}" {ol(2.5)}/>')
    return "".join(r)


def make_keyart():
    W, H = 1920, 1080
    b = ['<defs><linearGradient id="ks" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8FB4E3"/><stop offset="0.55" stop-color="#F6D7B8"/><stop offset="1" stop-color="#FFE6C2"/></linearGradient>'
         '<radialGradient id="sun" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#FFF3D6"/><stop offset="1" stop-color="#FFF3D6" stop-opacity="0"/></radialGradient></defs>',
         f'<rect width="{W}" height="{H}" fill="url(#ks)"/>',
         f'<circle cx="1480" cy="430" r="260" fill="url(#sun)"/>',
         f'<path d="M0,560 Q300,470 620,540 Q900,600 1220,500 Q1560,420 {W},520 L{W},700 L0,700 Z" fill="#DCE6F5"/>']
    # distant pines
    rnd = random.Random(7)
    for i in range(26):
        tx = i * 78 + rnd.uniform(-20, 20)
        th = rnd.uniform(70, 120)
        ty = 600 - rnd.uniform(0, 40)
        b.append(f'<path d="M{tx},{ty - th} L{tx + th * 0.32},{ty} L{tx - th * 0.32},{ty} Z" fill="#7F9FC4"/>')
    base = 690
    for hx, hw, hh, wall, door in ((40, 250, 190, BRICK, "#2F6B57"), (330, 230, 170, "#E8D8BE", "#C8382E"), (600, 260, 200, "#9C4A3A", "#3B6FB6"),
                                   (1240, 250, 185, "#E8D8BE", "#7A4FD6"), (1530, 240, 200, BRICK, "#F2C14E"), (1800, 220, 170, "#9C4A3A", "#2F6B57")):
        b.append(house(hx, base, hw, hh, wall, door))
    # street lamp
    b.append(f'<rect x="1080" y="430" width="12" height="270" fill="#3A3550" {ol(2)}/><path d="M1060,430 L1112,430 L1100,404 L1072,404 Z" fill="#3A3550" {ol(2)}/><ellipse cx="1086" cy="436" rx="18" ry="8" fill="{GLOW}"/><rect x="1066" y="398" width="40" height="8" rx="4" fill="{SNOW_LIGHT}"/>')
    # ground
    b.append(f'<path d="M0,690 Q500,660 960,700 Q1400,736 {W},680 L{W},{H} L0,{H} Z" fill="{SNOW_LIGHT}" {ol(3)}/>')
    b.append(f'<path d="M0,760 Q400,740 800,770 Q1300,800 {W},760" fill="none" stroke="{SNOW_SHADOW}" stroke-width="10" opacity="0.6"/>')
    # footprints
    for i in range(10):
        fx, fy = 620 + i * 52, 900 - i * 14 + (8 if i % 2 else -8)
        b.append(f'<ellipse cx="{fx}" cy="{fy}" rx="11" ry="6" fill="{DEEP_SHADOW}" opacity="0.4"/>')
    # away team (purple) behind a wall on the right
    b.append(kid(1440, 900, 0.95, skin="#E2B48C", coat="#4FA3D9", legs="#33405E", boots="#2A2A2A", team=PLUM, hair="#3A2318", pose="throw", face="grin", flip=True))
    b.append(kid(1680, 880, 0.8, skin="#8A5A3C", coat="#E0A93A", legs="#3A2E3F", boots="#7A2E2E", team=PLUM, hair="#1E1A22", hair_style="puffs", pose="out", face="determined", flip=True))
    b.append(f'<path d="M1300,930 Q1300,846 1360,840 L1580,836 Q1640,840 1640,930 Z" fill="{SNOW_LIGHT}" {ol()}/>'
             f'<path d="M1302,930 L1302,900 L1638,900 L1638,930 Z" fill="{SNOW_SHADOW}" opacity="0.7"/>'
             f'<path d="M1320,880 h300 M1330,906 h290 M1400,840 v40 M1500,838 v42 M1360,880 v26 M1460,880 v26 M1560,880 v26" stroke="{SNOW_SHADOW}" stroke-width="3"/>')
    # home team (orange) behind a snowbank on the left
    b.append(kid(400, 920, 1.05, skin="#7A4A2E", coat="#2BA6A0", legs="#33405E", boots="#C8382E", hair="#1E1A22", hair_style="puffs", pose="throw", mitten="#B5D33D", mitten_r=18, face="determined"))
    b.append(kid(640, 950, 0.8, skin="#EBC29A", coat="#FFD23F", legs="#FFD23F", boots="#3B6FB6", hair="#1E1A22", pose="out", face="grin", onesie=True))
    b.append(f'<path d="M180,1000 Q190,880 300,870 Q420,850 520,890 Q600,920 610,1000 Z" fill="{SNOW_LIGHT}" {ol()}/>'
             f'<path d="M184,1000 Q200,950 300,945 Q430,935 520,960 Q590,975 608,1000 Z" fill="{SNOW_SHADOW}" opacity="0.8"/>'
             f'<path d="M250,890 Q320,872 400,880" fill="none" stroke="white" stroke-width="7" stroke-linecap="round"/>')
    # telegraphed arcs
    b.append(f'<path d="M452,690 Q900,360 1330,680" fill="none" stroke="{EMBER}" stroke-width="5" stroke-dasharray="4 14" stroke-linecap="round"/>')
    b.append(f'<ellipse cx="1340" cy="830" rx="40" ry="12" fill="none" stroke="{EMBER}" stroke-width="4" stroke-dasharray="8 6"/>')
    b.append(f'<path d="M1395,672 Q980,440 640,770" fill="none" stroke="{PLUM}" stroke-width="5" stroke-dasharray="4 14" stroke-linecap="round"/>')
    b.append(snowball(900, 470, 16))
    b.append(snowball(1040, 520, 14))
    b.append(flakes(W, H, 260, 11, 1.5, 5))
    b.append(sparkle(W, 720, 1060, 60, 12))
    # title
    b.append(f'<g transform="translate(960,190)">'
             f'<text text-anchor="middle" {FONT} font-size="150" font-weight="900" fill="{INK}" stroke="{INK}" stroke-width="22" stroke-linejoin="round">THE SNOWBALL GAME</text>'
             f'<text text-anchor="middle" {FONT} font-size="150" font-weight="900" fill="white">THE SNOWBALL GAME</text>'
             f'<text y="64" text-anchor="middle" {FONT} font-size="40" font-weight="700" fill="{INK}">Snow Week in Hollybrook · pick your crew</text></g>')
    for sx in range(-620, 640, 58):
        b.append(f'<ellipse cx="{960 + sx}" cy="{62 + (sx * 7919 % 13)}" rx="34" ry="14" fill="{SNOW_LIGHT}" {ol(2)}/>')
    (OUT / "KeyArt_SnowDay_v01.svg").write_text(svg(W, H, "".join(b), "Key art v0.1"), encoding="utf-8")


# ------------------------------------------------------------- map layout
def make_map():
    W, H = 1780, 1100
    S = 30  # px per metre; 40 x 30 m playable
    ox, oy = 100, 120
    b = [f'<rect width="{W}" height="{H}" fill="#EEF2F9"/>',
         f'<text x="{ox}" y="62" {FONT} font-size="40" font-weight="800" fill="{INK}">Map concept — Maple Close (cul-de-sac)</text>',
         f'<text x="{ox}" y="96" {FONT} font-size="20" fill="{INK}" opacity="0.75">Top-down layout v0.1 · 40 × 30 m · 1 m grid · mirrored for competitive play · tutorial / home map</text>']
    b.append(f'<rect x="{ox}" y="{oy}" width="{40 * S}" height="{30 * S}" fill="{SNOW_LIGHT}" {ol()}/>')
    for i in range(41):
        b.append(f'<line x1="{ox + i * S}" y1="{oy}" x2="{ox + i * S}" y2="{oy + 30 * S}" stroke="{SNOW_SHADOW}" stroke-width="{1.4 if i % 5 == 0 else 0.6}" opacity="0.7"/>')
    for j in range(31):
        b.append(f'<line x1="{ox}" y1="{oy + j * S}" x2="{ox + 40 * S}" y2="{oy + j * S}" stroke="{SNOW_SHADOW}" stroke-width="{1.4 if j % 5 == 0 else 0.6}" opacity="0.7"/>')

    def m(x, y):
        return ox + x * S, oy + y * S

    # packed road: the close and the turning circle
    cx, cy = m(20, 15)
    b.append(f'<rect x="{m(0, 12.5)[0]}" y="{m(0, 12.5)[1]}" width="{40 * S}" height="{5 * S}" fill="#C9CFDA"/>')
    b.append(f'<circle cx="{cx}" cy="{cy}" r="{6.5 * S}" fill="#C9CFDA"/>')
    b.append(f'<circle cx="{cx}" cy="{cy}" r="{2.2 * S}" fill="{SNOW_LIGHT}" {ol(2.5)}/>')
    # gritting lorry strip
    b.append(f'<rect x="{m(0, 14.2)[0]}" y="{m(0, 14.2)[1]}" width="{40 * S}" height="{1.6 * S}" fill="#D9B98A" opacity="0.55"/>')
    # houses at both ends (spawns)
    for x0, col, team in ((0, EMBER, "HOME"), (36, PLUM, "AWAY")):
        for yy in (1, 11, 20):
            hx, hy = m(x0, yy)
            b.append(f'<rect x="{hx}" y="{hy}" width="{4 * S}" height="{9 * S if yy != 11 else 0}" fill="{BRICK}" {ol(2.5)}/>' if yy != 11 else "")
        sx, sy = m(x0 + (0.5 if x0 == 0 else 0.5), 11.5)
        b.append(f'<rect x="{sx}" y="{sy}" width="{3 * S}" height="{7 * S}" rx="10" fill="{col}" opacity="0.28" stroke="{col}" stroke-width="3"/>')
        b.append(f'<text x="{sx + 1.5 * S}" y="{sy + 3.7 * S}" text-anchor="middle" {FONT} font-size="20" font-weight="800" fill="{col}">{team}</text>')
        bx, by = m(5.6 if x0 == 0 else 34.4, 18.7)
        b.append(f'<rect x="{bx - 30}" y="{by}" width="60" height="16" rx="6" fill="{COCOA}" {ol(2)}/><text x="{bx}" y="{by + 36}" text-anchor="middle" {FONT} font-size="13" fill="{COCOA}">cocoa bench</text>')
    # mirrored cover
    def mirror(fn):
        fn(False)
        fn(True)

    def cover(mx):
        f = (lambda x: 40 - x) if mx else (lambda x: x)
        # parked cars (hard)
        for (x, y, w, h) in ((8, 10, 4, 2), (12, 18, 4, 2)):
            X, Y = m(min(f(x), f(x + w)), y)
            b.append(f'<rect x="{X}" y="{Y}" width="{w * S}" height="{h * S}" rx="10" fill="#5C6B8A" {ol(2.5)}/><rect x="{X + S}" y="{Y + 8}" width="{(w - 2) * S}" height="{h * S - 16}" rx="5" fill="{SNOW_LIGHT}"/>')
        # garden walls (low hard)
        for (x, y, w, h) in ((5, 4, 6, 0.6), (5, 25.4, 6, 0.6)):
            X, Y = m(min(f(x), f(x + w)), y)
            b.append(f'<rect x="{X}" y="{Y}" width="{w * S}" height="{h * S}" fill="{BRICK}" {ol(2)}/>')
        # snowbanks (destructible)
        for (x, y, rx, ry) in ((9, 6.5, 2.2, 1), (8, 22, 2, 1.1), (15, 8, 1.6, 1), (14.5, 22.5, 1.6, 0.9), (17.5, 4, 1.4, 0.8)):
            X, Y = m(f(x), y)
            b.append(f'<ellipse cx="{X}" cy="{Y}" rx="{rx * S}" ry="{ry * S}" fill="white" stroke="{DEEP_SHADOW}" stroke-width="3"/>')
        # hedges (sight blockers)
        X, Y = m(min(f(4.6), f(7.1)), 0.6)
        b.append(f'<rect x="{X}" y="{Y}" width="{2.5 * S}" height="{1.2 * S}" rx="12" fill="{PINE}" {ol(2)}/>')
        X, Y = m(min(f(4.6), f(7.1)), 28.2)
        b.append(f'<rect x="{X}" y="{Y}" width="{2.5 * S}" height="{1.2 * S}" rx="12" fill="{PINE}" {ol(2)}/>')
        # wheelie bins
        for (x, y) in ((10.5, 3), (10.5, 26.5)):
            X, Y = m(f(x), y)
            b.append(f'<rect x="{X - 12}" y="{Y - 12}" width="24" height="24" rx="4" fill="#3C8D4A" {ol(2)}/>')
        # trees
        for (x, y) in ((6, 9.6), (13, 2.5), (13, 27.5)):
            X, Y = m(f(x), y)
            b.append(f'<circle cx="{X}" cy="{Y}" r="{1.4 * S}" fill="{PINE}" opacity="0.9" {ol(2.5)}/><circle cx="{X - 8}" cy="{Y - 8}" r="{0.6 * S}" fill="white" opacity="0.8"/>')
    mirror(cover)
    # centre: half-built snowman and lamp post, tree fort spot = high point
    b.append(f'<circle cx="{cx}" cy="{cy}" r="{0.9 * S}" fill="white" {ol(2.5)}/><circle cx="{cx}" cy="{cy}" r="{0.5 * S}" fill="white" {ol(2)}/>')
    lx, ly = m(20, 6)
    b.append(f'<circle cx="{lx}" cy="{ly}" r="10" fill="{GLOW}" {ol(2.5)}/><circle cx="{lx}" cy="{ly}" r="{2.2 * S}" fill="{GLOW}" opacity="0.15"/>')
    hx, hy = m(20, 25)
    b.append(f'<path d="M{hx - 2.6 * S},{hy + 2 * S} Q{hx},{hy - 3 * S} {hx + 2.6 * S},{hy + 2 * S} Z" fill="#DDE6F4" {ol(2.5)}/>')
    b.append(f'<path d="M{hx - 1.4 * S},{hy + 2 * S} Q{hx},{hy - 0.6 * S} {hx + 1.4 * S},{hy + 2 * S}" fill="none" stroke="{SNOW_SHADOW}" stroke-width="2"/>')
    b.append(f'<text x="{hx}" y="{hy + 3 * S}" text-anchor="middle" {FONT} font-size="15" fill="{INK}">snow mound — high point (range bonus)</text>')
    # sledge and bucket pickups
    for (x, y, lab) in ((20, 10.6, "bucket"), (20, 19.6, "sledge")):
        X, Y = m(x, y)
        b.append(f'<rect x="{X - 14}" y="{Y - 8}" width="28" height="16" rx="5" fill="#F2C14E" {ol(2)}/><text x="{X}" y="{Y + 26}" text-anchor="middle" {FONT} font-size="13" fill="{INK}">{lab}</text>')
    # lanes
    for path, lab, lx2, ly2 in ((f"M{m(4, 7)[0]},{m(4, 7)[1]} Q{m(20, 0.5)[0]},{m(20, 0.5)[1]} {m(36, 7)[0]},{m(36, 7)[1]}", "north flank (garden walls, trees)", 20, 1.2),
                                (f"M{m(4, 15)[0]},{m(4, 15)[1]} L{m(36, 15)[0]},{m(36, 15)[1]}", "", 20, 15),
                                (f"M{m(4, 23)[0]},{m(4, 23)[1]} Q{m(20, 19)[0]},{m(20, 19)[1]} {m(36, 23)[0]},{m(36, 23)[1]}", "", 20, 29.2)):
        b.append(f'<path d="{path}" fill="none" stroke="{INK}" stroke-width="3" stroke-dasharray="10 10" opacity="0.45"/>')
        if lab:
            b.append(f'<text x="{m(lx2, ly2)[0]}" y="{m(lx2, ly2)[1]}" text-anchor="middle" {FONT} font-size="14" fill="{INK}" opacity="0.7">{lab}</text>')
    gx, gy = m(27.5, 13.4)
    b.append(f'<path d="M{gx},{gy} l50,0 l-10,-8 m10,8 l-10,8" stroke="#9C6A2E" stroke-width="3" fill="none"/><text x="{gx + 58}" y="{gy + 5}" {FONT} font-size="14" font-weight="700" fill="#9C6A2E">gritting lorry strip</text>')
    # legend
    lx0, ly0 = ox + 40 * S + 30, oy
    leg = [("white", DEEP_SHADOW, "Snowbank — destructible, re-packable"),
           ("#5C6B8A", INK, "Parked car — hard cover (alarm!)"),
           (BRICK, INK, "Garden wall — low hard cover"),
           (PINE, INK, "Hedge / tree — blocks sight"),
           ("#3C8D4A", INK, "Wheelie bin — pushable"),
           ("#C9CFDA", "#C9CFDA", "Packed road — fast, no ammo"),
           (SNOW_LIGHT, INK, "Fresh snow — slow, ammo"),
           ("#F2C14E", INK, "Pickup (bucket, sledge)"),
           (GLOW, INK, "Lamp post — lights on = last 30 s")]
    b.append(f'<text x="{lx0}" y="{ly0 + 20}" {FONT} font-size="22" font-weight="800" fill="{INK}">Legend</text>')
    for i, (fill, stroke, lab) in enumerate(leg):
        yy = ly0 + 50 + i * 40
        b.append(f'<rect x="{lx0}" y="{yy}" width="28" height="22" rx="5" fill="{fill}" stroke="{stroke}" stroke-width="2.5"/><text x="{lx0 + 40}" y="{yy + 17}" {FONT} font-size="15" fill="{INK}">{lab}</text>')
    notes = ["Design notes",
             "• Three lanes + one high point.",
             "• Ammo at the edges, packed road",
             "  in the middle: leave cover to reload.",
             "• Toy: the gritting lorry sweeps the",
             "  centre strip on a timer.",
             "• Cars' alarms reveal kids nearby.",
             "• Grid underneath keeps the layout",
             "  turn-based compatible."]
    for i, t in enumerate(notes):
        b.append(f'<text x="{lx0}" y="{ly0 + 460 + i * 26}" {FONT} font-size="{20 if i == 0 else 15}" font-weight="{800 if i == 0 else 400}" fill="{INK}">{t}</text>')
    (OUT / "Map_MapleClose_v01.svg").write_text(svg(W, H, "".join(b), "Map concept Maple Close v0.1"), encoding="utf-8")


# ------------------------------------------------------------ profile card
def make_card():
    W, H = 760, 1060
    stats = [("Arm", 5), ("Aim", 4), ("Hustle", 3), ("Grit", 2), ("Craft", 2)]
    b = [f'<rect width="{W}" height="{H}" fill="#E7EDF7"/>',
         f'<rect x="40" y="40" width="{W - 80}" height="{H - 80}" rx="36" fill="{EMBER}" {ol(5)}/>',
         f'<rect x="64" y="64" width="{W - 128}" height="{H - 128}" rx="26" fill="#FFF8EE" {ol(3)}/>',
         f'<clipPath id="pc"><rect x="88" y="150" width="{W - 176}" height="400" rx="20"/></clipPath>',
         f'<rect x="88" y="150" width="{W - 176}" height="400" rx="20" fill="#CFE3F5"/>',
         f'<g clip-path="url(#pc)"><path d="M88,470 Q380,430 672,480 L672,560 L88,560 Z" fill="{SNOW_LIGHT}"/>{flakes(W, 560, 50, 21)}'
         + kid(380, 820, 2.05, skin="#7A4A2E", coat="#2BA6A0", legs="#33405E", boots="#C8382E", hair="#1E1A22", hair_style="puffs", pose="throw", mitten="#B5D33D", mitten_r=18, face="determined") + '</g>',
         f'<rect x="88" y="150" width="{W - 176}" height="400" rx="20" fill="none" {ol(3)}/>',
         f'<text x="96" y="122" {FONT} font-size="44" font-weight="900" fill="{INK}">Maisie “Fastball”</text>',
         role_icon("Thrower", 640, 108, EMBER),
         f'<text x="96" y="596" {FONT} font-size="22" font-weight="700" fill="{COCOA}">Okafor · age 10 · Maple Close · Thrower</text>']
    for i, (n, v) in enumerate(stats):
        yy = 636 + i * 36
        b.append(f'<text x="96" y="{yy + 18}" {FONT} font-size="20" font-weight="700" fill="{INK}">{n}</text>')
        for k in range(5):
            b.append(f'<circle cx="{220 + k * 34}" cy="{yy + 11}" r="12" fill="{EMBER if k < v else "#E3DCCF"}" {ol(2)}/>')
    ab = [("Run-up", "throws after moving are faster"), ("Bouncer", "flat, very fast, ignores wind"),
          ("Spinner", "curves round the edge of cover"), ("★ Hat-trick", "three throws, three targets")]
    for i, (n, d) in enumerate(ab):
        yy = 650 + i * 46
        b.append(f'<text x="410" y="{yy}" {FONT} font-size="19" font-weight="800" fill="{INK}">{n}</text><text x="410" y="{yy + 21}" {FONT} font-size="14" fill="{INK}" opacity="0.8">{d}</text>')
    b.append(f'<line x1="96" y1="840" x2="{W - 96}" y2="840" stroke="{INK}" stroke-width="2" opacity="0.25"/>')
    b.append(f'<text x="96" y="874" {FONT} font-size="17" fill="{INK}"><tspan font-weight="800">Likes:</tspan> cricket, keeping score, being right</text>')
    b.append(f'<text x="96" y="902" {FONT} font-size="17" fill="{INK}"><tspan font-weight="800">Dislikes:</tspan> wet socks, “that didn’t count”</text>')
    b.append(f'<text x="96" y="936" {FONT} font-size="17" fill="{INK}" opacity="0.85">Fastest bowler in Year 6. Wears her dad’s goalkeeping gloves</text>')
    b.append(f'<text x="96" y="958" {FONT} font-size="17" fill="{INK}" opacity="0.85">over her mittens. Keeps score out loud. Always.</text>')
    b.append(f'<rect x="{W - 250}" y="866" width="150" height="54" rx="27" fill="{INK}"/><text x="{W - 175}" y="901" text-anchor="middle" {FONT} font-size="24" font-weight="900" fill="white">“Howzat!”</text>')
    (OUT / "ProfileCard_Maisie_v01.svg").write_text(svg(W, H, "".join(b), "Kid profile card v0.1"), encoding="utf-8")


# ---------------------------------------------------------- colour script
def make_palette():
    sw = [("Snow Light", SNOW_LIGHT), ("Snow Shadow", SNOW_SHADOW), ("Deep Shadow", DEEP_SHADOW), ("Sky Morning", SKY),
          ("Window Glow", GLOW), ("Brick", BRICK), ("Pine", PINE), ("Ember Orange · home", EMBER),
          ("Plum Purple · away", PLUM), ("Cocoa", COCOA), ("Ink", INK)]
    W, H = 1600, 520
    b = [f'<rect width="{W}" height="{H}" fill="white"/>',
         f'<text x="60" y="70" {FONT} font-size="38" font-weight="800" fill="{INK}">Colour script v0.1</text>',
         f'<text x="60" y="104" {FONT} font-size="19" fill="{INK}" opacity="0.75">Snow is never pure white: warm cream in light, lavender-blue in shade. Team colours chosen to pop on snow and stay distinct for colour-blind players.</text>']
    for i, (n, c) in enumerate(sw):
        x = 60 + i * 136
        b.append(f'<rect x="{x}" y="140" width="120" height="160" rx="16" fill="{c}" {ol(2.5)}/><text x="{x}" y="330" {FONT} font-size="15" font-weight="700" fill="{INK}">{n.split(" · ")[0]}</text><text x="{x}" y="352" {FONT} font-size="14" fill="{INK}" opacity="0.7">{c}</text>')
    b.append(f'<rect x="60" y="390" width="700" height="96" rx="18" fill="{SNOW_LIGHT}" {ol(2.5)}/>')
    b.append(f'<path d="M60,450 Q410,420 760,450 L760,468 Q760,486 742,486 L78,486 Q60,486 60,468 Z" fill="{SNOW_SHADOW}"/>')
    for i, c in enumerate((EMBER, PLUM, EMBER, PLUM)):
        b.append(f'<circle cx="{150 + i * 160}" cy="438" r="30" fill="{c}" {ol(3)}/>')
    b.append(f'<text x="800" y="430" {FONT} font-size="17" fill="{INK}">Team colours over lit and shaded snow. Team is always shown by colour <tspan font-weight="800">and</tspan> shape</text>')
    b.append(f'<text x="800" y="456" {FONT} font-size="17" fill="{INK}">(flag badge over each kid), never colour alone.</text>')
    (OUT / "ColourScript_v01.svg").write_text(svg(W, H, "".join(b), "Colour script v0.1"), encoding="utf-8")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    make_lineup()
    make_keyart()
    make_map()
    make_card()
    make_palette()
    print("Wrote concept sheets to", OUT)

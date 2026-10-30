/* My courses: tear a closed program pack open by dragging across its top, like opening a card pack, and light pours out.
   The pack's Enroll button still does the saving (a normal postback): without this script it is a plain button, and keyboard
   users press it to see the same opening. Everything is drawn on one canvas over the page, because the pack rail clips
   whatever leaves it. With reduced motion the tear still follows the pointer, but nothing flashes. */
(function () {
    'use strict';

    const rail = document.querySelector('.my-courses .mc-program-scroll');
    if (!rail) return;

    // Each program is its own page load and the rail restarts at the top: bring the selected pack back into view.
    const selected = rail.querySelector('.mc-program[data-selected=true]');
    if (selected) rail.scrollTop = selected.offsetTop - (rail.clientHeight - selected.offsetHeight) / 2;

    const closedPacks = [...rail.querySelectorAll('.mc-program[data-kit=closed]')];
    const arrived = rail.querySelector('.mc-program[data-just-opened]');
    const endArrivalCover = () => document.body.classList.remove('mc-kit-arrival');
    if (!closedPacks.length && !arrived) {
        endArrivalCover();
        return;
    }

    const reducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;
    const clamp = (value, low, high) => Math.min(high, Math.max(low, value));
    const smooth = (from, to, value) => {
        const p = clamp((value - from) / (to - from), 0, 1);
        return p * p * (3 - 2 * p);
    };
    const ease = (rate, dt) => 1 - Math.exp(-rate * dt); // how far an eased value moves towards its target in dt seconds
    const random = (low, high) => low + Math.random() * (high - low);
    const sheet = (width, height) => Object.assign(document.createElement('canvas'), { width, height });

    // ---- The pack, measured in artwork pixels -----------------------------------------------------------

    // Both cut-outs are the kit JPGs without their black background.
    const ART_WIDTH = 700, ART_HEIGHT = 950;
    const closedArt = new Image(), openArt = new Image();
    closedArt.src = 'assets/my-courses/skillup-kit-closed-cut.webp';
    openArt.src = 'assets/my-courses/skillup-kit-open-cut.webp';

    // Where the front sheet is torn in the open artwork, traced left to right. The drag tears along this line, so what
    // appears behind the flap is already the real open pack.
    const TORN_EDGE = [[30, 84], [60, 90], [103, 88], [125, 103], [140, 108], [160, 100], [170, 108], [190, 128], [205, 135],
        [218, 145], [235, 158], [255, 165], [270, 170], [280, 175], [300, 172], [315, 175], [330, 173], [345, 183], [360, 178],
        [375, 172], [385, 162], [400, 165], [425, 158], [445, 152], [460, 148], [470, 138], [490, 130], [510, 120], [525, 105],
        [540, 90], [555, 84], [575, 90], [600, 100], [640, 80], [682, 82]];
    const edge = new Float32Array(ART_WIDTH + 1);
    for (let x = 0, i = 0; x <= ART_WIDTH; x++) {
        while (i < TORN_EDGE.length - 2 && x > TORN_EDGE[i + 1][0]) i++;
        const [x0, y0] = TORN_EDGE[i], [x1, y1] = TORN_EDGE[i + 1];
        edge[x] = y0 + (y1 - y0) * clamp((x - x0) / (x1 - x0), 0, 1) + Math.sin(x * 1.3) * 1.3 + Math.sin(x * 0.41) * 1.7;
    }
    const edgeY = x => edge[clamp(Math.round(x), 0, ART_WIDTH)];

    const PACK_LEFT = 46, PACK_RIGHT = 652;   // the sides of the closed pack at its top
    const guide = 'M' + TORN_EDGE.filter(point => point[0] > PACK_LEFT && point[0] < PACK_RIGHT).map(point => point.join(' ')).join('L');
    const TEAR_START = 18, TEAR_LENGTH = 664; // the tear runs from just outside one side of the pack to the other
    const FINISH_AT = 0.93;                   // the tear finishes by itself this far across, as the last bit gives way
    const MIN_TRAVEL = 0.35;                  // the pointer must have moved at least this share of the pack's width
    const MOUTH = { x: 345, y: 120 };         // where the light pours out
    const HINGE = 62;                         // the top edge of the pack, which the peeled flap bends along
    const SLICE = 6;                          // width of the strips the bending flap is drawn in
    const BUILD_UP = 0.85, FLOOD = 0.42;      // seconds: the light gathers in the pack, then floods the page
    const TINTS = ['#ffffff', '#ddd3ff', '#a9e7ff', '#d9ff70'];

    const frontOf = o => o.direction > 0 ? TEAR_START + o.torn * TEAR_LENGTH : TEAR_START + TEAR_LENGTH - o.torn * TEAR_LENGTH;

    // ---- The tearing sound ----------------------------------------------------------------------------------
    // Played once, the moment the pack is fully torn. Web Audio so it starts without lag; the context is made on the first
    // press, which is the gesture browsers want before any sound, and the file is fetched then so it is ready by the end.

    let audio = null, tearBuffer = null, tearLoading = false;

    function prepareSound() {
        if (!audio) {
            const Context = window.AudioContext || window.webkitAudioContext;
            if (!Context) return;
            audio = new Context();
        }
        if (audio.state === 'suspended') audio.resume().catch(() => {});
        if (tearBuffer || tearLoading) return;
        tearLoading = true;
        fetch('assets/my-courses/tearing_pack.mp3')
            .then(r => r.arrayBuffer())
            .then(data => audio.decodeAudioData(data))
            .then(buffer => { tearBuffer = buffer; })
            .catch(() => { tearLoading = false; });
    }

    function playTearSound() {
        if (!audio || !tearBuffer || reducedMotion()) return;
        const source = audio.createBufferSource();
        source.buffer = tearBuffer;
        source.connect(audio.destination);
        source.start();
    }

    // ---- Light, drawn once and reused every frame ---------------------------------------------------------

    const HALO_PAD = 150;
    let litBackSheet, halo, rayWheels, mote;

    function rayWheel(count, core, rim) {
        const radius = 320, wheel = sheet(radius * 2, radius * 2), g = wheel.getContext('2d');
        g.translate(radius, radius);
        g.filter = 'blur(3px)';
        const fill = g.createRadialGradient(0, 0, 0, 0, 0, radius);
        fill.addColorStop(0, core);
        fill.addColorStop(0.3, rim + 'b0');
        fill.addColorStop(1, rim + '00');
        g.fillStyle = fill;
        for (let i = 0; i < count; i++) {
            const angle = random(0, Math.PI * 2), half = random(0.012, 0.05), length = radius * random(0.6, 1);
            g.beginPath();
            g.moveTo(0, 0);
            g.lineTo(Math.cos(angle - half) * length, Math.sin(angle - half) * length);
            g.lineTo(Math.cos(angle + half) * length, Math.sin(angle + half) * length);
            g.fill();
        }
        return wheel;
    }

    function prepareLight() {
        // The back sheet lit from inside the pack, strongest at the top.
        litBackSheet = sheet(ART_WIDTH, ART_HEIGHT);
        let g = litBackSheet.getContext('2d');
        g.drawImage(openArt, 0, 0);
        g.globalCompositeOperation = 'source-atop';
        g.fillStyle = '#ece6ff';
        g.fillRect(0, 0, ART_WIDTH, ART_HEIGHT);
        g.globalCompositeOperation = 'destination-in';
        const fade = g.createLinearGradient(0, 0, 0, 230);
        fade.addColorStop(0.35, '#000');
        fade.addColorStop(1, '#0000');
        g.fillStyle = fade;
        g.fillRect(0, 0, ART_WIDTH, ART_HEIGHT);

        // A soft glow in the pack's outline, drawn behind it.
        halo = sheet(ART_WIDTH + HALO_PAD * 2, ART_HEIGHT + HALO_PAD * 2);
        g = halo.getContext('2d');
        g.filter = 'blur(44px)';
        g.drawImage(openArt, HALO_PAD, HALO_PAD);
        g.filter = 'none';
        g.globalCompositeOperation = 'source-in';
        g.fillStyle = '#b8a4ff';
        g.fillRect(0, 0, halo.width, halo.height);

        rayWheels = [rayWheel(26, '#ffffff', '#b9a6ff'), rayWheel(18, '#f2fdff', '#7fd8ff')];

        mote = sheet(32, 32);
        g = mote.getContext('2d');
        const dot = g.createRadialGradient(16, 16, 0, 16, 16, 16);
        dot.addColorStop(0, '#fff');
        dot.addColorStop(0.22, '#ffffffc8');
        dot.addColorStop(1, '#fff0');
        g.fillStyle = dot;
        g.fillRect(0, 0, 32, 32);
    }

    // ---- One opening on screen -------------------------------------------------------------------------

    let opening = null;
    let saving = false; // the Enroll button has been pressed for real

    function begin(pack, phase) {
        const canvas = sheet(1, 1);
        canvas.className = 'mc-tear-stage';
        canvas.setAttribute('aria-hidden', 'true');
        document.body.append(canvas);
        opening = {
            pack, phase, canvas, ctx: canvas.getContext('2d'),
            caption: pack.querySelector('.mc-program-caption'),
            button: pack.querySelector('button.mc-program-cta'),
            direction: 1, auto: false,
            goal: 0, torn: 0, speed: 0,                                       // the tear
            last: 0, time: 0, lightTime: 0,                                   // clocks, in seconds
            alpha: 1, closed: 1, scale: 1, tilt: 0, tremble: 0, kick: 0,      // the pack
            dim: 0, inner: 0, halo: 0, rays: 0, reach: 0, bloom: 0, flash: 0, // the light
            flap: null, layer: sheet(1, 1), ring: null, sparks: [], sparkDue: 0
        };
        opening.raf = requestAnimationFrame(now => {
            opening.last = now;
            frame(now);
        });
        return opening;
    }

    function end() {
        const o = opening;
        if (!o) return;
        cancelAnimationFrame(o.raf);
        o.canvas.remove();
        o.pack.classList.remove('is-tearing');
        document.documentElement.classList.remove('mc-tearing');
        opening = null;
    }

    function startTear(pack, direction, auto) {
        const o = begin(pack, 'tear');
        o.direction = direction;
        o.auto = auto;
        o.alpha = Number(getComputedStyle(pack).opacity) || 1;
        pack.classList.add('is-tearing');
        if (!auto) document.documentElement.classList.add('mc-tearing');
    }

    function save(o) {
        saving = true;
        o.button.click();
    }

    function finishTear(o) {
        o.torn = 1;
        playTearSound();
        if (reducedMotion()) {
            o.phase = 'held';
            save(o);
            return;
        }
        o.phase = 'light';
        o.kick = 1;
        o.flap = { x: 0, y: 0, spin: 0, vx: o.direction * 300, vy: -900, vr: o.direction * 2.2, alpha: 1 };
    }

    // ---- Each frame: move everything on, then paint it ------------------------------------------------------

    function frame(now) {
        const o = opening;
        if (!o) return;
        const dt = clamp((now - o.last) / 1000, 0, 0.05);
        o.last = now;
        o.time += dt;
        advance(o, dt);
        if (opening !== o) return;
        paint(o, dt);
        o.raf = requestAnimationFrame(frame);
    }

    function advance(o, dt) {
        const calm = reducedMotion();
        if (o.phase === 'tear') {
            if (o.auto) o.goal = Math.min(1, o.goal + dt / 0.75);
            const before = o.torn;
            o.torn += (o.goal - o.torn) * ease(20, dt);
            o.speed = dt ? (o.torn - before) / dt : 0;
            o.inner = calm ? 0 : 0.5 + 0.2 * Math.sin(o.time * 31) * Math.sin(o.time * 17);
            if (o.goal >= 1 && o.torn > 0.985) finishTear(o);
        } else if (o.phase === 'release') {
            o.torn -= o.torn * ease(14, dt);
            if (o.torn < 0.004) {
                end();
                return;
            }
        } else if (o.phase === 'light') {
            o.lightTime += dt;
            const t = o.lightTime, gather = smooth(0, BUILD_UP, t), flood = smooth(BUILD_UP, BUILD_UP + FLOOD, t);
            o.inner = 0.55 + 0.45 * gather;
            o.halo = 0.25 * gather + 0.75 * flood;
            o.rays = 0.3 * smooth(0.15, BUILD_UP, t) + 0.7 * flood;
            o.reach = flood;
            o.bloom = 0.3 * gather + 0.7 * flood;
            o.flash = 0.97 * flood * flood;
            o.scale = 1 + 0.05 * gather;
            o.tremble = gather * gather * (1 - flood) * 2.4;
            o.closed = 1 - smooth(BUILD_UP, BUILD_UP + FLOOD * 0.8, t);
            if (!o.ring && t >= BUILD_UP) o.ring = { age: 0 };
            if (t >= BUILD_UP + FLOOD) {
                o.phase = 'held';
                save(o);
            }
        } else if (o.phase === 'arrival') {
            const t = o.time;
            o.flash = 0.97 * (1 - smooth(0.05, 0.95, t));
            o.bloom = 1 - smooth(0, 0.9, t);
            o.rays = 1 - smooth(0.1, 1.5, t);
            o.reach = 1 - 0.4 * smooth(0, 1.5, t);
            o.halo = o.rays;
            o.inner = 0.9 * o.rays;
            o.scale = 1 + 0.05 * (1 - smooth(0, 1.1, t));
            o.alpha = 1 - smooth(1.35, 1.75, t);
            if (t >= 1.75) {
                end();
                return;
            }
        }

        const dimTo = o.phase === 'arrival' || o.phase === 'release' || calm ? 0 : 0.68 * smooth(0, 0.1, o.torn);
        o.dim += (dimTo - o.dim) * ease(10, dt);
        if (o.phase !== 'arrival') o.alpha += (1 - o.alpha) * ease(8, dt);
        o.kick -= o.kick * ease(9, dt);
        o.tilt += ((o.phase === 'tear' && !calm ? clamp(o.speed * 0.02, -0.03, 0.03) * o.direction : 0) - o.tilt) * ease(12, dt);
        if (o.flap) {
            const f = o.flap;
            f.vy += 1500 * dt;
            f.x += f.vx * dt;
            f.y += f.vy * dt;
            f.spin += f.vr * dt;
            f.alpha = 1 - smooth(0.25, 0.7, o.lightTime);
        }
    }

    function emit(o, x, y, count, power, streak) {
        for (let i = 0; i < count; i++) {
            const angle = random(0, Math.PI * 2), pace = power * random(0.25, 1);
            o.sparks.push({
                x, y, streak, age: 0, life: random(0.35, 1.05), tint: TINTS[Math.floor(random(0, TINTS.length))],
                size: streak ? random(0.8, 1.9) : random(2.5, 7),
                vx: Math.cos(angle) * pace, vy: Math.sin(angle) * pace - power * 0.25, gravity: streak ? 800 : 60
            });
        }
    }

    function paintSparks(ctx, o, dt) {
        o.sparks = o.sparks.filter(s => (s.age += dt) < s.life);
        ctx.lineCap = 'round';
        for (const s of o.sparks) {
            const drag = 1 - 2.4 * dt;
            s.vx *= drag;
            s.vy = s.vy * drag + s.gravity * dt;
            s.x += s.vx * dt;
            s.y += s.vy * dt;
            ctx.globalAlpha = 1 - s.age / s.life;
            if (s.streak) {
                ctx.strokeStyle = s.tint;
                ctx.lineWidth = s.size;
                ctx.beginPath();
                ctx.moveTo(s.x, s.y);
                ctx.lineTo(s.x - s.vx * 0.035, s.y - s.vy * 0.035);
                ctx.stroke();
            } else {
                const r = s.size * (0.7 + 0.5 * Math.sin(s.age * 24 + s.x));
                ctx.drawImage(mote, s.x - r, s.y - r, r * 2, r * 2);
            }
        }
        ctx.globalAlpha = 1;
    }

    // The part of the pack the tear has passed, down to the torn edge. The open pack is taller than the closed one, so the
    // upper limit slopes away behind the flap: the back sheet rises into view instead of being cut off at the tear.
    function traceTorn(ctx, o) {
        const front = frontOf(o), start = o.direction > 0 ? -90 : ART_WIDTH + 90;
        ctx.moveTo(start, -900);
        for (let x = start; (front - x) * o.direction > 0; x += o.direction * 4) ctx.lineTo(x, edgeY(x));
        ctx.lineTo(front, edgeY(front));
        ctx.lineTo(front, HINGE);
        ctx.lineTo(front - o.direction * 1200, HINGE - 300);
        ctx.closePath();
    }

    function paintPack(ctx, o) {
        const torn = o.torn > 0;
        // The open pack shows where the tear has passed, and all of it once the closed pack fades.
        if (torn || o.closed < 1) {
            ctx.save();
            if (o.closed >= 1) {
                ctx.beginPath();
                traceTorn(ctx, o);
                ctx.clip();
            }
            ctx.globalAlpha = o.alpha;
            ctx.drawImage(openArt, 0, 0);
            if (o.inner > 0) {
                ctx.globalCompositeOperation = 'lighter';
                ctx.globalAlpha = o.alpha * o.inner;
                ctx.drawImage(litBackSheet, 0, 0);
            }
            ctx.restore();
        }
        if (o.closed > 0) {
            ctx.save();
            ctx.beginPath();
            ctx.rect(-500, -900, ART_WIDTH + 1000, ART_HEIGHT + 1800);
            traceTorn(ctx, o);
            ctx.clip('evenodd');
            ctx.globalAlpha = o.alpha * o.closed;
            ctx.drawImage(closedArt, 0, 0);
            ctx.restore();
        }

        // The program name sticker lives on the page; the pack hides it, so it is drawn here too.
        const k = ART_WIDTH / o.pack.offsetWidth, cap = o.caption;
        const width = cap.offsetWidth * k, height = cap.offsetHeight * k;
        ctx.save();
        ctx.globalAlpha = o.alpha;
        ctx.translate(ART_WIDTH / 2, (cap.offsetTop + cap.offsetHeight / 2) * k);
        ctx.rotate(-2 * Math.PI / 180);
        ctx.fillStyle = '#cfe8f7';
        ctx.beginPath();
        ctx.roundRect(-width / 2, -height / 2, width, height, 7 * k);
        ctx.fill();
        ctx.fillStyle = '#0b0b0d';
        ctx.font = (12 * k) + "px 'Archivo Black','DM Sans',sans-serif";
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(cap.textContent.toUpperCase(), 0, 0.5 * k, width - 16 * k);
        ctx.restore();

        // The torn edge catches the light.
        if (torn && o.closed > 0) {
            const front = frontOf(o);
            ctx.save();
            ctx.beginPath();
            for (let x = o.direction > 0 ? PACK_LEFT : PACK_RIGHT; (front - x) * o.direction > 0; x += o.direction * 4) ctx.lineTo(x, edgeY(x));
            ctx.lineTo(clamp(front, PACK_LEFT, PACK_RIGHT), edgeY(front));
            ctx.globalAlpha = o.closed * o.alpha;
            ctx.globalCompositeOperation = 'lighter';
            ctx.lineJoin = 'round';
            ctx.strokeStyle = '#a58dff50';
            ctx.lineWidth = 10;
            ctx.stroke();
            ctx.strokeStyle = '#ffffffd8';
            ctx.lineWidth = 2.2;
            ctx.stroke();
            ctx.restore();
        }
    }

    // The peeled part of the front sheet: strips of the closed art, each bent a little further than the one before. They are
    // drawn on their own layer, so the seams between strips never show, and the foil gloss goes across the flap in one piece.
    function paintFlap(ctx, o) {
        const f = o.flap, front = frontOf(o), strips = [];
        const curl = 0.74 - 0.4 * o.torn; // a long flap lies flatter than a short one
        let along = 0, px = front, py = HINGE;
        for (let x = front - o.direction * SLICE / 2; x >= TEAR_START && x <= TEAR_START + TEAR_LENGTH; x -= o.direction * SLICE) {
            along += SLICE;
            const bend = curl * (1 - Math.exp(-along / 80)) + 0.05 * Math.sin(along * 0.02 - o.time * 8) * smooth(0, 160, along);
            px -= o.direction * Math.cos(bend) * SLICE;
            py -= Math.sin(bend) * SLICE;
            strips.push({ x, px, py, bend });
        }
        if (!strips.length) return;

        const { layer } = o, g = layer.getContext('2d');
        if (layer.width !== o.canvas.width || layer.height !== o.canvas.height) {
            layer.width = o.canvas.width;
            layer.height = o.canvas.height;
        }
        g.resetTransform();
        g.clearRect(0, 0, layer.width, layer.height);
        g.setTransform(ctx.getTransform());
        if (f) {
            g.translate(350 + f.x, 120 + f.y);
            g.rotate(f.spin);
            g.translate(-350, -120);
        }
        const width = SLICE + 3;
        for (let i = strips.length - 1; i >= 0; i--) {
            const s = strips[i], height = edgeY(s.x);
            g.save();
            g.translate(s.px, s.py);
            g.rotate(o.direction * s.bend);
            g.drawImage(closedArt, s.x - width / 2, 0, width, height, -width / 2, -HINGE, width, height);
            g.restore();
        }
        const tip = strips[strips.length - 1], gloss = g.createLinearGradient(front, HINGE, tip.px, tip.py);
        for (let i = 0; i <= 8; i++) gloss.addColorStop(i / 8, 'rgba(232,226,255,' + (0.04 + 0.3 * Math.abs(Math.sin(i * 0.8 + o.time * 4))) + ')');
        g.globalCompositeOperation = 'source-atop';
        g.fillStyle = gloss;
        g.fillRect(-900, -1400, 2500, 2800);
        g.globalCompositeOperation = 'source-over';

        // The flap's own torn edge.
        g.beginPath();
        for (const s of strips) {
            const drop = edgeY(s.x) - HINGE;
            g.lineTo(s.px - o.direction * Math.sin(s.bend) * drop, s.py + Math.cos(s.bend) * drop);
        }
        g.lineJoin = 'round';
        g.strokeStyle = '#ffffffd0';
        g.lineWidth = 2.2;
        g.stroke();

        ctx.save();
        ctx.resetTransform();
        ctx.globalAlpha = f ? f.alpha : o.alpha;
        ctx.drawImage(layer, 0, 0);
        ctx.restore();
    }

    function paint(o, dt) {
        const { canvas, ctx } = o;
        const view = canvas.getBoundingClientRect();
        const density = Math.min(2, devicePixelRatio || 1);
        const stageWidth = Math.round(view.width * density), stageHeight = Math.round(view.height * density);
        if (canvas.width !== stageWidth || canvas.height !== stageHeight) {
            canvas.width = stageWidth;
            canvas.height = stageHeight;
        }
        ctx.resetTransform();
        ctx.scale(density, density);
        ctx.clearRect(0, 0, view.width, view.height);

        const box = o.pack.getBoundingClientRect();
        const unit = box.width / ART_WIDTH * o.scale; // page pixels per artwork pixel
        const centerX = box.left - view.left + box.width / 2 + random(-o.tremble, o.tremble);
        const centerY = box.top - view.top + box.height / 2 + random(-o.tremble, o.tremble) + o.kick * 9;
        const stageX = x => centerX + (x - ART_WIDTH / 2) * unit, stageY = y => centerY + (y - ART_HEIGHT / 2) * unit;
        const mouthX = stageX(MOUTH.x), mouthY = stageY(MOUTH.y);
        const diagonal = Math.hypot(view.width, view.height);

        // The page falls back, with a pool of light left around the pack.
        if (o.dim > 0.003) {
            const shade = ctx.createRadialGradient(centerX, centerY, box.height * 0.3, centerX, centerY, diagonal * 0.75);
            shade.addColorStop(0, 'rgba(5,3,14,' + o.dim * 0.5 + ')');
            shade.addColorStop(1, 'rgba(5,3,14,' + o.dim + ')');
            ctx.fillStyle = shade;
            ctx.fillRect(0, 0, view.width, view.height);
        }

        // Light behind the pack: slow rays turning against each other.
        if (o.rays > 0.003) {
            const radius = box.height * (0.8 + o.rays * 0.4) + diagonal * o.reach * 0.8;
            ctx.save();
            ctx.globalCompositeOperation = 'lighter';
            ctx.translate(mouthX, mouthY);
            ctx.globalAlpha = o.rays;
            ctx.rotate(o.time * 0.14);
            ctx.drawImage(rayWheels[0], -radius, -radius, radius * 2, radius * 2);
            ctx.rotate(o.time * -0.32);
            ctx.globalAlpha = o.rays * 0.7;
            ctx.drawImage(rayWheels[1], -radius, -radius, radius * 2, radius * 2);
            ctx.restore();
        }

        ctx.save();
        ctx.translate(centerX, centerY);
        ctx.rotate(o.tilt);
        ctx.scale(unit, unit);
        ctx.translate(-ART_WIDTH / 2, -ART_HEIGHT / 2);
        if (o.halo > 0.003) {
            ctx.save();
            ctx.globalCompositeOperation = 'lighter';
            ctx.globalAlpha = o.halo;
            ctx.drawImage(halo, -HALO_PAD, -HALO_PAD);
            ctx.restore();
        }
        paintPack(ctx, o);
        if (o.torn > 0 && (!o.flap || o.flap.alpha > 0.01)) paintFlap(ctx, o);
        ctx.restore();

        ctx.globalCompositeOperation = 'lighter';

        // Sparks fly from the point of the tear; the faster the drag, the more.
        if (o.phase === 'tear' && !reducedMotion()) {
            const front = frontOf(o), tipX = stageX(front), tipY = stageY(edgeY(front));
            const flare = ctx.createRadialGradient(tipX, tipY, 0, tipX, tipY, 62 * unit);
            flare.addColorStop(0, '#ffffff');
            flare.addColorStop(0.25, '#c9bbffa0');
            flare.addColorStop(1, '#8b5cf600');
            ctx.fillStyle = flare;
            ctx.fillRect(tipX - 62 * unit, tipY - 62 * unit, 124 * unit, 124 * unit);
            o.sparkDue += Math.abs(o.speed) * dt * 90;
            if (o.sparkDue >= 1) {
                emit(o, tipX, tipY, Math.floor(o.sparkDue), 300, true);
                o.sparkDue %= 1;
            }
        }

        if (o.bloom > 0.003) {
            const radius = box.height * (0.35 + 0.35 * o.bloom) + diagonal * o.reach * 0.7;
            const glow = ctx.createRadialGradient(mouthX, mouthY, 0, mouthX, mouthY, radius);
            glow.addColorStop(0, 'rgba(255,255,255,' + o.bloom + ')');
            glow.addColorStop(0.3, 'rgba(204,190,255,' + o.bloom * 0.6 + ')');
            glow.addColorStop(1, 'rgba(120,90,255,0)');
            ctx.fillStyle = glow;
            ctx.fillRect(0, 0, view.width, view.height);
        }

        // The moment it bursts: a shock ring and a spray of light.
        if (o.ring) {
            if (!o.ring.age) {
                emit(o, mouthX, mouthY, 34, diagonal * 0.5, true);
                emit(o, mouthX, mouthY, 46, diagonal * 0.34, false);
            }
            o.ring.age += dt;
            const p = o.ring.age / 0.6;
            if (p < 1) {
                const radius = box.height * 0.2 + diagonal * 0.65 * (1 - Math.pow(1 - p, 3));
                const wave = ctx.createRadialGradient(mouthX, mouthY, radius * 0.72, mouthX, mouthY, radius);
                wave.addColorStop(0, 'rgba(214,202,255,0)');
                wave.addColorStop(0.82, 'rgba(236,230,255,' + 0.5 * (1 - p) + ')');
                wave.addColorStop(1, 'rgba(214,202,255,0)');
                ctx.fillStyle = wave;
                ctx.fillRect(0, 0, view.width, view.height);
            }
        }
        paintSparks(ctx, o, dt);
        ctx.globalCompositeOperation = 'source-over';

        if (o.flash > 0.003) {
            ctx.fillStyle = 'rgba(244,240,255,' + o.flash + ')';
            ctx.fillRect(0, 0, view.width, view.height);
        }
    }

    // ---- Dragging ---------------------------------------------------------------------------------------

    const TEAR_MARKUP = '<span class="mc-tear" aria-hidden="true"><svg viewBox="0 0 ' + ART_WIDTH + ' ' + ART_HEIGHT + '" preserveAspectRatio="none">'
        + '<path class="mc-tear-line" pathLength="100" d="' + guide + '"/><path class="mc-tear-glint" pathLength="100" d="' + guide + '"/></svg></span>'
        + '<span class="mc-program-cta mc-tear-hint" aria-hidden="true"><span class="mc-cta-label">Drag to open</span><span class="mc-cta-icon">'
        + '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 12h15m-6-6 6 6-6 6"/></svg></span></span>';

    // The tear follows the pointer and never closes up again while the pointer is held. How far it gets also depends on how
    // far the pointer has really travelled, so a small twitch can never rip a pack open, wherever the press landed.
    function follow(o, clientX, pressX) {
        const box = o.pack.getBoundingClientRect();
        const across = (clientX - box.left) / box.width * ART_WIDTH;
        const reached = ((o.direction > 0 ? across : ART_WIDTH - across) - TEAR_START) / TEAR_LENGTH;
        const travelled = Math.abs(clientX - pressX) / box.width;
        o.goal = Math.max(o.goal, reached >= FINISH_AT && travelled >= MIN_TRAVEL ? 1 : clamp(Math.min(reached, 0.1 + travelled * 1.6), 0, 1));
    }

    function enableTear(pack) {
        const link = pack.querySelector('.mc-program-link');
        link.insertAdjacentHTML('beforeend', TEAR_MARKUP);
        pack.classList.add('mc-can-tear');
        const zone = link.querySelector('.mc-tear');
        let press = null, swallowClick = false;

        zone.addEventListener('pointerdown', e => {
            swallowClick = false;
            prepareSound();
            if (e.isPrimary && e.button === 0 && !opening && !saving) press = { id: e.pointerId, x: e.clientX, y: e.clientY, dragging: false };
        });
        pack.addEventListener('pointermove', e => {
            if (!press || e.pointerId !== press.id) return;
            if (!press.dragging) {
                const dx = e.clientX - press.x, dy = e.clientY - press.y;
                if (Math.abs(dx) < 6 && Math.abs(dy) < 6) return;
                if (Math.abs(dy) > Math.abs(dx)) {
                    press = null;
                    return;
                }
                // Only now: capturing on press would turn a plain click on the pack into a click on nothing.
                press.dragging = swallowClick = true;
                pack.setPointerCapture(e.pointerId);
                // Starting near a side tears away from it; from the middle, the first movement decides.
                const box = pack.getBoundingClientRect(), side = (press.x - box.left) / box.width;
                startTear(pack, side < 0.35 ? 1 : side > 0.65 ? -1 : Math.sign(dx), false);
            }
            if (opening && opening.pack === pack && opening.phase === 'tear') follow(opening, e.clientX, press.x);
        });
        const letGo = e => {
            if (!press || e.pointerId !== press.id) return;
            press = null;
            // Once the far side is reached the tear finishes by itself, however quickly the pointer lets go.
            if (opening && opening.pack === pack && opening.phase === 'tear' && opening.goal < 1) opening.phase = 'release';
        };
        pack.addEventListener('pointerup', letGo);
        pack.addEventListener('pointercancel', letGo);
        pack.addEventListener('click', e => {
            if (!swallowClick || !e.detail) return;
            swallowClick = false;
            e.preventDefault();
            e.stopPropagation();
        }, true);
        pack.addEventListener('dragstart', e => e.preventDefault());
    }

    // Enter on the Enroll button plays the same opening; the opening then presses the button for real.
    rail.addEventListener('click', e => {
        const pack = e.target.closest('button.mc-program-cta')?.closest('.mc-can-tear');
        if (!pack || saving || reducedMotion()) return;
        e.preventDefault();
        prepareSound();
        if (!opening) startTear(pack, 1, true);
    });

    // Back from the next page can restore this one exactly as it was left, in the middle of the light.
    addEventListener('pageshow', e => {
        if (!e.persisted) return;
        saving = false;
        end();
    });

    // The enhancement starts once the art is ready; until then the Enroll button works as it always did.
    Promise.all([closedArt.decode(), openArt.decode()]).then(() => {
        prepareLight();
        closedPacks.forEach(enableTear);
        if (arrived && !reducedMotion()) {
            const o = begin(arrived, 'arrival');
            o.closed = 0;
            o.rays = o.reach = o.flash = o.bloom = o.inner = o.halo = 1;
            o.scale = 1.05;
            const box = arrived.getBoundingClientRect();
            emit(o, box.left + box.width / 2, box.top + box.height / 8, 40, 420, false);
            requestAnimationFrame(() => requestAnimationFrame(endArrivalCover));
        } else {
            endArrivalCover();
        }
    }).catch(endArrivalCover);
})();

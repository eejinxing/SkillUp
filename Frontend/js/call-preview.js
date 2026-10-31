/* Calls. A button with data-call-peer (a user id) rings that signed-in person; the audio and video then go straight
   between the two browsers over WebRTC, and only the small set-up messages pass through the site (Backend/CallSignals.cs,
   polled below). Buttons without a peer (live sessions) open a device-only preview, and the header's [data-call-test]
   buttons simulate an incoming call. Opened by [data-start-call] (data-call-name / data-call-role / data-call-title).
   The page supplies #call-context (SkillUp.Master). No STUN or TURN server: both people must be on the same network. */
(function () {
    'use strict';

    // The header's simulated-call buttons were removed from SkillUp.Master now that real calls work. Put them back there
    // (buttons with data-call-test, and data-call-test="voice") and set this to true to simulate an incoming call again.
    const SHOW_INCOMING_CALL_TEST = false;

    let ctx = {};
    try { ctx = JSON.parse(document.getElementById('call-context')?.textContent || '{}'); } catch { ctx = {}; }
    const me = ctx.me || { name: 'You' };
    const course = ctx.course || null;

    let call = null;      // the open call: {name, title, role, video, demo, notes, chat, started, status, minimized, expanded, summaryClosed, focusChat}
    let stream = null;    // camera / microphone
    let timer = null;     // updates the elapsed time every second
    let request = 0;      // device requests; a newer one makes an older answer stale
    let incoming = null;  // the incoming call (real or simulated) waiting for Accept / Decline
    let net = null;       // the live connection to the other person: {peer, id, callee, pc, queue, ring}
    let remote = null;    // the other person's audio and video

    // ---- Helpers -----------------------------------------------------------------------------------------
    const esc = v => String(v ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const initials = name => esc(String(name || '?').split(' ').filter(Boolean).map(x => x[0]).slice(0, 2).join(''));
    const firstName = name => String(name).split(' ')[0];
    const svg = paths => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths}</svg>`;
    const icons = {
        phone: svg('<path d="m5 3 4 1 1 5-3 2c2 3 3 4 6 6l2-3 5 1 1 4c-1 4-6 3-11-1S0 5 5 3Z"/>'),
        video: svg('<rect x="3" y="6" width="12" height="12" rx="3"/><path d="m15 10 6-4v12l-6-4"/>'),
        mic: svg('<rect x="9" y="2" width="6" height="13" rx="3"/><path d="M5 10v2a7 7 0 0 0 14 0v-2M12 19v3m-4 0h8"/>'),
        end: svg('<path d="M3 15c4-6 14-6 18 0l-2 4-5-2v-3h-4v3l-5 2Z"/>'),
        expand: svg('<path d="M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5"/>'),
        min: svg('<path d="m7 7 10 10M17 7 7 17"/>'),
        arrow: svg('<path d="M7 17 17 7M8 7h9v9"/>'),
        gear: svg('<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9 7 7M17 17l2.1 2.1M19.1 4.9 17 7M7 17l-2.1 2.1"/>'),
        send: svg('<path d="m4 12 16-8-6 16-2-6-8-2Z"/>'),
        shrink: svg('<path d="M4 14h6v6M20 10h-6V4M14 10l7-7M3 21l7-7"/>'),
        doc: svg('<path d="M6 3h8l4 4v14H6Z"/><path d="M14 3v4h4M9 12h6M9 16h4"/>'),
        target: svg('<circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="3"/>'),
        pencil: svg('<path d="m4 16 12-12 4 4L8 20H4Z"/>'),
    };
    const demoCaller = 'assets/call-preview/demo-caller-2.webp';
    const demoYou = 'assets/call-preview/demo-you.webp';
    const demoPhoto = `<img class="call-demo-photo" src="${demoCaller}" alt="Illustrative demo caller, not a live remote feed">`;

    const lessons = () => course?.lessons || [];
    const goalPlaceholder = () => esc((course?.outcomes || '').split('\n')[0] || 'Set a goal for this meeting…');
    const elapsed = () => {
        const s = call ? Math.floor((Date.now() - call.started) / 1000) : 0;
        return String(Math.floor(s / 60)).padStart(2, '0') + ':' + String(s % 60).padStart(2, '0');
    };
    // How far into the 30-minute timeline strip the call is.
    const stripProgress = () => Math.min(1, Math.max(0.015, (Date.now() - call.started) / 1800000));
    const videoOn = (requireLive = false) => !!stream?.getVideoTracks().some(t => t.enabled && (!requireLive || t.readyState === 'live'));
    const micOn = () => !!stream?.getAudioTracks().some(t => t.enabled);

    // ---- Devices -----------------------------------------------------------------------------------------
    function release() {
        request++;
        stream?.getTracks().forEach(t => t.stop());
        stream = null;
    }

    async function media(video, audio) {
        if (!call) return;
        const id = ++request, owner = call;
        if (!net) owner.status = 'Requesting device access…';
        paint();
        try {
            if (!navigator.mediaDevices?.getUserMedia) throw Error('unavailable');
            const next = await navigator.mediaDevices.getUserMedia({ video, audio });
            if (id !== request || call !== owner) {
                next.getTracks().forEach(t => t.stop());
                return;
            }
            stream?.getTracks().forEach(t => t.stop());
            stream = next;
            if (!owner.demo) owner.video = video;
            if (net?.pc) next.getTracks().forEach(t => net.pc.getSenders().find(x => x.track === null || x.track?.kind === t.kind)?.replaceTrack(t).catch(() => {}));
            if (!net) owner.status = 'Local preview ready · no remote connection';
        } catch {
            if (id !== request || call !== owner) return;
            owner.status = net
                ? 'Camera or microphone unavailable here (browsers need HTTPS or localhost for them). You can still see and hear the other person.'
                : 'Device access unavailable. Check browser permissions or continue without a device.';
        }
        paint();
    }

    function attachVideo() {
        const video = document.querySelector('#skill-call-video');
        if (!video) return;
        video.srcObject = stream;
        const on = videoOn(true);
        video.hidden = !on;
        video.closest('.call-self, .room-self')?.classList.toggle('is-live', on);
        video.play()?.catch(() => {});
    }

    function attachRemote() {
        const video = document.querySelector('#skill-call-remote');
        if (!video) return;
        if (video.srcObject !== remote) video.srcObject = remote;
        const seen = !!remote?.getVideoTracks().some(t => t.readyState === 'live' && !t.muted);
        video.hidden = !seen;
        video.closest('.cd-stage, .room-stage')?.classList.toggle('has-remote', seen);
        video.play()?.catch(() => {});
    }

    // ---- Connection to the other person --------------------------------------------------------------------
    const uid = () => Math.random().toString(36).slice(2, 10) + Date.now().toString(36);

    async function post(op, body) {
        const r = await fetch(ctx.api + op, {
            method: 'POST', credentials: 'same-origin', keepalive: true,
            headers: { 'Content-Type': 'application/json', 'X-SkillUp-Request': '1' }, body: JSON.stringify(body || {})
        });
        if (!r.ok) throw Error(String(r.status));
        return r.json();
    }

    function signal(type, data, to = net?.peer, id = net?.id) {
        if (!to) return Promise.resolve();
        return post('send', { to, type, call: id, data, video: !!call?.video }).catch(() => {});
    }

    function say(text) {
        if (!call) return;
        call.status = text;
        paint();
    }

    function connection() {
        const owner = net, pc = new RTCPeerConnection({ iceServers: [] });
        owner.pc = pc;
        pc.onicecandidate = e => { if (e.candidate && net === owner) signal('ice', e.candidate.toJSON()); };
        pc.ontrack = e => {
            remote = remote || new MediaStream();
            remote.addTrack(e.track);
            e.track.onunmute = e.track.onmute = e.track.onended = attachRemote;
            attachRemote();
        };
        pc.onconnectionstatechange = () => {
            if (net !== owner || !call) return;
            if (pc.connectionState === 'connected') {
                if (!call.linked) call.started = Date.now();
                call.linked = true;
                say('Connected');
            } else if (pc.connectionState === 'failed') say('Could not connect. Both devices need to be on the same network.');
            else if (pc.connectionState === 'disconnected') say('Connection lost…');
        };
        return pc;
    }

    async function addLocal(pc, offering) {
        await call.mediaPromise;
        if (stream) stream.getTracks().forEach(t => pc.addTrack(t, stream));
        if (!offering) return;
        // A device without a microphone or camera can still hear and see the other person.
        if (!stream?.getAudioTracks().length) pc.addTransceiver('audio', { direction: 'recvonly' });
        if (call.video && !stream?.getVideoTracks().length) pc.addTransceiver('video', { direction: 'recvonly' });
    }

    async function flushIce(n) {
        const waiting = n.queue.splice(0);
        for (const c of waiting) await n.pc.addIceCandidate(c).catch(() => {});
    }

    async function sendOffer() {
        const owner = net, pc = connection();
        await addLocal(pc, true);
        if (net !== owner) return;
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        signal('offer', { type: offer.type, sdp: offer.sdp });
    }

    async function sendAnswer(offer) {
        const owner = net, pc = owner.pc || connection();
        await pc.setRemoteDescription(offer);
        await flushIce(owner);
        await addLocal(pc, false);
        if (net !== owner) return;
        const answer = await pc.createAnswer();
        await pc.setLocalDescription(answer);
        signal('answer', { type: answer.type, sdp: answer.sdp });
    }

    function ended(text) {
        if (!call) return;
        const owner = call;
        say(text);
        setTimeout(() => { if (call === owner) stop(true); }, 2500);
    }

    async function receive(m) {
        if (m.type === 'invite') {
            if (call || incoming) {
                signal('decline', null, m.from, m.call);
                return;
            }
            showIncoming({ peer: m.from, callId: m.call, video: !!m.video, name: m.name, role: m.role, title: (m.video ? 'Video call with ' : 'Voice call with ') + m.name });
            return;
        }
        if (incoming && incoming.callId === m.call && m.type === 'hangup') {
            dismissIncoming();
            return;
        }
        if (!call || !net || net.id !== m.call || net.peer !== m.from) return;
        try {
            if (m.type === 'accept') {
                clearTimeout(net.ring);
                say('Connecting…');
                await sendOffer();
            } else if (m.type === 'offer') await sendAnswer(m.data);
            else if (m.type === 'answer') {
                await net.pc.setRemoteDescription(m.data);
                await flushIce(net);
            } else if (m.type === 'ice') {
                if (net.pc?.remoteDescription) await net.pc.addIceCandidate(m.data).catch(() => {});
                else net.queue.push(m.data);
            } else if (m.type === 'decline') ended(call.name + ' declined the call.');
            else if (m.type === 'chat') {
                call.chat.push({ from: 'peer', text: String(m.data || '').slice(0, 1000), at: Date.now() });
                paint();
            } else if (m.type === 'hangup') ended(call.name + ' ended the call.');
        } catch {
            say('Could not set up the call. Please try again.');
        }
    }

    async function poll() {
        let wait = 2500;
        try {
            const { messages } = await post('poll');
            for (const m of messages) await receive(m);
            wait = call || incoming ? 500 : 2000;
        } catch {
            wait = 6000;
        }
        setTimeout(poll, wait);
    }

    // ---- Incoming call card (real or simulated) -----------------------------------------------------------
    function dismissIncoming() {
        incoming = null;
        document.getElementById('skill-incoming-call')?.remove();
        document.querySelectorAll('[data-call-test]').forEach(b => { b.disabled = !!call; });
    }

    function simulateIncoming(kind) {
        const video = kind !== 'voice', peer = ctx.peer;
        if (!peer || call || incoming) return;
        showIncoming({ demo: true, video, name: peer.name, role: peer.role, title: (video ? 'Demo meeting with ' : 'Demo call with ') + peer.name });
    }

    function showIncoming(info) {
        incoming = info;
        const video = info.video, peer = info, demo = !!info.demo;
        const card = document.createElement('aside');
        card.id = 'skill-incoming-call';
        card.setAttribute('role', 'alertdialog');
        card.setAttribute('aria-modal', 'false');
        card.setAttribute('aria-labelledby', 'incoming-call-title');
        card.setAttribute('aria-describedby', 'incoming-call-description');
        card.className = 'incoming-v2';
        const dots = '<svg class="book-dots" viewBox="0 0 20 20" aria-hidden="true">'
            + [[6, 3], [9, 6], [12, 9], [15, 10], [12, 11], [9, 14], [6, 17]].map(([x, y], n) => `<circle cx="${x}" cy="${y}" r="1.4" style="--n:${n}"/>`).join('')
            + '</svg>';
        card.innerHTML =
            '<div class="incoming-top">'
            + `<span class="incoming-avatar">${demo ? `<img src="${demoCaller}" alt="">` : `<b>${initials(peer.name)}</b>`}<i></i><i></i></span>`
            + '<div class="incoming-copy">'
            + `<span class="incoming-demo-label">${demo ? 'Demo · ' : ''}Incoming ${video ? 'video' : 'voice'} call</span>`
            + `<h2 id="incoming-call-title">${esc(peer.name)}</h2>`
            + `<p id="incoming-call-description">${esc(peer.role)} · ${video ? 'Video meeting invitation' : 'Voice call'}</p>`
            + '</div>'
            + '<button type="button" class="incoming-decline" data-incoming-call="decline">Decline</button>'
            + '</div>'
            + '<button type="button" class="book-call" data-incoming-call="accept">'
            + `<span class="book-knob">${dots}</span>`
            + `<span class="book-label">${video ? 'Accept video call' : 'Accept call'}</span>`
            + `<span class="book-phone" aria-hidden="true">${video ? icons.video : icons.phone}</span>`
            + '</button>'
            + (demo ? '<small>Simulated invitation for testing</small>' : '');
        document.body.append(card);
        document.querySelectorAll('[data-call-test]').forEach(b => { b.disabled = true; });
        card.querySelector('[data-incoming-call=accept]').focus();
    }

    // ---- Header timeline strip -----------------------------------------------------------------------------
    function face(name, photo) {
        return '<span class="gn-face">' + (photo ? `<img src="${esc(photo)}" alt="">` : initials(name)) + '</span>';
    }

    function strip() {
        const at = minutes => new Date(call.started + minutes * 60000).toLocaleTimeString('en-GB', { hour: 'numeric', minute: '2-digit', hour12: true, timeZone: 'Asia/Kuala_Lumpur' });
        const portrait = call.demo ? demoCaller : '';
        const pair = face(call.name, portrait) + face(me.name, call.demo ? demoYou : me.avatar);
        return `<button type="button" class="gn-timeline-open cs-bar" data-call-control="show" aria-label="Show active ${call.demo ? 'demo ' : ''}call" style="--p:${stripProgress()}">`
            + `<span class="cs-who">${face(call.name, portrait)}<span class="cs-who-copy"><b>${esc(call.name)}</b><small><i></i>Live · ${call.video ? 'Video' : 'Voice'}</small></span></span>`
            + `<span class="cs-time cs-start">${at(0)}</span>`
            + `<span class="cs-rail"><span class="cs-fill"></span><span class="gn-playhead cs-head"><time>${elapsed()}</time></span></span>`
            + `<span class="cs-time cs-end">${at(30)}</span>`
            + `<span class="gn-attendees">${pair}</span>`
            + `<span class="cs-open">${icons.arrow}</span>`
            + '</button>';
    }

    function updateStrip() {
        document.querySelectorAll('[data-call-test]').forEach(b => {
            b.disabled = !!call || !!incoming;
            b.hidden = !SHOW_INCOMING_CALL_TEST || !ctx.peer;
        });
        document.querySelectorAll('.gn-capsule').forEach(n => n.classList.toggle('is-calling', !!call));
        document.querySelectorAll('.gn-workspace-label').forEach(n => {
            if (!n.dataset.label) n.dataset.label = n.textContent;
            n.textContent = call ? 'Your Schedule' : n.dataset.label;
        });
        document.querySelectorAll('.gn-call-strip').forEach(n => { n.innerHTML = call ? strip() : ''; });
    }

    // ---- Call window pieces ---------------------------------------------------------------------------------
    function paper(kind) {
        const lines = kind === 'outline' ? lessons().slice(0, 4) : ['Meeting objective', 'Discussion points', 'Next learning activity', 'Follow-up notes'];
        return '<span class="call-paper">'
            + `<b>${kind === 'outline' ? 'COURSE OUTLINE' : 'MEETING NOTES'}</b><span class="paper-rule"></span>`
            + `<strong>${esc(course?.title || 'Skill UP meeting')}</strong>`
            + lines.map(t => `<span class="paper-copy">${esc(t)}</span><i></i><i></i>`).join('')
            + '</span>';
    }

    function openDocument(kind) {
        if (!call) return;
        document.getElementById('skill-call-document')?.remove();
        const outline = kind === 'outline';
        const dialog = document.createElement('dialog');
        dialog.id = 'skill-call-document';
        dialog.setAttribute('aria-label', outline ? 'Course outline' : 'Meeting notes');
        const content = outline
            ? `<p>${esc(course?.description || 'No course selected for this call.')}</p><ol>${lessons().map(l => `<li>${esc(l)}</li>`).join('')}</ol>`
            : `<p>${esc(call.notes || 'No notes yet. Add your goal or discussion notes in the call panel.')}</p>`;
        dialog.innerHTML =
            `<header><strong>${outline ? 'Course outline' : 'Meeting notes'}</strong><button type="button" data-call-document-close aria-label="Close document">×</button></header>`
            + `<article><small>SKILL UP · ${call.demo ? 'DEMO MEETING' : 'LOCAL PREVIEW'}</small><h2>${esc(course?.title || call.title)}</h2>${content}</article>`;
        document.body.append(dialog);
        dialog.showModal();
    }

    function selfTile(cls, video) {
        return `<span class="${cls}${video ? ' is-live' : ''}">`
            + `<video id="skill-call-video" autoplay muted playsinline${video ? '' : ' hidden'}></video>`
            + (call.demo ? `<img src="${demoYou}" alt="">` : `<b>${initials(me.name)}</b>`)
            + `<small>You${video ? '' : ' · camera off'}</small>`
            + '</span>';
    }

    function controls(cls, video, mic) {
        return `<div class="call-controls ${cls}">`
            + `<button type="button" data-call-control="mic" aria-label="${mic ? 'Mute microphone' : 'Enable microphone'}" aria-pressed="${mic}">${icons.mic}</button>`
            + `<button type="button" data-call-control="video" aria-label="${video ? 'Turn camera off' : 'Turn camera on'}" aria-pressed="${video}">${icons.video}</button>`
            + `<button type="button" class="call-device-menu" data-call-control="device" aria-label="Device information">${icons.gear}</button>`
            + `<button type="button" class="call-end" data-call-control="end" aria-label="End call">${icons.end}</button>`
            + '</div>';
    }

    // The expanded meeting room: stage, people, insights and chat.
    function room(video, mic) {
        const pct = course?.progress || 0;
        const time = at => new Date(at).toLocaleTimeString('en-GB', { hour: 'numeric', minute: '2-digit', hour12: true });
        const tile = (name, sub, photo, cls = '') => `<div class="room-tile ${cls}">`
            + (photo ? `<img src="${photo}" alt="">` : `<span>${initials(name)}</span>`)
            + `<b>${esc(name)}</b><small>${esc(sub)}</small></div>`;
        const messages = call.chat.map(m => {
            const mine = m.from === 'me';
            const avatar = mine ? '' : '<span class="room-msg-face">' + (call.demo ? `<img src="${demoCaller}" alt="">` : initials(call.name)) + '</span>';
            return `<li class="${mine ? 'is-me' : ''}">${avatar}<div><header><b>${esc(mine ? 'You' : call.name)}</b><time>${time(m.at)}</time></header><p>${esc(m.text)}</p></div></li>`;
        }).join('') || '<li class="room-empty">No messages yet. Say hello.</li>';
        const attendees = call.demo
            ? `<img src="${demoCaller}" alt=""><img src="${demoYou}" alt="">`
            : `<span>${initials(call.name)}</span><span>${initials(me.name)}</span>`;
        const quickReplies = ['Sounds good', 'Can you share the outline?', 'One moment please'];

        const head = '<header class="room-head">'
            + `<div><h2>Hello, ${esc(firstName(me.name))}!</h2><p>${esc(call.title)} <span>|</span> <b>Live now</b></p></div>`
            + `<div class="room-attendees" aria-label="3 participants">${attendees}<span class="is-bot">SU</span><small>3 in call</small></div>`
            + '<div class="room-head-tools">'
            + `<span class="room-timer"><i></i><time data-call-time>${elapsed()}</time></span>`
            + `<button type="button" data-call-control="expand" aria-label="Shrink call">${icons.shrink}</button>`
            + `<button type="button" data-call-control="minimize" aria-label="Minimize call">${icons.min}</button>`
            + '</div></header>';
        const stage = `<div class="room-stage"${call.demo ? ` style="--stage-img:url(${demoCaller})"` : ''}>`
            + (call.demo ? demoPhoto : `<div class="room-stage-empty"><span>${initials(call.name)}</span><b>${esc(call.name)}</b><small>${net ? esc(call.status) : 'No remote participant connected'}</small></div>`)
            + (call.demo ? '' : '<video id="skill-call-remote" class="call-remote" autoplay playsinline hidden></video>')
            + selfTile('room-self', video)
            + '<span class="room-live">Live <i></i></span><span class="room-level" aria-hidden="true"><i></i></span>'
            + controls('room-controls', video, mic)
            + '</div>';
        const people = '<div class="room-people">'
            + tile(call.name, call.role || 'Guest', call.demo ? demoCaller : '', 'is-speaking')
            + tile(me.name + ' (you)', mic ? 'Microphone on' : 'Muted', call.demo ? demoYou : '')
            + tile('Skill UP assistant', 'Taking notes', '', 'is-bot')
            + '</div>';
        const insights = '<section class="room-insights">'
            + `<header><h3>Meeting insights</h3><span class="call-status" role="status">${esc(call.status)}</span></header>`
            + `<div class="room-row"><span class="room-row-icon">${icons.doc}</span><div><b>Course outline</b><small>${esc(course?.title || 'No course selected')} · ${lessons().length} activities</small></div>`
            + `<span class="room-progress" style="--p:${pct}"><i></i></span><em>${pct}%</em><button type="button" data-call-doc="outline" aria-label="Open course outline">${icons.arrow}</button></div>`
            + `<div class="room-row"><span class="room-row-icon">${icons.doc}</span><div><b>Meeting notes</b><small>Objective, discussion points and follow-ups</small></div><button type="button" data-call-doc="notes" aria-label="Open meeting notes">${icons.arrow}</button></div>`
            + `<label class="room-row room-goal"><span class="room-row-icon">${icons.target}</span><div><b>Goal</b><textarea id="skill-call-notes" rows="2" placeholder="${goalPlaceholder()}">${esc(call.notes)}</textarea></div></label>`
            + '</section>';
        const chat = '<aside class="room-chat" aria-label="Meeting chat">'
            + `<header><h3>Chat room</h3><span>${call.chat.length} messages</span></header>`
            + `<ol class="room-messages"><li class="room-day"><span>Today</span></li>${messages}</ol>`
            + '<div class="room-quick" aria-label="Quick replies">' + quickReplies.map(q => `<button type="button" data-call-quick="${esc(q)}">${esc(q)}</button>`).join('') + '</div>'
            + '<form class="room-compose" data-call-chat><label class="sr-only" for="room-message">Message</label><input id="room-message" name="msg" autocomplete="off" placeholder="Type your message…">'
            + `<button type="submit" aria-label="Send message to meeting">${icons.send}</button></form>`
            + `<small class="call-local-note">${call.demo ? 'Illustrative demo · ' : ''}${net ? 'Messages in this call go to the other person only.' : 'Messages stay on this device.'}</small>`
            + '</aside>';
        return `<div class="room"><div class="room-main">${head}${stage}${people}${insights}</div>${chat}</div>`;
    }

    // The compact call window: picture with controls, and a summary card with documents and the goal.
    function compact(video, mic) {
        const picture = '<div class="cd-stage call-picture">'
            + (call.demo ? demoPhoto : `<div class="cd-waiting"><span>${initials(call.name)}</span><b>${esc(call.name)}</b><small>${net ? esc(call.status) : 'Waiting to connect · local preview'}</small></div>`)
            + (call.demo ? '' : '<video id="skill-call-remote" class="call-remote" autoplay playsinline hidden></video>')
            + '<div class="cd-top">'
            + `<span class="cd-live"><i></i><time data-call-time>${elapsed()}</time></span>`
            + `<span class="cd-who"><b>${esc(call.name)}</b><small>${esc(call.role || 'Guest')}${call.demo ? ' · demo' : ''}</small></span>`
            + '<span class="call-picture-tools">'
            + `<button type="button" data-call-control="expand" aria-label="Expand call">${icons.expand}</button>`
            + `<button type="button" data-call-control="minimize" aria-label="Minimize call">${icons.min}</button>`
            + '</span></div>'
            + selfTile('call-self cd-self', video)
            + controls('cd-controls', video, mic)
            + '</div>';
        const summary = '<section class="call-summary cd-summary">'
            + `<header><h2>Summary</h2><span class="cd-sub">${esc(course?.title || call.title)}</span>`
            + `<button type="button" data-call-control="summary" aria-label="${call.summaryClosed ? 'Show' : 'Hide'} summary" aria-expanded="${!call.summaryClosed}">${icons.arrow}</button></header>`
            + '<div class="cd-summary-body">'
            + '<section class="call-documents cd-docs">'
            + `<button type="button" data-call-doc="outline" aria-label="Open course outline">${paper('outline')}<span><b>Course outline</b><small>${lessons().length} activities</small></span></button>`
            + `<button type="button" data-call-doc="notes" aria-label="Open meeting notes">${paper('notes')}<span><b>Meeting notes</b><small>Objective · follow-ups</small></span></button>`
            + '</section>'
            + '<label class="call-goal cd-goal">'
            + `<span class="cd-goal-head">${icons.target}<b>Goal</b><button type="button" data-call-control="goal" aria-label="Edit meeting goal">${icons.pencil}</button></span>`
            + `<textarea id="skill-call-notes" rows="3" placeholder="${goalPlaceholder()}">${esc(call.notes)}</textarea>`
            + '</label>'
            + `<p class="call-status" role="status">${esc(call.status)}</p>`
            + `<small class="call-local-note">${net ? 'Audio and video go directly between the two browsers on this network.' : (call.demo ? 'Illustrative demo portrait · ' : '') + 'Device preview only; audio and video are not transmitted.'}</small>`
            + '</div></section>';
        return picture + summary;
    }

    // Draws the call window (compact or expanded) for the current state.
    function paint() {
        if (!call) return;
        let dock = document.getElementById('skill-call-dock');
        if (!dock) {
            dock = document.createElement('aside');
            dock.id = 'skill-call-dock';
            dock.setAttribute('aria-label', 'Local call preview');
            document.body.append(dock);
        }
        dock.hidden = !!call.minimized;
        dock.dataset.summary = call.summaryClosed ? 'closed' : 'open';
        const video = videoOn(), mic = micOn();
        if (call.expanded) {
            // keep the chat draft and focus across a redraw
            const hadFocus = document.activeElement?.id === 'room-message';
            const draft = document.getElementById('room-message')?.value || '';
            dock.className = 'is-room' + (call.demo ? ' is-demo' : '');
            dock.innerHTML = room(video, mic);
            attachVideo();
            attachRemote();
            document.body.classList.add('has-local-call');
            updateStrip();
            const list = dock.querySelector('.room-messages');
            if (list) list.scrollTop = list.scrollHeight;
            const field = dock.querySelector('.room-compose input');
            if (field && hadFocus && !call.focusChat) field.value = draft;
            if (field && (call.focusChat || hadFocus)) field.focus();
            call.focusChat = false;
            return;
        }
        dock.className = 'cd' + (call.demo ? ' is-demo' : '');
        dock.innerHTML = compact(video, mic);
        attachVideo();
        attachRemote();
        document.body.classList.add('has-local-call');
        updateStrip();
    }

    // ---- Start / stop ---------------------------------------------------------------------------------------
    function start(options) {
        stop(true);
        const now = Date.now();
        net = ctx.api && me.id && options.peer ? { peer: Number(options.peer), id: options.callId || uid(), callee: !!options.callId, pc: null, queue: [], ring: 0 } : null;
        call = {
            name: options.name || 'Live session',
            title: options.title || 'Private conversation',
            role: options.role || '',
            video: !!options.video,
            demo: !!options.demo,
            notes: '',
            chat: options.demo ? [
                { from: 'peer', text: 'Hi ' + firstName(me.name) + '! Can you hear me okay?', at: now },
                { from: 'peer', text: 'I added the course outline to Documents so we can go through it.', at: now },
            ] : [],
            started: now,
            status: !net ? 'Local preview · no remote connection' : net.callee ? 'Connecting…' : 'Calling ' + (options.name || 'them') + '…',
        };
        paint();
        timer = setInterval(() => {
            if (!call) return;
            document.querySelectorAll('[data-call-time], .gn-call-strip time').forEach(t => { t.textContent = elapsed(); });
            document.querySelectorAll('.cs-bar').forEach(n => n.style.setProperty('--p', stripProgress()));
        }, 1000);
        call.mediaPromise = media(!!options.video, true);
        if (!net) return;
        if (net.callee) signal('accept');
        else {
            signal('invite');
            const owner = net;
            net.ring = setTimeout(() => { if (net === owner && !owner.pc) { signal('hangup'); ended('No answer.'); } }, 45000);
        }
    }

    // quiet: the other side already knows (it hung up, declined, or the call never connected)
    function stop(quiet) {
        if (net) {
            clearTimeout(net.ring);
            if (!quiet) signal('hangup');
            net.pc?.close();
            net = null;
        }
        remote = null;
        call = null;
        dismissIncoming();
        release();
        clearInterval(timer);
        timer = null;
        document.getElementById('skill-call-dock')?.remove();
        document.getElementById('skill-call-document')?.remove();
        document.body.classList.remove('has-local-call');
        updateStrip();
    }

    // ---- Events ------------------------------------------------------------------------------------------------
    // Start a call, and the call window's buttons.
    document.addEventListener('click', e => {
        const begin = e.target.closest('[data-start-call]');
        if (begin) {
            const name = begin.dataset.callName || 'Live session';
            start({ name, peer: begin.dataset.callPeer, role: begin.dataset.callRole, title: begin.dataset.callTitle || 'Conversation with ' + name, video: begin.dataset.startCall === 'video' });
            return;
        }
        const button = e.target.closest('[data-call-control]');
        if (!button || !call) return;
        switch (button.dataset.callControl) {
            case 'goal': document.getElementById('skill-call-notes')?.focus(); return;
            case 'summary': call.summaryClosed = !call.summaryClosed; paint(); return;
            case 'device': if (!net) call.status = stream ? 'Browser devices active · no remote connection' : 'No device enabled · local preview only'; paint(); return;
            case 'end': stop(); return;
            case 'minimize': call.minimized = true; paint(); return;
            case 'show': call.minimized = false; paint(); return;
            case 'expand': call.expanded = !call.expanded; paint(); return;
            case 'video': {
                const track = stream?.getVideoTracks()[0];
                if (track) {
                    track.enabled = !track.enabled;
                    call.video = track.enabled;
                    paint();
                } else media(true, micOn());
                return;
            }
            case 'mic': {
                const track = stream?.getAudioTracks()[0];
                if (track) {
                    track.enabled = !track.enabled;
                    paint();
                } else media(videoOn(), true);
            }
        }
    });

    // The header's test buttons and the incoming call card.
    document.addEventListener('click', e => {
        const test = e.target.closest('[data-call-test]');
        if (test) {
            simulateIncoming(test.dataset.callTest || 'video');
            return;
        }
        const button = e.target.closest('[data-incoming-call]');
        if (!button || !incoming) return;
        const options = { ...incoming };
        dismissIncoming();
        if (button.dataset.incomingCall === 'accept') start(options);
        else {
            if (options.peer) signal('decline', null, options.peer, options.callId);
            document.querySelector('[data-call-test]')?.focus();
        }
    });

    // Course outline / meeting notes documents.
    document.addEventListener('click', e => {
        const doc = e.target.closest('[data-call-doc]');
        if (doc) openDocument(doc.dataset.callDoc);
        if (e.target.closest('[data-call-document-close]')) document.getElementById('skill-call-document')?.close();
    });

    // Meeting chat: quick replies, sending (the demo caller answers), and the goal notes.
    document.addEventListener('click', e => {
        const quick = e.target.closest('[data-call-quick]');
        if (!quick || !call) return;
        const input = document.getElementById('room-message');
        if (input) {
            input.value = quick.dataset.callQuick;
            input.closest('form').requestSubmit();
        }
    });
    document.addEventListener('submit', e => {
        const form = e.target.closest('[data-call-chat]');
        if (!form || !call) return;
        e.preventDefault();
        e.stopPropagation();
        const input = form.querySelector('input'), text = input.value.trim();
        if (!text) return;
        call.chat.push({ from: 'me', text, at: Date.now() });
        if (net) signal('chat', text);
        call.focusChat = true;
        paint();
        if (call.demo) {
            const owner = call;
            setTimeout(() => {
                if (call !== owner) return;
                call.chat.push({ from: 'peer', text: 'Got it. Let’s go through that together.', at: Date.now() });
                paint();
            }, 1400);
        }
    }, true);
    document.addEventListener('input', e => {
        if (e.target.id === 'skill-call-notes' && call) call.notes = e.target.value;
    });

    addEventListener('pagehide', () => {
        if (incoming?.peer) signal('decline', null, incoming.peer, incoming.callId);
        stop();
    });
    updateStrip();
    if (ctx.api && me.id) poll();
})();

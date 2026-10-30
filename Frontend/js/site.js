/* Small progressive enhancements for the server-rendered pages. Every page works without this file. */
(function () {
    'use strict';

    const body = document.body;
    const reducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;
    const scrollBehavior = () => (reducedMotion() ? 'auto' : 'smooth');
    const isVisitorHome = body.classList.contains('smooth-scroll') && body.classList.contains('ui-public');
    const scrollTopButton = document.querySelector('[data-scroll-top]');

    function clearHash() {
        if (location.hash) history.replaceState(null, '', location.pathname + location.search);
    }

    // ---- Every page --------------------------------------------------------------------------------

    // The save notice fades after a few seconds.
    const toast = document.getElementById('toast');
    if (toast && toast.classList.contains('visible')) setTimeout(() => toast.classList.remove('visible'), 5000);

    // Buttons with data-confirm ask before they post back (deletes, withdrawals).
    document.addEventListener('click', e => {
        const button = e.target.closest('[data-confirm]');
        if (button && !confirm(button.dataset.confirm)) e.preventDefault();
    });

    // Validator controls re-check a field when it loses focus. Clicking Send blurs the field, its message disappears,
    // the button moves up mid-click and the click misses, so fields are checked on submit only (as before).
    if (window.Page_Validators) {
        for (const validator of window.Page_Validators) {
            const field = document.getElementById(validator.controltovalidate);
            if (field && String(field.onchange).includes('ValidatorOnChange')) field.onchange = null;
        }
    }

    // Show or hide a password field.
    document.addEventListener('click', e => {
        const eye = e.target.closest('[data-auth-eye]');
        if (!eye) return;
        const input = eye.parentElement.querySelector('input');
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        eye.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
    });

    // Escape closes any open disclosure menu, and the open Log in / Sign up pop-up (as its close button would).
    document.addEventListener('keydown', e => {
        if (e.key !== 'Escape') return;
        document.querySelectorAll('details[open].gn-more, details[open][data-menu], details[open].ld-monitor-filter, details[open].ld-monitor-actions')
            .forEach(menu => { menu.open = false; });
        document.querySelector('.auth-popup:is(:target, .is-open) .dialog-close')?.click();
    });

    // Back to top button: appears once the page is scrolled down a screen, scrolls up smoothly (instantly with reduced motion).
    if (scrollTopButton) {
        const sync = () => scrollTopButton.classList.toggle('is-visible', scrollY > Math.max(400, innerHeight / 2));
        addEventListener('scroll', sync, { passive: true });
        sync();
        scrollTopButton.addEventListener('click', () => {
            scrollTo({ top: 0, behavior: scrollBehavior() });
            clearHash();
        });
    }

    // ---- Landing page (Home) -------------------------------------------------------------------------

    // Section entrance animation.
    const reveal = document.querySelectorAll('.home-reveal');
    if (reveal.length && 'IntersectionObserver' in window && !reducedMotion()) {
        const observer = new IntersectionObserver(entries => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue;
                entry.target.classList.add('home-reveal-active');
                observer.unobserve(entry.target);
            }
        }, { threshold: 0.06 });
        reveal.forEach(item => observer.observe(item));
    } else {
        reveal.forEach(item => item.classList.add('home-reveal-active'));
    }

    // Instructor carousel: the arrows scroll the track by one card.
    document.addEventListener('click', e => {
        const arrow = e.target.closest('[data-instructor-step]');
        if (!arrow) return;
        const track = arrow.parentElement.querySelector('.instructor-track');
        const card = track && track.querySelector('.instructor-card');
        if (card) track.scrollBy({ left: Number(arrow.dataset.instructorStep) * (card.offsetWidth + 20), behavior: 'smooth' });
    });

    // Tapping an instructor card shows their short bio.
    document.addEventListener('click', e => {
        const card = e.target.closest('.instructor-card');
        if (!card) return;
        const open = !card.classList.contains('is-open');
        card.classList.toggle('is-open', open);
        card.setAttribute('aria-expanded', String(open));
    });

    if (isVisitorHome) {
        // A browser refresh starts again at the top of Home (no #section in the address, no remembered scroll position),
        // wherever on the page it was pressed. Links to a section still open it, since those are not refreshes.
        history.scrollRestoration = 'manual';
        const navigation = performance.getEntriesByType('navigation')[0];
        if (navigation && navigation.type === 'reload') {
            clearHash();
            const toTop = () => scrollTo({ top: 0, behavior: 'instant' });
            toTop();
            addEventListener('load', toTop);
        }

        // The section tab you clicked stays highlighted (also when a link from another page opened that section)
        // until you press the back-to-top button, which highlights About again. Scrolling by hand changes nothing.
        const tabs = [...document.querySelectorAll('.gn-links a')];
        const tabFor = hash => tabs.find(a => (a.getAttribute('href') || '').split('#')[1] === hash);
        const mark = tab => {
            if (!tab) return;
            tabs.forEach(a => a.removeAttribute('aria-current'));
            tab.setAttribute('aria-current', 'page');
        };
        mark(tabFor(location.hash.slice(1)));
        tabs.forEach(a => a.addEventListener('click', () => {
            if ((a.getAttribute('href') || '').includes('#')) mark(a);
        }));
        scrollTopButton?.addEventListener('click', () => mark(tabFor('top')));
    }

    // ---- Workspace pages --------------------------------------------------------------------------------

    // Buttons that jump to a section of the page (learner dashboard).
    document.addEventListener('click', e => {
        const button = e.target.closest('[data-vd-scroll]');
        if (button) document.getElementById(button.dataset.vdScroll)?.scrollIntoView({ behavior: scrollBehavior(), block: 'start' });
    });

    // Messages: show the newest message, Enter sends (Shift + Enter adds a line), Ctrl + Enter posts a comment.
    const thread = document.getElementById('conversation-messages');
    if (thread) thread.scrollTop = thread.scrollHeight;
    document.addEventListener('keydown', e => {
        const box = e.target;
        if (box.id === 'MessageBox' && e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            if (box.value.trim()) box.closest('.sc-composer').querySelector('.sc-send').click();
        }
        if (box.id === 'CommentBox' && e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
            e.preventDefault();
            if (box.value.trim()) box.closest('.sc-comment-box').querySelector('.sc-comment-post').click();
        }
    });

    // Messages: pick up new messages from the other person every few seconds, without a refresh. The page is fetched in the
    // background and only the conversation and the contact list are swapped, so a half-typed message is never touched.
    const liveThread = document.querySelector('#conversation-messages .sc-thread');
    if (liveThread) {
        const contactList = document.querySelector('.sc-contact-scroll'), unreadBadge = document.querySelector('.sc-contacts-head');
        let checking = false;
        // force: after sending, wait for any check already under way, then look now (even in a hidden tab) and show the newest message.
        const check = async force => {
            if (force === true) while (checking) await new Promise(done => setTimeout(done, 40));
            else if (checking || document.hidden) return;
            checking = true;
            try {
                const r = await fetch(location.href, { credentials: 'same-origin', headers: { 'X-SkillUp-Live': '1' } });
                if (!r.ok) return;
                const page = new DOMParser().parseFromString(await r.text(), 'text/html');
                const next = page.querySelector('#conversation-messages .sc-thread');
                if (!next) return;
                if (next.innerHTML !== liveThread.innerHTML) {
                    const box = liveThread.parentElement, atEnd = force === true || box.scrollHeight - box.scrollTop - box.clientHeight < 90;
                    liveThread.innerHTML = next.innerHTML;
                    if (atEnd) box.scrollTop = box.scrollHeight;
                }
                const list = page.querySelector('.sc-contact-scroll'), head = page.querySelector('.sc-contacts-head');
                if (list && contactList && list.innerHTML !== contactList.innerHTML) contactList.innerHTML = list.innerHTML;
                if (head && unreadBadge && head.innerHTML !== unreadBadge.innerHTML) unreadBadge.innerHTML = head.innerHTML;
            } catch { /* offline for a moment: try again next time */ } finally { checking = false; }
        };
        setInterval(check, 3000);
        document.addEventListener('visibilitychange', () => check());

        // Sending: post the message in the background and update the conversation in place, so the page never reloads.
        // If that fails for any reason, the button falls back to its normal form post, which shows the server's message.
        const chat = document.querySelector('.skill-chat');
        let sending = false, plain = false;
        document.addEventListener('click', async e => {
            const send = e.target.closest('.sc-send');
            if (!send || plain || !chat?.dataset.api || !chat.dataset.with) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            const field = document.getElementById('MessageBox'), text = field.value.trim();
            if (!text || sending) return;
            sending = true;
            try {
                const r = await fetch(chat.dataset.api + 'message', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', 'X-SkillUp-Request': '1' },
                    body: JSON.stringify({ to: chat.dataset.with, text })
                });
                if (!r.ok) throw Error(String(r.status));
                field.value = '';
                await check(true);
                field.focus();
            } catch {
                plain = true;
                send.click();
            } finally { sending = false; }
        }, true);
    }

    // Print buttons, and the leader overview's "more updates" arrow scrolls the alert list.
    document.addEventListener('click', e => {
        if (e.target.closest('[data-action=print]')) print();
        const more = e.target.closest('[data-leader-note-scroll]');
        if (more) {
            const list = more.closest('.ld-notifications')?.querySelector('.ld-note-scroll');
            list?.scrollBy({ top: Math.max(80, list.clientHeight * 0.75) });
        }
    });

    // Analytics figures count up from zero once.
    if (!reducedMotion()) {
        document.querySelectorAll('.la-page [data-count], .rq-summary [data-count]').forEach((figure, k) => {
            const target = Number(figure.dataset.count) || 0;
            const start = performance.now() + k * 90;
            const step = now => {
                const p = Math.min(1, Math.max(0, (now - start) / 900));
                figure.textContent = Math.round(target * (1 - Math.pow(1 - p, 3)));
                if (p < 1) requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
        });
    }

    // Course requests: tick rows to export only those; the header box ticks every row on the page.
    const requests = document.querySelector('.course-requests-page');
    if (requests) {
        const boxes = () => [...requests.querySelectorAll('.rq-row-check')];
        const all = requests.querySelector('#rq-select-all');
        const count = requests.querySelector('#rq-selection-count');
        const clear = requests.querySelector('#rq-clear-selection');
        const exportButton = document.getElementById('ExportButton');
        const sync = () => {
            const ticked = boxes().filter(b => b.checked).length;
            count.textContent = ticked + ' selected';
            clear.hidden = !ticked;
            if (all) {
                all.checked = ticked > 0 && ticked === boxes().length;
                all.indeterminate = ticked > 0 && ticked < boxes().length;
                all.disabled = !boxes().length;
            }
            if (exportButton && !exportButton.disabled) exportButton.textContent = ticked ? 'Export selected (' + ticked + ')' : 'Export CSV';
        };
        requests.addEventListener('change', e => {
            if (e.target === all) boxes().forEach(b => { b.checked = all.checked; });
            if (e.target === all || e.target.matches('.rq-row-check')) sync();
        });
        clear?.addEventListener('click', () => {
            boxes().forEach(b => { b.checked = false; });
            sync();
        });
        sync();
    }

    // The quiz editor's companion shows its newest reply.
    const aiHistory = document.getElementById('qe-ai-history');
    if (aiHistory) aiHistory.scrollTop = aiHistory.scrollHeight;

    // ---- Last: on narrow screens the nav links scroll sideways; keep the highlighted tab in view -----------
    // (after the landing page has highlighted the tab of the #section it opened at)
    const navLinks = document.querySelector('.gn-links');
    const currentLink = navLinks && navLinks.querySelector('[aria-current=page]');
    if (currentLink && navLinks.scrollWidth > navLinks.clientWidth) {
        navLinks.scrollLeft += currentLink.getBoundingClientRect().left - navLinks.getBoundingClientRect().left
            - (navLinks.clientWidth - currentLink.offsetWidth) / 2;
    }
})();

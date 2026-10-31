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

    // A link to a folded form (such as Profile's "Give feedback") opens it, scrolls to it and puts the cursor in its first field.
    document.addEventListener('click', e => {
        const link = e.target.closest('a[href^="#"]');
        const form = link && link.getAttribute('href').length > 1 ? document.getElementById(link.getAttribute('href').slice(1)) : null;
        if (!form || !form.matches('details.inline-form')) return;
        e.preventDefault();
        form.open = true;
        form.scrollIntoView({ behavior: scrollBehavior(), block: 'center' });
        form.querySelector('textarea, input, select')?.focus({ preventScroll: true });
    });

    // Profile: the side menu highlights the section you chose (also when another link on the page, such as "Account security",
    // takes you there), and the section named in the address is highlighted when the page opens.
    const profileNav = [...document.querySelectorAll('.pf-nav a[href^="#"]')];
    if (profileNav.length) {
        const mark = hash => {
            const tab = profileNav.find(a => a.getAttribute('href') === hash);
            if (tab) profileNav.forEach(a => a.classList.toggle('is-active', a === tab));
        };
        if (location.hash) mark(location.hash);
        document.addEventListener('click', e => {
            const link = e.target.closest('a[href^="#pf-"]');
            if (link) mark(link.getAttribute('href'));
        });
        addEventListener('hashchange', () => mark(location.hash));
    }

    // Buttons that jump to a section of the page (learner dashboard).
    document.addEventListener('click', e => {
        const button = e.target.closest('[data-vd-scroll]');
        if (button) document.getElementById(button.dataset.vdScroll)?.scrollIntoView({ behavior: scrollBehavior(), block: 'start' });
    });

    // Messages: show the newest message, Enter sends (Shift + Enter adds a line), Ctrl + Enter posts a comment.
    const chatRoot = () => document.querySelector('.skill-chat');
    const showNewest = () => {
        const box = document.getElementById('conversation-messages');
        if (box) box.scrollTop = box.scrollHeight;
    };
    showNewest();
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

    // Messages without page reloads: new messages arrive every few seconds, sending happens in the background, and a
    // channel, contact or announcement opens in place. Each fetches the page itself and swaps in the part that changed, so a
    // half-typed message is never touched by a refresh. If anything fails, the link or button works as it always did.
    if (chatRoot()) {
        const live = { credentials: 'same-origin', headers: { 'X-SkillUp-Live': '1' } };
        const parse = async response => new DOMParser().parseFromString(await response.text(), 'text/html');
        let checking = false, switching = false, sending = false, plain = false;

        // force: after sending, wait for any check already under way, then look now (even in a hidden tab) and show the newest message.
        const check = async force => {
            if (force === true) while (checking) await new Promise(done => setTimeout(done, 40));
            else if (checking || switching || document.hidden) return;
            checking = true;
            const url = location.href;
            try {
                const r = await fetch(url, live);
                if (!r.ok) return;
                const page = await parse(r);
                const thread = document.querySelector('#conversation-messages .sc-thread'), next = page.querySelector('#conversation-messages .sc-thread');
                if (switching || url !== location.href || !thread || !next) return;
                if (next.innerHTML !== thread.innerHTML) {
                    const box = thread.parentElement, atEnd = force === true || box.scrollHeight - box.scrollTop - box.clientHeight < 90;
                    thread.innerHTML = next.innerHTML;
                    if (atEnd) box.scrollTop = box.scrollHeight;
                }
                for (const part of ['.sc-contact-scroll', '.sc-contacts-head']) {
                    const here = document.querySelector(part), there = page.querySelector(part);
                    if (here && there && here.innerHTML !== there.innerHTML) here.innerHTML = there.innerHTML;
                }
            } catch { /* offline for a moment: try again next time */ } finally { checking = false; }
        };
        setInterval(check, 3000);
        document.addEventListener('visibilitychange', () => check());

        // Opening another channel, contact or announcement: swap the whole workspace. The page's hidden state fields and the
        // form's address come from the new page too, so Mark as read, Send and the other buttons act on what is on screen.
        const open = async (address, push) => {
            switching = true;
            try {
                const target = new URL(address, location.href);
                const r = await fetch(target.pathname + target.search, live);
                const page = r.ok ? await parse(r) : null;
                const next = page?.querySelector('.skill-chat');
                if (!next || new URL(r.url).pathname !== location.pathname) throw Error('not the messages page');
                page.querySelectorAll('input[type=hidden][name^="__"]').forEach(field => {
                    const same = [...document.querySelectorAll('input[type=hidden]')].find(x => x.name === field.name);
                    if (same) same.value = field.value;
                });
                const form = chatRoot().closest('form');
                if (form) form.setAttribute('action', target.pathname + target.search);
                // The sidebar keeps its scroll position, so choosing someone far down the list does not send it back to the top.
                const sidebarScroll = chatRoot().querySelector('.sc-contacts')?.scrollTop || 0;
                chatRoot().replaceWith(document.importNode(next, true));
                const sidebar = chatRoot().querySelector('.sc-contacts');
                if (sidebar) sidebar.scrollTop = sidebarScroll;
                document.title = page.title || document.title;
                if (push) history.pushState(null, '', target.href);
                if (target.hash && document.getElementById(target.hash.slice(1))) document.getElementById(target.hash.slice(1)).scrollIntoView();
                else showNewest();
            } catch {
                location.href = address;
            } finally { switching = false; }
        };
        document.addEventListener('click', e => {
            if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
            const link = e.target.closest('.skill-chat a[href]');
            if (!link || link.target) return;
            const target = new URL(link.href, location.href);
            if (target.origin !== location.origin || target.pathname !== location.pathname) return;
            e.preventDefault();
            if (target.href !== location.href) open(target.href, true);
        });
        addEventListener('popstate', () => open(location.href, false));

        // Sending: post the message in the background and update the conversation in place, so the page never reloads.
        // If that fails for any reason, the button falls back to its normal form post, which shows the server's message.
        document.addEventListener('click', async e => {
            const send = e.target.closest('.sc-send'), chat = chatRoot();
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
                plain = false;
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

    // My courses: the program pack nearest the middle of the rail lights up as the rail scrolls (CSS: .mc-program.is-lit), and
    // once the rail settles the course panel switches to that program's courses, so scrolling to a pack is choosing it.
    const packRail = document.querySelector('.my-courses .mc-program-scroll');
    if (packRail) {
        const packs = [...packRail.querySelectorAll('.mc-program')];
        const idOf = pack => +new URL(pack.querySelector('.mc-program-link').href).searchParams.get('program');
        let shown = packs.find(pack => pack.dataset.selected === 'true') || packs[0];
        let request = 0, settle = 0;

        const nearestPack = () => {
            const mid = packRail.scrollTop + packRail.clientHeight / 2;
            return packs.reduce((best, pack) =>
                Math.abs(pack.offsetTop + pack.offsetHeight / 2 - mid) < Math.abs(best.offsetTop + best.offsetHeight / 2 - mid) ? pack : best);
        };

        // Swaps in the course panel of another program from a background copy of the page; the rail itself is left alone.
        const showProgram = async pack => {
            const mine = ++request;
            const link = pack.querySelector('.mc-program-link');
            try {
                const reply = await fetch(link.href, { credentials: 'same-origin', headers: { 'X-SkillUp-Live': '1' } });
                if (!reply.ok || mine !== request) return;
                const next = new DOMParser().parseFromString(await reply.text(), 'text/html').querySelector('.mc-courses-panel');
                const panel = document.querySelector('.mc-courses-panel');
                if (!next || !panel) return;
                panel.innerHTML = next.innerHTML;
                packs.forEach(other => {
                    other.dataset.selected = other === pack ? 'true' : 'false';
                    other.querySelector('.mc-program-link').setAttribute('aria-selected', other === pack ? 'true' : 'false');
                });
                shown = pack;
                history.replaceState(null, '', link.href);
            } catch (error) { /* offline: the panel keeps the program it had */ }
        };

        const light = () => {
            const nearest = nearestPack();
            packs.forEach(pack => pack.classList.toggle('is-lit', pack === nearest));
            clearTimeout(settle);
            if (nearest !== shown) settle = setTimeout(() => { if (nearestPack() === nearest) showProgram(nearest); }, 220);
        };
        packRail.addEventListener('scroll', light, { passive: true });
        light();
    }
    // Certificate: write the completion date in the viewing device's own date and time zone, e.g. 06 October 2026.
    document.querySelectorAll('time[data-local-date]').forEach(stamp => {
        const given = stamp.dataset.localDate;
        const date = given && given !== 'today' ? new Date(given) : new Date();
        if (!isNaN(date)) stamp.textContent = date.toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' });
    });
    // Catalog: on a window too short for the full layout, tighten it (CSS: .is-compact) so the first row of cards fits.
    const catalogPage = document.querySelector('.catalog-page');
    if (catalogPage) {
        const fit = () => {
            const zoom = parseFloat(getComputedStyle(document.documentElement).zoom) || 1;
            catalogPage.classList.toggle('is-compact', innerHeight / zoom < 800);
        };
        fit();
        addEventListener('resize', fit);
    }
})();

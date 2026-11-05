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

    // Once the page has finished rising into place, parts swapped in later (a quiz result, new reviews) must not animate again.
    setTimeout(() => document.documentElement.classList.add('is-settled'), 1600);

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
    const quietValidators = () => {
        if (!window.Page_Validators) return;
        for (const validator of window.Page_Validators) {
            const field = document.getElementById(validator.controltovalidate);
            if (field && String(field.onchange).includes('ValidatorOnChange')) field.onchange = null;
        }
    };
    quietValidators();

    // Show or hide a password field.
    document.addEventListener('click', e => {
        const eye = e.target.closest('[data-auth-eye]');
        if (!eye) return;
        const input = eye.parentElement.querySelector('input');
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        eye.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
    });

    // Password rules and strength (Sign up and Reset password). The rules match Backend Passwords.Problem.
    const passwordRules = v => ({ length: v.length >= 8 && v.length <= 128, capital: /[A-Z]/.test(v), number: /[0-9]/.test(v), special: /[^A-Za-z0-9\s]/.test(v) });
    const passwordMet = ok => ok.length && ok.capital && ok.number && ok.special;
    const passwordStrength = v => {
        if (!v) return 0;
        if (!passwordMet(passwordRules(v))) return 1;
        return v.length >= 16 ? 4 : v.length >= 12 ? 3 : 2;
    };
    window.skillUpPasswordCheck = (source, args) => {
        const ok = passwordRules(args.Value || '');
        args.IsValid = passwordMet(ok);
        if (!ok.length) source.innerHTML = 'Use 8 to 128 characters.';
        else if (!ok.capital) source.innerHTML = 'Add at least one capital letter.';
        else if (!ok.number) source.innerHTML = 'Add at least one number.';
        else if (!ok.special) source.innerHTML = 'Add at least one special character, such as ! @ # $ %.';
    };
    window.skillUpAgreeCheck = (source, args) => {
        args.IsValid = !!document.getElementById('AgreeTerms')?.checked;
    };
    const strengthNames = ['not entered yet', 'Weak', 'Fair', 'Good', 'Strong'];
    const initPasswordMeters = () => document.querySelectorAll('[data-pw-for]').forEach(help => {
        const input = document.getElementById(help.dataset.pwFor);
        if (!input) return;
        const meter = help.querySelector('.pw-meter');
        const label = help.querySelector('.pw-label b');
        const update = () => {
            const level = passwordStrength(input.value);
            const ok = passwordRules(input.value);
            meter.dataset.level = level;
            meter.setAttribute('aria-valuenow', level);
            label.textContent = strengthNames[level];
            help.querySelectorAll('[data-rule]').forEach(item => {
                if (ok[item.dataset.rule] !== undefined) item.classList.toggle('is-met', ok[item.dataset.rule]);
            });
        };
        input.addEventListener('input', update);
        update();
    });
    initPasswordMeters();
    // Activity hint button: the hint is already in the page, so it opens and closes in place instead of reloading the page.
    document.addEventListener('click', e => {
        const button = e.target.closest('.answer-hint');
        const hint = document.getElementById('answer-hint-text');
        if (!button || !hint || button.disabled) return;
        e.preventDefault();
        e.stopPropagation();
        hint.hidden = !hint.hidden;
        document.querySelectorAll('.answer-hint').forEach(b => b.setAttribute('aria-expanded', String(!hint.hidden)));
        if (!hint.hidden) hint.scrollIntoView({ block: 'nearest', behavior: scrollBehavior() });
    }, true);

    // After part of an activity page is swapped in place, the page's hidden form state must match the new markup, or the next
    // form post would be refused as invalid.
    const syncFormState = fresh => ['__VIEWSTATE', '__VIEWSTATEGENERATOR', '__EVENTVALIDATION'].forEach(name => {
        const from = fresh.querySelector('input[name="' + name + '"]');
        const to = document.querySelector('input[name="' + name + '"]');
        if (from && to) to.value = from.value;
    });
    const answerToast = text => {
        const el = document.getElementById('toast');
        if (!el) return;
        el.textContent = text;
        el.classList.add('visible');
        setTimeout(() => el.classList.remove('visible'), 3500);
    };

    // Quiz: submitting, "Try again" and "Restart quiz" post the page's own form in the background and swap in the new page,
    // so the result (or a fresh quiz) appears without a reload. Falls back to the normal form post if anything fails.
    document.addEventListener('click', async e => {
        const button = e.target.closest('.answer-quiz button.answer-submit, .answer-quiz button.ghost, .answer-quiz-result button.ghost');
        const form = button?.closest('form');
        if (!button || !form || !button.name || button.disabled || button.dataset.plain || !button.closest('.answer-page')) return;
        const quiz = button.closest('.answer-quiz');
        const error = quiz?.querySelector('.form-error');
        if (quiz && button.classList.contains('answer-submit')) {
            const unanswered = [...quiz.querySelectorAll('fieldset.answer-question')].some(q => !q.querySelector('input[type=radio]:checked'));
            if (unanswered) {
                e.preventDefault();
                e.stopPropagation();
                if (error) error.textContent = 'Answer every question before submitting.';
                return;
            }
        }
        e.preventDefault();
        e.stopPropagation();
        button.disabled = true;
        try {
            const data = new FormData(form);
            data.set(button.name, button.value || '');
            const r = await fetch(form.action || location.href, { method: 'POST', body: data, credentials: 'same-origin' });
            if (!r.ok) throw Error(String(r.status));
            const fresh = new DOMParser().parseFromString(await r.text(), 'text/html');
            const workspace = fresh.querySelector('.answer-workspace');
            if (!workspace) throw Error('no page');
            document.querySelector('.answer-workspace').replaceWith(workspace);
            const crumb = fresh.querySelector('.answer-breadcrumb');
            if (crumb) document.querySelector('.answer-breadcrumb').replaceWith(crumb);
            const nav = fresh.querySelector('.answer-activity-nav');
            if (nav) document.querySelector('.answer-activity-nav')?.replaceWith(nav);
            syncFormState(fresh);
            if (window.skillUpQuizTimer) window.skillUpQuizTimer();
            const result = workspace.querySelector('.answer-quiz-result');
            if (result) {
                const score = result.querySelector('h1')?.textContent.trim();
                answerToast(result.querySelector('.answer-task-label')?.textContent.includes('passed') ? 'Quiz passed — ' + score : 'Not passed yet — ' + score + '. Review and try again.');
            }
        } catch {
            button.dataset.plain = '1';
            button.disabled = false;
            button.click();
        }
    }, true);

    // Submitting a lab answer, or marking a reading complete, happens in the background: a wrong answer shows its message
    // under the box, a right one shows a pop-up and updates the page in place. If anything fails, the button falls back to
    // its normal form post.
    document.addEventListener('click', async e => {
        const button = e.target.closest('button.answer-submit:not(.shell-execute)');
        const task = button?.closest('.answer-task');
        const page = button?.closest('.answer-page');
        if (!button || !task || !page || task.classList.contains('answer-quiz') || task.classList.contains('lab-shell') || button.disabled || button.dataset.plain) return;
        const api = page.querySelector('.answer-ai')?.dataset.api;
        const box = task.querySelector('.answer-answer-row input[type=text], .answer-answer-row input:not([type])');
        const error = task.querySelector('.form-error');
        if (!api) return;
        e.preventDefault();
        e.stopPropagation();
        const answer = box ? box.value.trim() : null;
        if (box && !answer) {
            if (error) error.textContent = 'Type your answer first.';
            box.focus();
            return;
        }
        const toast = text => {
            const el = document.getElementById('toast');
            if (!el) return;
            el.textContent = text;
            el.classList.add('visible');
            setTimeout(() => el.classList.remove('visible'), 3500);
        };
        button.disabled = true;
        try {
            const body = { courseId: page.dataset.answerCourse, lessonId: page.dataset.answerLesson };
            if (box) body.answer = answer;
            const r = await fetch(api + 'complete', {
                method: 'POST', credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'X-SkillUp-Request': '1' }, body: JSON.stringify(body)
            });
            const result = await r.json();
            if (!r.ok) {
                if (error) error.textContent = result.detail || 'That could not be saved. Please try again.';
                button.disabled = false;
                return;
            }
            if (result.data && result.data.correct === false) {
                if (error) error.textContent = 'Not quite. Check the method name and try again. Your attempt has been recorded.';
                button.disabled = false;
                box?.focus();
                box?.select();
                return;
            }
            // Correct (or a reading completed): show the finished page without reloading it.
            const html = await (await fetch(location.href, { credentials: 'same-origin' })).text();
            const fresh = new DOMParser().parseFromString(html, 'text/html');
            const swap = selector => {
                const from = fresh.querySelector(selector);
                const to = document.querySelector(selector);
                if (from && to) to.replaceWith(from);
            };
            // Only the parts that change are swapped; the terminal (if any) keeps running.
            const freshTask = [...fresh.querySelectorAll('.answer-task')].find(t => !t.classList.contains('lab-shell') && !t.classList.contains('answer-quiz'));
            if (freshTask) task.replaceWith(freshTask);
            const savedNote = fresh.querySelector('.answer-saved');
            const workspace = document.querySelector('.answer-workspace');
            if (savedNote && workspace && !workspace.querySelector('.answer-saved'))
                workspace.insertBefore(savedNote, workspace.querySelector('.lab-shell, .answer-task'));
            swap('.answer-activity-nav');
            swap('.answer-breadcrumb');
            syncFormState(fresh);
            toast(box ? 'Correct — lab completed!' : 'Activity completed');
        } catch {
            button.dataset.plain = '1';
            button.disabled = false;
            button.click();
        }
    }, true);

    // Course page, learner reviews: switching between "This course" and "All courses" posts the form in the background and
    // swaps in the new review section, so the page does not reload or jump. Falls back to the normal postback if it fails.
    document.addEventListener('change', async e => {
        const select = e.target.closest('select[id$="ReviewScope"]');
        const form = select?.closest('form');
        const section = document.getElementById('course-reviews');
        if (!select || !form || !section || select.dataset.plain) return;
        e.stopPropagation();
        section.style.opacity = '.55';
        section.setAttribute('aria-busy', 'true');
        try {
            const data = new FormData(form);
            data.set('__EVENTTARGET', select.name);
            data.set('__EVENTARGUMENT', '');
            const r = await fetch(form.action || location.href, { method: 'POST', body: data, credentials: 'same-origin' });
            if (!r.ok) throw Error(String(r.status));
            const fresh = new DOMParser().parseFromString(await r.text(), 'text/html');
            const next = fresh.getElementById('course-reviews');
            if (!next) throw Error('no reviews');
            section.replaceWith(next);
            syncFormState(fresh);
            document.querySelector('select[id$="ReviewScope"]')?.focus({ preventScroll: true });
        } catch {
            section.style.opacity = '';
            select.dataset.plain = '1';
            if (window.__doPostBack) window.__doPostBack(select.name, '');
        }
    }, true);

    // Course editor: Save stays off until a course field really changes. (The server also refuses a save with nothing new, because a
    // save sends a published course back to draft.)
    const initCourseFields = () => {
        const courseFields = document.querySelector('.course-info-page[data-fresh="false"][data-locked="false"] .ci-fields');
        if (!courseFields) return;
        const fields = [...courseFields.querySelectorAll('input:not([readonly]), select, textarea')];
        const snapshot = () => fields.map(f => f.value.replace(/\r\n/g, '\n').trim()).join('\u0001');
        const initial = snapshot();
        const saves = [...document.querySelectorAll('.course-info-page [id$="SaveButton"], .course-info-page [id$="HeaderSave"]')];
        const sync = () => {
            const changed = snapshot() !== initial;
            saves.forEach(b => {
                b.disabled = !changed;
                b.title = changed ? '' : 'No changes to save';
            });
        };
        courseFields.addEventListener('input', sync);
        courseFields.addEventListener('change', sync);
        sync();
    };
    initCourseFields();

    // A search box above a long dropdown (the Enrol learner list): typing narrows the choices as you type. Each word typed must begin a
    // word of the choice (name, username, email or ID), the same rule as the other search boxes. The choices still post as before.
    const initFilterSelects = () => document.querySelectorAll('input[data-filter-select]').forEach(box => {
        const select = document.getElementById(box.dataset.filterSelect);
        if (!select) return;
        const all = [...select.options].map(o => ({ value: o.value, text: o.text }));
        const count = document.querySelector('[data-filter-count="' + select.id + '"]');
        const submit = select.closest('form')?.querySelector('.sp-enrol-submit');
        const wordsOf = text => (text || '').toLowerCase().match(/[\p{L}\p{N}]+/gu) || [];
        // data-match="id": the box finds a learner by user ID only (the option's value). "14", "#14" and "ID 14" all mean learner 14.
        const byId = box.dataset.match === 'id';
        const wantedId = () => {
            let t = box.value.trim();
            if (t.startsWith('#')) t = t.slice(1);
            else if (/^id\b/i.test(t)) t = t.slice(2);
            t = t.trim();
            return t === '' ? null : /^\d+$/.test(t) ? String(Number(t)) : 'none';
        };
        const fits = (option) => {
            if (byId) {
                const id = wantedId();
                return id === null || option.value === id;
            }
            const wanted = wordsOf(box.value);
            if (!wanted.length) return true;
            const words = wordsOf(option.text);
            return wanted.every(w => words.some(x => x.startsWith(w)));
        };
        const render = () => {
            const chosen = select.value;
            const list = all.filter(o => fits(o));
            select.replaceChildren(...list.map(o => new Option(o.text, o.value, false, o.value === chosen)));
            if (list.length && !list.some(o => o.value === chosen)) select.selectedIndex = 0;
            if (count) count.textContent = list.length === 0 ? 'No learner has that user ID.' : box.value.trim() ? list.length + ' of ' + all.length + ' learners' : '';
            if (submit) submit.disabled = list.length === 0;
        };
        box.addEventListener('input', render);
        // Enter in the search box picks the first match; it does not submit the form.
        box.addEventListener('keydown', e => {
            if (e.key === 'Enter') {
                e.preventDefault();
                select.focus();
            }
        });
    });
    initFilterSelects();

    // Ticking the terms box clears its message at once.
    document.getElementById('AgreeTerms')?.addEventListener('change', e => {
        if (e.target.checked) e.target.closest('.auth-terms')?.querySelectorAll('.field-error').forEach(m => { m.style.display = 'none'; });
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
        // The certificate pages are only a little taller than the window, so the button shows after a small scroll there.
        const early = !!document.querySelector('main[data-view="certificates"]');
        const sync = () => scrollTopButton.classList.toggle('is-visible', scrollY > (early ? 40 : Math.max(400, innerHeight / 2)));
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

        // Announcements and their comments refresh like direct messages do. The compose form and the comment box move into the
        // new content as they are, so half-typed text, the cursor and the focus are never lost. If a newer announcement has
        // replaced the one on screen while a comment is half-written, the swap waits, so the comment is never posted to the wrong one.
        const syncAnnouncements = page => {
            const here = document.querySelector('.sc-announcements-scroll'), there = page.querySelector('.sc-announcements-scroll');
            if (!here || !there) return;
            const keep = '.sc-comment-form, #ComposePanel';
            const signature = root => {
                const copy = root.cloneNode(true);
                copy.querySelectorAll(keep).forEach(part => part.remove());
                return copy.innerHTML;
            };
            if (signature(here) === signature(there)) return;
            const shown = root => root.querySelector('[data-announcement-id]')?.dataset.announcementId;
            if (shown(here) !== shown(there) && document.getElementById('CommentBox')?.value.trim()) return;
            const active = document.activeElement, focused = here.contains(active) ? active : null;
            const cursor = focused && 'selectionStart' in focused ? [focused.selectionStart, focused.selectionEnd] : null;
            for (const live of here.querySelectorAll(keep)) {
                const same = live.id ? there.querySelector('#' + live.id) : there.querySelector(keep.split(',')[0].trim());
                if (same) same.replaceWith(live);
            }
            here.replaceChildren(...there.childNodes);
            // Keep the page's hidden state fields current, so Delete on a comment that just arrived is accepted.
            page.querySelectorAll('input[type=hidden][name^="__"]').forEach(field => {
                const old = [...document.querySelectorAll('input[type=hidden]')].find(x => x.name === field.name);
                if (old) old.value = field.value;
            });
            if (focused?.isConnected) {
                focused.focus({ preventScroll: true });
                if (cursor) try { focused.setSelectionRange(cursor[0], cursor[1]); } catch { /* no cursor in this field */ }
            }
        };

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
                if (switching || url !== location.href) return;
                if (thread && next && next.innerHTML !== thread.innerHTML) {
                    const box = thread.parentElement, atEnd = force === true || box.scrollHeight - box.scrollTop - box.clientHeight < 90;
                    thread.innerHTML = next.innerHTML;
                    if (atEnd) box.scrollTop = box.scrollHeight;
                }
                for (const part of ['.sc-contact-scroll', '.sc-contacts-head', '.sc-channels']) {
                    const here = document.querySelector(part), there = page.querySelector(part);
                    if (here && there && here.innerHTML !== there.innerHTML) here.innerHTML = there.innerHTML;
                }
                syncAnnouncements(page);
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

        // Announcements without a page reload: posting or deleting a comment and publishing an announcement are sent in the
        // background and the page updates in place, like sending a direct message. If anything fails, the button falls back
        // to its normal form post, which shows the server's message.
        const notice = text => {
            const toast = document.getElementById('toast');
            if (!toast) return;
            toast.textContent = text;
            toast.classList.add('visible');
            setTimeout(() => toast.classList.remove('visible'), 3000);
        };
        const postJson = async (command, body) => {
            const r = await fetch(chatRoot().dataset.api + command, {
                method: 'POST', credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'X-SkillUp-Request': '1' }, body: JSON.stringify(body)
            });
            if (!r.ok) throw Error(String(r.status));
        };
        const announcementId = () => document.querySelector('[data-announcement-id]')?.dataset.announcementId;
        document.addEventListener('click', async e => {
            if (plain || e.button !== 0 || !chatRoot()?.dataset.api) return;
            const post = e.target.closest('.sc-comment-post'), del = e.target.closest('.sc-comment-delete'), publish = e.target.closest('[id$="PublishButton"]');
            const button = post || del || publish;
            if (!button) return;
            const fallback = () => { plain = true; button.click(); plain = false; };
            if (post) {
                const field = document.getElementById('CommentBox'), text = field?.value.trim(), id = announcementId();
                e.preventDefault();
                e.stopImmediatePropagation();
                if (!text || !id || sending) return;
                sending = true;
                try {
                    await postJson('announcementComment', { id, comment: text });
                    field.value = '';
                    await check(true);
                    field.focus({ preventScroll: true });
                    field.scrollIntoView({ block: 'nearest', behavior: scrollBehavior() });
                    notice('Comment posted');
                } catch { fallback(); } finally { sending = false; }
            } else if (del) {
                const id = announcementId(), commentId = del.dataset.commentId;
                e.preventDefault();
                e.stopImmediatePropagation();
                if (!id || !commentId || sending || !confirm(del.dataset.confirm || 'Delete your comment?')) return;
                sending = true;
                try {
                    await postJson('announcementCommentDelete', { id, commentId });
                    await check(true);
                    notice('Comment deleted');
                } catch { fallback(); } finally { sending = false; }
            } else {
                // Publish: the page's own validators run first; if they object, the normal click shows their messages.
                if (window.Page_ClientValidate && !window.Page_ClientValidate('compose')) return;
                const course = document.querySelector('[id$="ComposeCourse"]')?.value;
                const title = document.querySelector('[id$="ComposeTitle"]')?.value.trim(), body = document.querySelector('[id$="ComposeBody"]')?.value.trim();
                e.preventDefault();
                e.stopImmediatePropagation();
                if (!course || !title || !body || sending) return;
                sending = true;
                try {
                    await postJson('announcementSave', { courseId: course, title, body });
                    const conversation = chatRoot().dataset.with;
                    await open('Messages?channel=' + course + (conversation && conversation !== '0' ? '&id=' + conversation : ''), true);
                    notice('Announcement published');
                } catch { fallback(); } finally { sending = false; }
            }
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
    const initRequests = () => {
        const requests = document.querySelector('.course-requests-page');
        if (!requests) return;
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
    };
    initRequests();

    // The quiz editor's companion shows its newest reply.
    const aiHistory = document.getElementById('qe-ai-history');
    if (aiHistory) aiHistory.scrollTop = aiHistory.scrollHeight;

    // ---- Instructor pages: no full reload for what does not need one ----------------------------------------
    // Clicking a button (save, tab, filter, expand, delete...), choosing a filter or following a link to the same page with another
    // address (sorting, paging, picking a request) is sent in the background, and only the page's own content is swapped in, so the
    // top bar, the scroll position and what you were doing stay. A link to another page still opens it normally. If anything fails, the
    // browser does the normal full request instead.
    const softMain = document.getElementById('main');
    const softOn = body.classList.contains('role-instructor') && !!softMain
        && ['courses', 'course-edit', 'resources', 'learners', 'requests', 'sessions', 'profile'].includes(softMain.dataset.view);
    if (softOn) {
        const form = document.getElementById('SkillForm');
        let busy = false;
        history.replaceState({ soft: 1 }, '');
        const hasNotice = note => note && note.classList.contains('visible') && note.textContent.trim();
        const rerun = fresh => {
            // The validators' own setup scripts are part of each page, so they are run again for the new content.
            fresh.querySelectorAll('form script:not([src])').forEach(old => {
                if (!/Page_Validators|Page_ValidationActive|ValidatorOnLoad|\.controltovalidate/.test(old.textContent)) return;
                const s = document.createElement('script');
                s.textContent = old.textContent;
                document.body.appendChild(s);
                s.remove();
            });
            quietValidators();
        };
        const apply = async (response, hash, push) => {
            const finalUrl = new URL(response.url);
            const fresh = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = fresh.getElementById('main');
            if (!next || next.dataset.view !== softMain.dataset.view || finalUrl.pathname !== location.pathname) {
                // Another page: open it, carrying the "saved" notice (the server has already shown it to this request).
                const note = fresh.getElementById('toast');
                if (hasNotice(note)) { try { sessionStorage.setItem('skillup.toast', note.textContent.trim()); } catch { /* storage blocked */ } }
                location.assign(response.url + hash);
                return;
            }
            softMain.replaceChildren(...next.childNodes);
            document.title = fresh.title;
            syncFormState(fresh);
            // The page may set the form's address itself (the course editor's tab), so that is the address of what is shown now.
            const pageAction = fresh.getElementById('SkillForm')?.getAttribute('action');
            const shown = pageAction ? new URL(pageAction, response.url) : finalUrl;
            const where = shown.pathname + shown.search;
            form.setAttribute('action', where);
            if (push && where !== location.pathname + location.search) history.pushState({ soft: 1 }, '', where + hash);
            rerun(fresh);
            initCourseFields();
            initFilterSelects();
            initRequests();
            initPasswordMeters();
            const note = fresh.getElementById('toast');
            if (hasNotice(note)) answerToast(note.textContent.trim());
            // An activity opened for editing scrolls its form into view, as the old address with #lesson-editor did.
            const target = hash || (/[?&]lesson=/.test(finalUrl.search) ? '#lesson-editor' : '');
            if (target) document.querySelector(target)?.scrollIntoView({ behavior: scrollBehavior(), block: 'start' });
        };
        const working = on => {
            softMain.toggleAttribute('aria-busy', on);
            softMain.style.transition = 'opacity .15s';
            softMain.style.opacity = on ? '.7' : '';
        };
        const softPost = async (submitter, eventTarget, eventArgument) => {
            if (busy) return true;
            busy = true;
            working(true);
            try {
                const data = new FormData(form);
                if (submitter) data.set(submitter.name, submitter.value || '');
                if (eventTarget != null) {
                    data.set('__EVENTTARGET', eventTarget);
                    data.set('__EVENTARGUMENT', eventArgument || '');
                }
                const r = await fetch(form.getAttribute('action') || location.href, { method: 'POST', body: data, credentials: 'same-origin' });
                if (!r.ok || !(r.headers.get('content-type') || '').includes('text/html')) throw Error('not a page');
                await apply(r, '', true);
                return true;
            } catch {
                return false;
            } finally {
                busy = false;
                working(false);
            }
        };
        const softGet = async (url, push) => {
            if (busy) return;
            busy = true;
            working(true);
            try {
                const r = await fetch(url.pathname + url.search, { credentials: 'same-origin' });
                if (!r.ok || !(r.headers.get('content-type') || '').includes('text/html')) throw Error('not a page');
                await apply(r, url.hash, push);
            } catch {
                location.assign(url.href);
            } finally {
                busy = false;
                working(false);
            }
        };
        document.addEventListener('submit', async e => {
            const submitter = e.submitter;
            if (e.defaultPrevented || e.target !== form || !submitter || !submitter.name || !softMain.contains(submitter)
                || submitter.dataset.plain || submitter.matches('[id$="ExportButton"]')) return;
            e.preventDefault();
            if (!(await softPost(submitter))) {
                submitter.dataset.plain = '1';
                form.requestSubmit(submitter);
            }
        });
        // Drop-downs that post when changed (for example the rows per page) go through the same path.
        const nativePostBack = window.__doPostBack;
        if (nativePostBack) {
            window.__doPostBack = (eventTarget, eventArgument) => {
                if (form.onsubmit && form.onsubmit() === false) return;
                softPost(null, eventTarget, eventArgument).then(done => { if (!done) nativePostBack(eventTarget, eventArgument); });
            };
        }
        document.addEventListener('click', e => {
            if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
            const link = e.target.closest('a[href]');
            if (!link || !softMain.contains(link) || link.target || link.hasAttribute('download') || link.dataset.plain) return;
            const url = new URL(link.href, location.href);
            if (url.origin !== location.origin || url.pathname !== location.pathname) return;
            // A link to a place inside this very page (#anchor) is left to the browser; the same address without one (Cancel) redraws it.
            if (url.search === location.search && url.hash) return;
            e.preventDefault();
            softGet(url, true);
        });
        addEventListener('popstate', e => {
            if (e.state && e.state.soft) softGet(new URL(location.href), false);
        });
    }
    // A notice saved before opening another page (see above) shows once the page is open.
    try {
        const saved = sessionStorage.getItem('skillup.toast');
        if (saved) {
            sessionStorage.removeItem('skillup.toast');
            answerToast(saved);
        }
    } catch { /* storage blocked */ }

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
    // Live search: every search box filters as you type. A moment after the last keystroke the box is applied exactly as
    // pressing Enter would (its panel's default button is posted in the background, so each page's own filter code runs),
    // and only the results are swapped in. The box keeps focus and the cursor. If anything fails, Enter still works.
    // Only the results are swapped on the catalog and the contact list; on the other pages the whole page content is.
    const searchParts = box => box.closest('.catalog-page') ? ['#cat-count', '#results'] : box.id === 'ContactSearch' ? ['.sc-contact-scroll'] : ['#main'];
    let searchTimer = 0, searchRun = 0, searchCall = null;
    const searchButton = box => {
        for (let el = box.parentElement; el && el !== document.body; el = el.parentElement) {
            const match = /WebForm_FireDefaultButton\(event,\s*'([^']+)'/.exec(el.getAttribute('onkeypress') || '');
            if (match) return document.getElementById(match[1]);
        }
        return null;
    };
    const searchNow = async (box, form, button) => {
        const mine = ++searchRun;
        searchCall?.abort();
        searchCall = new AbortController();
        try {
            const data = new FormData(form);
            data.set(button.name, button.value || '');
            const reply = await fetch(form.getAttribute('action') || location.href, {
                method: 'POST', body: data, credentials: 'same-origin', headers: { 'X-SkillUp-Live': '1' }, signal: searchCall.signal
            });
            if (!reply.ok) return;
            const page = new DOMParser().parseFromString(await reply.text(), 'text/html');
            const target = new URL(reply.url);
            if (mine !== searchRun || target.pathname !== location.pathname) return;
            const focused = document.activeElement === box, start = box.selectionStart, end = box.selectionEnd;
            for (const selector of searchParts(box)) {
                const here = document.querySelector(selector), there = page.querySelector(selector);
                if (!here || !there) continue;
                // The live box moves into the new content, so it is never recreated.
                const same = [...there.querySelectorAll('input')].find(x => x.name === box.name);
                if (same) same.replaceWith(box);
                here.replaceChildren(...there.childNodes);
            }
            // The page's hidden state fields and the form's address come from the new page too, so later buttons act on what is shown.
            page.querySelectorAll('input[type=hidden][name^="__"]').forEach(field => {
                const old = [...document.querySelectorAll('input[type=hidden]')].find(x => x.name === field.name);
                if (old) old.value = field.value;
            });
            form.setAttribute('action', target.pathname + target.search);
            history.replaceState(null, '', target.pathname + target.search);
            if (focused) {
                box.focus({ preventScroll: true });
                try { box.setSelectionRange(start, end); } catch { /* not every input type has a cursor */ }
            }
        } catch { /* offline, or a newer search replaced this one: leave the page as it is */ }
    };
    document.addEventListener('input', e => {
        const box = e.target;
        // A box that only narrows a dropdown (the Enrol learner list) must never post its form: that would press "Enrol learner".
        if (box instanceof HTMLInputElement && box.hasAttribute('data-filter-select')) return;
        if (!(box instanceof HTMLInputElement) || !(box.type === 'search' || box.type === 'text' && /search/i.test(box.getAttribute('aria-label') || '')) || e.isComposing || !box.form) return;
        const button = searchButton(box);
        if (!button) return;
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => searchNow(box, box.form, button), 250);
    });
    // Message times in the companion chat: in the viewer's own clock and style, today's as a time and older ones with the day.
    const chatClock = date => {
        const time = date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
        return date.toDateString() === new Date().toDateString() ? time : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' }) + ', ' + time;
    };
    const stampTime = (label, date) => {
        const mark = document.createElement('time');
        mark.className = 'ai-time';
        mark.dateTime = date.toISOString();
        mark.title = date.toLocaleString();
        mark.textContent = chatClock(date);
        label.append(' \u00b7 ', mark);
    };
    document.querySelectorAll('time.ai-time[datetime]').forEach(mark => {
        const date = new Date(mark.getAttribute('datetime'));
        if (isNaN(date)) return;
        mark.textContent = chatClock(date);
        mark.title = date.toLocaleString();
    });
    // Lesson companion (also the AI Assistant page): a question is sent in the background (api/commands/ai) and the answer appears in place, so the page
    // never reloads. The first question shrinks the orb and heading to make room (css: .has-conversation). Without this script
    // the shortcuts and the Send button post the page as before.
    const lessonAi = document.querySelector('.answer-ai-reference[data-api]');
    if (lessonAi) {
        const log = lessonAi.querySelector('.answer-ai-conversation');
        const field = document.getElementById('AskBox');
        const sendButton = lessonAi.querySelector('.answer-ai-send');
        const problem = lessonAi.querySelector('.form-error');
        let asking = false;
        const toEnd = () => { log.scrollTop = log.scrollHeight; };
        toEnd();
        // Who a bubble belongs to: your own avatar (copied from the top bar, so a profile picture shows too), or the companion's orb.
        const avatarFor = kind => {
            const holder = document.createElement('span');
            holder.className = kind.includes('prompt') ? 'ai-avatar' : 'ai-avatar bot';
            if (kind.includes('prompt')) {
                const mine = document.querySelector('.gn-actions .avatar');
                if (mine) holder.append(mine.cloneNode(true));
            } else holder.setAttribute('aria-hidden', 'true');
            return holder;
        };
        const bubble = (kind, label, text, stamped) => {
            const box = document.createElement('div');
            box.className = kind + ' is-new';
            const small = document.createElement('small');
            small.textContent = label;
            if (stamped) stampTime(small, new Date());
            const p = document.createElement('p');
            p.textContent = text;
            box.append(avatarFor(kind), small, p);
            log.appendChild(box);
            toEnd();
            return box;
        };
        const askCompanion = async question => {
            question = (question || '').trim();
            if (!question || asking) return;
            asking = true;
            if (problem) problem.textContent = '';
            const first = !lessonAi.classList.contains('has-conversation');
            lessonAi.classList.add('has-conversation');
            if (field) field.value = '';
            if (sendButton) sendButton.disabled = true;
            bubble('answer-ai-prompt', 'You', question, true);
            const reply = bubble('answer-ai-reply is-wait', 'Skill UP companion', 'Thinking\u2026');
            try {
                const r = await fetch(lessonAi.dataset.api + 'ai', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', 'X-SkillUp-Request': '1' },
                    body: JSON.stringify({ text: question, lessonId: lessonAi.dataset.lesson })
                });
                const body = await r.json().catch(() => ({}));
                if (!r.ok || !body.data?.response) throw Error(body.detail || 'The companion could not answer. Please try again.');
                reply.classList.remove('is-wait');
                reply.querySelector('p').textContent = body.data.response;
                stampTime(reply.querySelector('small'), new Date());
                // The reply is on screen, so it is read: tell the server (its badge in the AI Assistant list never shows), and the open
                // conversation's row in that list counts as started.
                fetch(lessonAi.dataset.api + 'aiRead', {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', 'X-SkillUp-Request': '1' },
                    body: JSON.stringify({ lessonId: lessonAi.dataset.lesson })
                }).catch(() => { /* it is marked read the next time the page opens */ });
                document.querySelector('.assistant-convo.is-active')?.classList.remove('is-empty');
            } catch (err) {
                // Take the unanswered question back out and give it back to the box, so nothing is lost.
                reply.previousElementSibling.remove();
                reply.remove();
                if (first) lessonAi.classList.remove('has-conversation');
                if (field) field.value = question;
                if (problem) problem.textContent = err instanceof TypeError ? 'Could not reach the server. Check your connection and try again.' : err.message;
            }
            asking = false;
            if (sendButton) sendButton.disabled = false;
            field?.focus({ preventScroll: true });
            toEnd();
        };
        // Capture phase: this runs before the button's own form post. Pressing Enter in the box clicks Send, so it comes through here too.
        lessonAi.addEventListener('click', e => {
            const button = e.target.closest('button');
            if (!button || button.disabled) return;
            let question = null;
            if (button.closest('.answer-ai-suggest') || button.classList.contains('answer-ai-add')) question = button.dataset.question;
            else if (button.classList.contains('answer-ai-send')) question = field?.value;
            if (question == null) return;
            e.preventDefault();
            e.stopImmediatePropagation();
            askCompanion(question);
        }, true);
    }
    // AI Assistant page: find a conversation in the list, keep the short-window marker right when the window changes (the page also sets it before the first paint),
    // and show a "scroll to the latest message" button while the conversation is scrolled up.
    const assistantSplit = document.querySelector('.assistant-split');
    if (assistantSplit) {
        const fitAssistant = () => {
            const zoom = parseFloat(getComputedStyle(document.documentElement).zoom) || 1, height = innerHeight / zoom;
            assistantSplit.classList.toggle('is-short', height < 800);
            assistantSplit.classList.toggle('is-tiny', height < 660);
        };
        fitAssistant();
        addEventListener('resize', fitAssistant);
        // Find a conversation: hides the rows that do not match and opens the courses that have a match; clearing it puts the courses back.
        const findBox = assistantSplit.querySelector('[data-conversation-search]');
        if (findBox) {
            const rows = [...assistantSplit.querySelectorAll('.assistant-convo')], groups = [...assistantSplit.querySelectorAll('.assistant-group')];
            const noMatch = assistantSplit.querySelector('.assistant-nomatch');
            const find = () => {
                const term = findBox.value.trim().toLowerCase();
                let shown = 0;
                for (const group of groups) {
                    if (group.dataset.was === undefined) group.dataset.was = group.open ? '1' : '0';
                    const courseHit = !term || group.querySelector(':scope > summary').textContent.toLowerCase().includes(term);
                    let any = false;
                    for (const row of group.querySelectorAll('.assistant-convo')) {
                        const hit = courseHit || row.textContent.toLowerCase().includes(term);
                        row.hidden = !hit;
                        any = any || hit;
                    }
                    group.hidden = !any;
                    if (term) group.open = any; else { group.open = group.dataset.was === '1'; delete group.dataset.was; }
                    shown += any ? 1 : 0;
                }
                for (const row of rows.filter(r => !r.closest('.assistant-group'))) {
                    row.hidden = !!term && !row.textContent.toLowerCase().includes(term);
                    shown += row.hidden ? 0 : 1;
                }
                if (noMatch) noMatch.hidden = shown > 0;
            };
            findBox.addEventListener('input', find);
            findBox.addEventListener('keydown', e => { if (e.key === 'Enter') e.preventDefault(); });
        }
        const assistantPanel = assistantSplit.querySelector('.assistant-companion');
        const assistantLog = assistantPanel?.querySelector('.answer-ai-conversation');
        if (assistantLog) {
            const jump = document.createElement('button');
            jump.type = 'button';
            jump.className = 'assistant-jump';
            jump.setAttribute('aria-label', 'Scroll to the latest message');
            jump.title = 'Latest message';
            jump.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 5v14m-6-6 6 6 6-6"/></svg>';
            assistantPanel.appendChild(jump);
            const syncJump = () => {
                jump.classList.toggle('is-visible', assistantLog.scrollHeight - assistantLog.scrollTop - assistantLog.clientHeight > 120);
                jump.style.bottom = (assistantPanel.querySelector('.answer-ai-bottom').offsetHeight + 12) + 'px';
            };
            assistantLog.addEventListener('scroll', syncJump, { passive: true });
            new MutationObserver(syncJump).observe(assistantLog, { childList: true });
            addEventListener('resize', syncJump);
            jump.addEventListener('click', () => assistantLog.scrollTo({ top: assistantLog.scrollHeight, behavior: scrollBehavior() }));
            syncJump();
        }
    }
    // Messages: drag the dividers. Two handles sit on the borders between the course list, the announcements and the conversation;
    // dragging one resizes that side (the announcements take the rest). The widths are remembered in this browser, a double-click
    // (or Home) puts a divider back to the default, and the arrow keys move a focused one (Shift for bigger steps).
    const widthsKey = 'skillup.messages.widths';
    const pageRoot = document.documentElement;
    const pageZoom = () => parseFloat(getComputedStyle(pageRoot).zoom) || 1;
    const savedWidths = () => {
        try {
            const value = JSON.parse(localStorage.getItem(widthsKey) || 'null');
            return value && isFinite(value.left) && isFinite(value.right) ? value : null;
        } catch { return null; }
    };
    const rememberWidths = widths => {
        try { widths ? localStorage.setItem(widthsKey, JSON.stringify(widths)) : localStorage.removeItem(widthsKey); } catch { /* private window: the widths last until the page closes */ }
    };
    const showWidths = widths => {
        pageRoot.classList.toggle('sc-sized', !!widths);
        if (widths) {
            pageRoot.style.setProperty('--sc-left', widths.left + 'px');
            pageRoot.style.setProperty('--sc-right', widths.right + 'px');
        } else {
            pageRoot.style.removeProperty('--sc-left');
            pageRoot.style.removeProperty('--sc-right');
        }
    };
    // Keeps each side between its limits and leaves at least 300px for the announcements.
    const limitWidths = (widths, total) => {
        let left = Math.min(Math.max(widths.left, 170), 420), right = Math.min(Math.max(widths.right, 240), 560);
        const over = left + right + 300 - total;
        if (over > 0) {
            const fromRight = Math.min(over, right - 240);
            right -= fromRight;
            left -= Math.min(over - fromRight, left - 170);
        }
        return { left: Math.round(left), right: Math.round(right) };
    };
    const mountResizers = () => {
        const chat = chatRoot();
        if (!chat || !chat.classList.contains('sc-workspace') || chat.querySelector(':scope > .sc-resizer')) return;
        const contacts = chat.querySelector(':scope > .sc-contacts'), middle = chat.querySelector(':scope > .sc-announcements'), pane = chat.querySelector(':scope > .sc-pane');
        if (!contacts || !middle || !pane) return;
        const make = (side, label) => {
            const handle = document.createElement('div');
            handle.className = 'sc-resizer';
            handle.dataset.side = side;
            handle.tabIndex = 0;
            handle.setAttribute('role', 'separator');
            handle.setAttribute('aria-orientation', 'vertical');
            handle.setAttribute('aria-label', label);
            handle.title = label + ': drag to resize, double-click to reset';
            chat.appendChild(handle);
            return handle;
        };
        const handles = { left: make('left', 'Resize the course list'), right: make('right', 'Resize the conversation') };
        const place = () => {
            const zoom = pageZoom(), box = chat.getBoundingClientRect();
            handles.left.style.left = (contacts.getBoundingClientRect().right - box.left) / zoom + 'px';
            handles.right.style.left = (middle.getBoundingClientRect().right - box.left) / zoom + 'px';
        };
        const current = () => {
            const zoom = pageZoom();
            return { left: contacts.getBoundingClientRect().width / zoom, right: pane.getBoundingClientRect().width / zoom };
        };
        const resize = (side, width) => {
            const base = savedWidths() || current();
            const next = limitWidths({ left: side === 'left' ? width : base.left, right: side === 'right' ? width : base.right }, chat.getBoundingClientRect().width / pageZoom());
            showWidths(next);
            rememberWidths(next);
            place();
        };
        const reset = () => {
            showWidths(null);
            rememberWidths(null);
            requestAnimationFrame(place);
        };
        for (const side of ['left', 'right']) {
            const handle = handles[side];
            handle.addEventListener('pointerdown', e => {
                if (e.button !== 0) return;
                e.preventDefault();
                handle.setPointerCapture(e.pointerId);
                handle.classList.add('is-dragging');
                document.body.classList.add('sc-resizing');
            });
            handle.addEventListener('pointermove', e => {
                if (!handle.hasPointerCapture(e.pointerId)) return;
                const zoom = pageZoom(), box = chat.getBoundingClientRect();
                resize(side, side === 'left' ? (e.clientX - box.left) / zoom : (box.right - e.clientX) / zoom);
            });
            const release = e => {
                if (handle.hasPointerCapture(e.pointerId)) handle.releasePointerCapture(e.pointerId);
                handle.classList.remove('is-dragging');
                document.body.classList.remove('sc-resizing');
            };
            handle.addEventListener('pointerup', release);
            handle.addEventListener('pointercancel', release);
            handle.addEventListener('dblclick', reset);
            handle.addEventListener('keydown', e => {
                if (e.key === 'Home') { e.preventDefault(); reset(); return; }
                if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
                e.preventDefault();
                const step = (e.shiftKey ? 48 : 16) * (e.key === 'ArrowRight' ? 1 : -1), base = savedWidths() || current();
                resize(side, side === 'left' ? base.left + step : base.right - step);
            });
        }
        // The handles follow the columns whenever they change (dragging, a window resize, a media-query change).
        const watch = new ResizeObserver(place);
        watch.observe(contacts);
        watch.observe(middle);
        place();
    };
    mountResizers();
    // Opening another conversation replaces the whole workspace; the handles are put back on the new one.
    const workspaceHost = document.getElementById('main');
    if (workspaceHost && chatRoot()) new MutationObserver(mountResizers).observe(workspaceHost, { childList: true, subtree: true });
    // A window narrower than the saved widths allow shrinks them to fit.
    addEventListener('resize', () => {
        const chat = chatRoot(), saved = savedWidths();
        if (!chat || !saved) return;
        const fitted = limitWidths(saved, chat.getBoundingClientRect().width / pageZoom());
        if (fitted.left !== saved.left || fitted.right !== saved.right) showWidths(fitted);
    });
})();

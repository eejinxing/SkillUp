# UI refinement — 17 October 2026

## Learner dashboard reference components

The resume cards now follow the supplied rounded profile-card reference, adapted for learning: program name, course name, subject icon, calculated course rating, completed/total activities, duration in weeks, and a resume/explore button. Missing ratings use a dash; no score is invented. The existing blue summary and cyan learning-path artwork remain.

The Global glass-bubble panel is replaced by an Activity feed with All workspace / Personal / Team / Community filters. Events use current enrolments, course announcements, community messages, and relevant sessions. Community posting/history and session joining use the existing API actions. Filter selection persists within the tab. The layouts work on desktop, tablet, and phone. See `js/dashboard-components.js`, `dashboard-components.css`, and `tests/dashboard-components.cjs`.

## Legacy interface removal

The wide legacy sidebar, Workspace top bar, and demo footer markup have been removed from the signed-in shell. The shared edge navigation remains the sole workspace menu. The 21 supplied Learner/Instructor/Program Leader page examples retain their routes and actions; secondary workflows now use the existing Figma black/charcoal material language through `secondary-views.css`. Existing dedicated Figma screens are retained. Course catalogue/management thumbnails reuse the purple learning-course artwork; resources and the AI companion reuse existing Figma assets. No account records, course data, enrolments, preferences, or API commands were deleted.

Verified with `tests/secondary-views.cjs`: 21 role/page combinations, absent legacy DOM, catalogue search/course navigation, profile/preferences persistence and phone overflow. Existing workflow and route smoke checks also pass. The original source remains recoverable in the desktop project's timestamped backup.

This update follows the supplied Unregister User references. It preserves the black page, blue hero treatment, outlined category grid, team photograph, asymmetric testimonials, FAQ, and red call-to-action with grey footer.

## What changed

- One shared left-edge navigation on public and signed-in pages, with role-specific shortcuts, active state, unread message count, and an All pages drawer. The drawer supports Escape, backdrop dismissal, and focus restoration. Existing page-specific navigation rails are replaced visually; course/program selection controls remain available.
- Locally hosted DM Sans for interface text, clearer body/caption sizes, centred hero copy and buttons, consistent control alignment, table spacing, and narrow-screen layouts.
- Eight original SVG computer-topic illustrations replace small raster thumbnails. The testimonial portrait uses a crop of the existing high-resolution team image. No external image downloads or AI generation were required.
- Readable testimonial cards, keyboard-operable FAQ accordions, clearer buttons, and a sharp computer/learning illustration in the red call-to-action. Sample learner stories are labelled as samples.
- Short hover/press feedback, visible keyboard focus, and reduced-motion support.

## Files and integration

`index.html` loads `home-refinement.css`, `ui-refinement.css`, `js/home-refinement.js`, and `js/ui-refinement.js`. The responsive homepage implementation overrides `SkillVisitorHome` after the original visitor module loads. The original Figma reference source is retained. `js/app.js` calls `SkillUIAfterRender({state,user,icon})` after each render.

Business logic, API commands, and the local mock storage format are unchanged. This remains a frontend demo using fictional local data. For the future C# integration, continue using `API-CONTRACT.md` and the existing API adapter.

## Verification

- UI refinement: 32 route visits across public, Learner, Instructor, Program Leader, and Admin screens; typography, centring, navigation, login, FAQ, and desktop/tablet/phone overflow checks.
- Existing smoke suite: 51 routes without JavaScript errors or horizontal overflow.
- Role completion suite: 60 route visits and interactive account, chat, approval, upload, and mobile navigation checks.
- Existing workflow suite: create → submit → approve → enrol → complete → certificate, plus grading, persisted profile, rollback, and instructor verification.
- Visitor checks, domain assertions, program-management data checks, and application data checks passed.
- Visual review at 1440px, 768px, and 390px. The design inspector reports zero failures and two warnings: the retained static design has no video, and the tablet layout uses ten font sizes. Original gradients, outlined cards, thin borders, and a small number of existing photographs were explicitly retained to honour the requested design.

Screenshots and check results are in `previews/`. The inspector report is in `.taste-skill/final/report.json` in the working copy.

## Open the desktop copy

In VS Code, open `C:/Users/Jinwang/Desktop/skillup-frontend`, right-click `index.html`, then choose **Open with Live Server**. If it is already open, reload with **Ctrl+F5**. Do not open an HTML file from the assets folder.

The deployment saves replaced files under `backups/ui-refinement-<timestamp>/` and records exact file hashes in `deployment-ui-refinement.json`. Demo data remains in each browser origin's localStorage; Live Server and file:// use separate storage.

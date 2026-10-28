/* Small progressive enhancements for the server-rendered pages. Every page works without this file. */
(function(){
 'use strict';
 // Section entrance animation on the landing pages.
 const reveal=document.querySelectorAll('.home-reveal');
 if(reveal.length&&'IntersectionObserver' in window&&!matchMedia('(prefers-reduced-motion:reduce)').matches){
  const seen=new IntersectionObserver(entries=>{for(const entry of entries)if(entry.isIntersecting){entry.target.classList.add('home-reveal-active');seen.unobserve(entry.target);}},{threshold:.06});
  reveal.forEach(item=>seen.observe(item));
 }else reveal.forEach(item=>item.classList.add('home-reveal-active'));

 // The save notice fades after a few seconds.
 const toast=document.getElementById('toast');
 if(toast&&toast.classList.contains('visible'))setTimeout(()=>toast.classList.remove('visible'),5000);

 // Buttons with data-confirm ask before they post back (deletes, withdrawals).
 document.addEventListener('click',e=>{const b=e.target.closest('[data-confirm]');if(b&&!confirm(b.dataset.confirm))e.preventDefault();});

 // Validator controls re-check a field when it loses focus. Clicking Send blurs the field, its message disappears,
 // the button moves up mid-click and the click misses, so fields are checked on submit only (as before).
 if(window.Page_Validators)for(const v of Page_Validators){const c=document.getElementById(v.controltovalidate);if(c&&String(c.onchange).includes('ValidatorOnChange'))c.onchange=null;}

 // Show or hide a password field.
 document.addEventListener('click',e=>{const eye=e.target.closest('[data-auth-eye]');if(!eye)return;const input=eye.parentElement.querySelector('input');const show=input.type==='password';input.type=show?'text':'password';eye.setAttribute('aria-label',show?'Hide password':'Show password');});

 // Landing page instructor carousel: the arrows scroll the track by one card.
 document.addEventListener('click',e=>{const b=e.target.closest('[data-instructor-step]');if(!b)return;const track=b.parentElement.querySelector('.instructor-track'),card=track&&track.querySelector('.instructor-card');if(card)track.scrollBy({left:Number(b.dataset.instructorStep)*(card.offsetWidth+20),behavior:'smooth'});});

 // Tapping an instructor card shows their short bio.
 document.addEventListener('click',e=>{const card=e.target.closest('.instructor-card');if(!card)return;const open=!card.classList.contains('is-open');card.classList.toggle('is-open',open);card.setAttribute('aria-expanded',String(open));});

 // Buttons that jump to a section of the page (learner dashboard).
 document.addEventListener('click',e=>{const b=e.target.closest('[data-vd-scroll]');if(b)document.getElementById(b.dataset.vdScroll)?.scrollIntoView({behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth',block:'start'});});

 // Messages: show the newest message, Enter sends (Shift + Enter adds a line), Ctrl + Enter posts a comment.
 const thread=document.getElementById('conversation-messages');if(thread)thread.scrollTop=thread.scrollHeight;
 document.addEventListener('keydown',e=>{
  if(e.target.id==='MessageBox'&&e.key==='Enter'&&!e.shiftKey&&!e.isComposing){e.preventDefault();if(e.target.value.trim())e.target.closest('.sc-composer').querySelector('.sc-send').click();}
  if(e.target.id==='CommentBox'&&e.key==='Enter'&&(e.ctrlKey||e.metaKey)){e.preventDefault();if(e.target.value.trim())e.target.closest('.sc-comment-box').querySelector('.sc-comment-post').click();}
 });

 // Escape closes any open disclosure menu, and the open Log in / Sign up pop-up (as its close button would).
 document.addEventListener('keydown',e=>{if(e.key!=='Escape')return;document.querySelectorAll('details[open].gn-more,details[open][data-menu],details[open].ld-monitor-filter,details[open].ld-monitor-actions').forEach(d=>d.open=false);document.querySelector('.auth-popup:is(:target,.is-open) .dialog-close')?.click();});

 // Staff pages: Print buttons, and the leader overview's "more updates" arrow scrolls the alert list.
 document.addEventListener('click',e=>{if(e.target.closest('[data-action=print]'))print();const more=e.target.closest('[data-leader-note-scroll]');if(more){const list=more.closest('.ld-notifications')?.querySelector('.ld-note-scroll');list?.scrollBy({top:Math.max(80,list.clientHeight*.75)});}});

 // Analytics figures count up from zero once.
 if(!matchMedia('(prefers-reduced-motion: reduce)').matches)document.querySelectorAll('.la-page [data-count],.rq-summary [data-count]').forEach((n,k)=>{const to=Number(n.dataset.count)||0,start=performance.now()+k*90;const step=t=>{const p=Math.min(1,Math.max(0,(t-start)/900));n.textContent=Math.round(to*(1-Math.pow(1-p,3)));if(p<1)requestAnimationFrame(step);};requestAnimationFrame(step);});

 // Course requests: tick rows to export only those; the header box ticks every row on the page.
 const requests=document.querySelector('.course-requests-page');
 if(requests){const boxes=()=>[...requests.querySelectorAll('.rq-row-check')],all=requests.querySelector('#rq-select-all'),count=requests.querySelector('#rq-selection-count'),clear=requests.querySelector('#rq-clear-selection'),exportButton=document.getElementById('ExportButton');
  const sync=()=>{const ticked=boxes().filter(b=>b.checked).length;count.textContent=ticked+' selected';clear.hidden=!ticked;if(all){all.checked=ticked>0&&ticked===boxes().length;all.indeterminate=ticked>0&&ticked<boxes().length;all.disabled=!boxes().length;}if(exportButton&&!exportButton.disabled)exportButton.textContent=ticked?'Export selected ('+ticked+')':'Export CSV';};
  requests.addEventListener('change',e=>{if(e.target===all)boxes().forEach(b=>b.checked=all.checked);if(e.target===all||e.target.matches('.rq-row-check'))sync();});
  clear?.addEventListener('click',()=>{boxes().forEach(b=>b.checked=false);sync();});sync();}

 // The quiz editor's companion shows its newest reply.
 const aiHistory=document.getElementById('qe-ai-history');if(aiHistory)aiHistory.scrollTop=aiHistory.scrollHeight;

 // Back to top button: appears once the page is scrolled down a screen, scrolls up smoothly (instantly with reduced motion).
 const top=document.querySelector('[data-scroll-top]');
 if(top){const sync=()=>top.classList.toggle('is-visible',scrollY>Math.max(400,innerHeight/2));addEventListener('scroll',sync,{passive:true});sync();
  top.addEventListener('click',()=>{scrollTo({top:0,behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth'});if(location.hash)history.replaceState(null,'',location.pathname+location.search);});}

 // Landing page for visitors: a browser refresh starts again at the top of Home (no #section in the address, no remembered scroll
 // position), wherever on the page it was pressed. Links to a section still open it, since those are not refreshes.
 if(document.body.classList.contains('smooth-scroll')&&document.body.classList.contains('ui-public')){
  history.scrollRestoration='manual';
  const entry=performance.getEntriesByType('navigation')[0];
  if(entry&&entry.type==='reload'){
   if(location.hash)history.replaceState(null,'',location.pathname+location.search);
   const top=()=>scrollTo({top:0,behavior:'instant'});top();addEventListener('load',top);
  }
 }

 // Landing page tabs for visitors: the section tab you clicked stays highlighted (also when a link from another page opened
 // that section) until you press the back-to-top button, which highlights About again. Scrolling by hand changes nothing.
 if(document.body.classList.contains('smooth-scroll')&&document.body.classList.contains('ui-public')){
  const tabs=[...document.querySelectorAll('.gn-links a')],tabAt=hash=>tabs.find(a=>(a.getAttribute('href')||'').split('#')[1]===hash);
  const mark=tab=>{if(!tab)return;tabs.forEach(a=>a.removeAttribute('aria-current'));tab.setAttribute('aria-current','page');};
  mark(tabAt(location.hash.slice(1)));
  tabs.forEach(a=>a.addEventListener('click',()=>{if((a.getAttribute('href')||'').includes('#'))mark(a);}));
  document.querySelector('[data-scroll-top]')?.addEventListener('click',()=>mark(tabAt('top')));
 }

 // On narrow screens the nav links scroll sideways; keep the current page's link in view.
 const links=document.querySelector('.gn-links'),active=links&&links.querySelector('[aria-current=page]');
 if(active&&links.scrollWidth>links.clientWidth)links.scrollLeft+=active.getBoundingClientRect().left-links.getBoundingClientRect().left-(links.clientWidth-active.offsetWidth)/2;
})();

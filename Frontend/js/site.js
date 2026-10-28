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

 // Escape closes any open disclosure menu.
 document.addEventListener('keydown',e=>{if(e.key==='Escape')document.querySelectorAll('details[open].gn-more,details[open][data-menu],details[open].ld-monitor-filter,details[open].ld-monitor-actions').forEach(d=>d.open=false);});

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
 const history=document.getElementById('qe-ai-history');if(history)history.scrollTop=history.scrollHeight;

 // On narrow screens the nav links scroll sideways; keep the current page's link in view.
 const links=document.querySelector('.gn-links'),active=links&&links.querySelector('[aria-current=page]');
 if(active&&links.scrollWidth>links.clientWidth)links.scrollLeft+=active.getBoundingClientRect().left-links.getBoundingClientRect().left-(links.clientWidth-active.offsetWidth)/2;
})();

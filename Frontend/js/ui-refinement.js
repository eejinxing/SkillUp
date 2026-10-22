/* Workspace destinations feed the top navigation directly; there is no sidebar. */
(function(root){
 'use strict';let latest,entrances;
 const menus={Learner:[['dashboard','Overview'],['learning','My courses'],['catalog','Explore courses'],['leaderboard','Leaderboard'],['certificates','Certificates']],Instructor:[['dashboard','Overview'],['courses','My courses'],['requests','My requests'],['learners','Learners'],['resources','Learning resources'],['analytics','Course analytics']],'Program Leader':[['dashboard','Overview'],['programs','Programs'],['assignments','Instructors'],['approvals','Course approvals'],['learners','Learner progress'],['analytics','Program analytics']],Admin:[['dashboard','Overview'],['users','User accounts']]};
 const guest=[['home','Home'],['catalog','Courses'],['instructors','Instructors'],['faq','FAQ'],['about','About Skill UP'],['contact','Contact'],['help','Help center'],['login','Log in'],['register','Create account']];
 root.SkillNavigationItems=ctx=>{const me=ctx.user();return (me?[...(menus[me.role]||[]),...(me.role==='Admin'?[]:[['discussions','Discussions'],['messages','Messages'],['sessions','Live sessions'],['assistant','AI companion']])]:guest).map(([path,label])=>({href:'#/'+path,label}));};
 const zoomKey=uid=>'skillup.zoom.'+(uid||'guest');
 function autoZoom(){const w=innerWidth;return w<1024?1:Math.round(Math.min(1.35,Math.max(.85,w/1440))*20)/20;}
 function applyZoom(uid){let value=autoZoom(),auto=true;try{const saved=Number(localStorage.getItem(zoomKey(uid)));if(saved>=.7&&saved<=1.6){value=saved;auto=false;}}catch{}if(innerWidth<1024)value=1;document.documentElement.style.zoom=value===1?'':String(value);document.documentElement.dataset.zoom=auto?'auto':'manual';}
 root.SkillUIAfterRender=ctx=>{latest=ctx;const me=ctx.user();document.body.classList.add('ui-refined');document.body.classList.toggle('ui-public',!me);document.body.classList.toggle('ui-home',!!document.querySelector('.home-refined'));applyZoom(me?.id);
  entrances?.disconnect();const items=document.querySelectorAll('.home-reveal');if(items.length&&!matchMedia('(prefers-reduced-motion:reduce)').matches){entrances=new IntersectionObserver(entries=>{for(const entry of entries)if(entry.isIntersecting){entry.target.classList.add('home-reveal-active');entrances.unobserve(entry.target);}},{threshold:.06});items.forEach(item=>entrances.observe(item));}
 };
 let timer;addEventListener('resize',()=>{clearTimeout(timer);timer=setTimeout(()=>applyZoom(latest?.user()?.id),150);});
 document.addEventListener('click',e=>{const scroll=e.target.closest('[data-home-scroll]');if(!scroll)return;e.preventDefault();document.getElementById(scroll.dataset.homeScroll)?.scrollIntoView({block:'start',behavior:matchMedia('(prefers-reduced-motion:reduce)').matches?'instant':'smooth'});});
})(window);

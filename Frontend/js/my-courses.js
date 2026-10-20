/* Figma 296:448. Programs select data; course rows retain the original artwork. */
(function(root){
  const assets='assets/my-courses/';
  const ico=d=>'<svg class="mc-ico" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">'+d+'</svg>';
  const icoOut=ico('<path d="M7 17 17 7M9 7h8v8"/>'),icoDown=ico('<path d="m6 9 6 6 6-6"/>'),icoCheck=ico('<path d="m5 12.5 4.5 4.5L19 7.5"/>');
  const pad=n=>String(n).padStart(2,'0');
  function coursesFor(state,p){
    const members=state.courses.filter(c=>c.programId===p.id&&c.status==='Published');
    return [...new Set([...(p.courseIds||[]),...members.map(c=>c.id)])].map(id=>members.find(c=>c.id===id)).filter(Boolean);
  }
  function programsFor(state){return state.programs.filter(p=>p.published);}
  function key(uid){return 'skillup.learning.program.'+uid;}
  function progressRing(value,title){
    const count=28,filled=Math.round(value*count/100),point=(r,d)=>{const a=d*Math.PI/180;return [(50+r*Math.cos(a)).toFixed(3),(50+r*Math.sin(a)).toFixed(3)].join(' ');};
    const segments=Array.from({length:count},(_,i)=>{const start=-90+i*360/count+1,end=-90+(i+1)*360/count-1;return '<path d="M '+point(48,start)+' A 48 48 0 0 1 '+point(48,end)+' L '+point(33,end)+' A 33 33 0 0 0 '+point(33,start)+' Z" fill="'+(i<filled?'#f4f4f5':'#303033')+'"/>';}).join('');
    return '<span class="mc-task-ring" role="progressbar" aria-label="'+title+' completion" aria-valuemin="0" aria-valuemax="100" aria-valuenow="'+value+'"><svg viewBox="0 0 100 100" aria-hidden="true">'+segments+'</svg><strong>'+value+'<sup>%</sup></strong></span>';
  }
  const shortDate=value=>{if(!value)return '—';const d=new Date(String(value).slice(0,10)+'T12:00:00');return Number.isNaN(d.getTime())?'—':d.toLocaleDateString('en',{month:'short',day:'numeric',year:'numeric'});};
  function courseSummary(c,e,value,href,action,esc){
    const status=!e?'Not enrolled':value===100?'Completed':value?'In Progress':'Not started',completed=c.lessons.filter(l=>e?.completed.includes(l.id)).length;
    return '<section class="mc-course-summary" aria-label="'+esc(c.title)+' progress"><div class="mc-market-accent"><div class="mc-market-marks" aria-hidden="true"><span>'+ico('<path d="M5 5h6c1 0 1.5 1 1.5 1S13 5 14 5h5v14h-5c-1 0-1.5 1-1.5 1s-.5-1-1.5-1H5Z"/><path d="M12.5 6v14"/>')+'</span><span>'+ico('<path d="m6 12 4 4 8-8"/>')+'</span></div><strong>'+value+'<sup>%</sup></strong><p>Completed</p></div><div class="mc-market-main"><header><h3>'+esc(c.title)+'</h3><div class="mc-market-tags"><span>'+status+'</span><span>'+completed+'/'+c.lessons.length+'</span></div></header><p class="mc-market-subtitle">'+esc(c.difficulty||'Course')+' · '+c.lessons.length+' activities</p><p class="mc-market-date"><span>Due date</span><time datetime="'+esc(c.deadline||'')+'">'+shortDate(c.deadline)+'</time></p><footer><a href="#/course/'+encodeURIComponent(c.id)+'" class="mc-market-details" aria-label="Course details: '+esc(c.title)+'">'+ico('<rect x="5" y="5" width="4" height="4" rx="1"/><rect x="15" y="5" width="4" height="4" rx="1"/><rect x="5" y="15" width="4" height="4" rx="1"/><rect x="15" y="15" width="4" height="4" rx="1"/>')+'</a><a class="mc-market-action" href="#/'+href+'" aria-label="'+action+': '+esc(c.title)+'" title="'+action+'">'+icoOut+'</a></footer></div></section>';
  }
  function courseRows(ctx,p){
    const {state,user,esc}=ctx,courses=coursesFor(state,p);
    return courses.map((c,i)=>{
      const e=state.enrollments.find(e=>e.userId===user().id&&e.courseId===c.id),value=SkillData.progress(c,e),next=e&&c.lessons.find(l=>!e.completed.includes(l.id));
      const art=c.artwork||assets+'computer-kit-0'+(i%5+1)+'.webp';
      const action=e?(value===100?'Review course':'Continue learning'):'View course';
      const href=next?'lesson/'+encodeURIComponent(c.id)+'/'+encodeURIComponent(next.id):'course/'+encodeURIComponent(c.id);
      return `<article class="mc-course" data-course-id="${esc(c.id)}"><div class="mc-course-metrics"><div class="mc-course-index"><span class="mc-number">${String(i+1).padStart(2,'0')}</span></div><div class="mc-course-gauge">${progressRing(value,esc(c.title))}</div></div><a class="mc-course-art" href="#/course/${encodeURIComponent(c.id)}" aria-label="View course: ${esc(c.title)}"><img src="${esc(art)}" alt="" loading="eager"></a>${courseSummary(c,e,value,href,action,esc)}</article>`;
    }).join('')||'<div class="mc-empty"><h2>No courses here yet</h2><p>Published courses for this program will appear here.</p><a class="button" href="#/catalog">Explore courses</a></div>';
  }
  function panel(ctx,p){const {state,esc}=ctx;return `<header class="mc-panel-header"><div><small>Selected program</small><h2 id="mc-program-title">${esc(p.name)}</h2></div></header><div class="mc-course-scroll" tabindex="0" aria-label="Courses in ${esc(p.name)}">${courseRows(ctx,p)}</div>`;}
  function programPrint(name,esc){const words=name.split(/\s+/),cut=name.includes('&')?name.indexOf('&')+1:0;let top,bottom;if(cut){top=name.slice(0,cut).trim();bottom=name.slice(cut).trim();}else{const mid=Math.ceil(words.length/2);top=words.slice(0,mid).join(' ');bottom=words.slice(mid).join(' ');}return '<span class="mc-program-name" aria-hidden="true"><span class="mc-print-top">'+esc(top)+'</span><span class="mc-print-bottom">'+esc(bottom)+'</span></span>';}
  function page(ctx){
    const {state,user,esc}=ctx,programs=programsFor(state),stored=sessionStorage.getItem(key(user().id));
    const p=programs.find(p=>p.id===stored)||programs.find(p=>coursesFor(state,p).some(c=>state.enrollments.some(e=>e.courseId===c.id&&e.userId===user().id)))||programs[0];
    if(!p)return '<section class="my-courses mc-empty"><h1 id="page-title" tabindex="-1"><img class="mc-pixel-title" src="assets/my-courses/my-course-pixel.png" alt="My course"></h1><h2>No programs available yet</h2><p>Your learning programs will appear here once published.</p><a href="#/catalog" class="button">Explore courses</a></section>';
    sessionStorage.setItem(key(user().id),p.id);
    return `<section class="my-courses" data-figma-node="296:448"><header class="mc-page-header"><h1 id="page-title" tabindex="-1"><img class="mc-pixel-title" src="assets/my-courses/my-course-pixel.png" alt="My course"></h1></header><div class="mc-layout"><aside class="mc-programs" aria-label="Programs"><div class="mc-program-scroll" role="tablist" aria-label="Choose a program" aria-orientation="vertical" tabindex="0">${programs.map((x,i)=>`<button class="mc-program" id="mc-tab-${i}" role="tab" aria-selected="${x.id===p.id}" aria-controls="mc-courses-panel" tabindex="${x.id===p.id?'0':'-1'}" data-program="${esc(x.id)}" aria-label="${esc(x.name)}"><img src="${assets}dev-kit-0${i%5+1}.webp" alt=""><span class="mc-program-caption">${esc(x.name)}</span></button>`).join('')}</div></aside><section class="mc-courses-panel" id="mc-courses-panel" role="tabpanel" aria-labelledby="mc-tab-${programs.indexOf(p)}">${panel(ctx,p)}</section></div></section>`;
  }
  let context=null;
  function railAxis(rail){return rail.scrollWidth>rail.clientWidth+1;}
  function centre(rail,card,behavior){const across=railAxis(rail);rail.scrollTo(across?{left:card.offsetLeft-(rail.clientWidth-card.offsetWidth)/2,behavior}:{top:card.offsetTop-(rail.clientHeight-card.offsetHeight)/2,behavior});}
  function syncIndex(rail){const cards=[...rail.querySelectorAll('.mc-program')],out=rail.closest('.mc-programs')?.querySelector('[data-mc-index]');if(!cards.length||!out)return;const across=railAxis(rail),mid=across?rail.scrollLeft+rail.clientWidth/2:rail.scrollTop+rail.clientHeight/2;let best=0,gap=Infinity;cards.forEach((c,i)=>{const d=Math.abs((across?c.offsetLeft+c.offsetWidth/2:c.offsetTop+c.offsetHeight/2)-mid);if(d<gap){gap=d;best=i;}});out.textContent=pad(best+1);}
  root.SkillMyCourses=ctx=>{context=ctx;setTimeout(()=>{const rail=document.querySelector('.my-courses .mc-program-scroll'),card=rail&&rail.querySelector('[aria-selected=true]');if(card)centre(rail,card,'instant');if(rail)syncIndex(rail);},0);return page(ctx);};
  let ticking=false;document.addEventListener('scroll',e=>{const rail=e.target;if(!(rail instanceof Element)||!rail.matches('.my-courses .mc-program-scroll')||ticking)return;ticking=true;requestAnimationFrame(()=>{ticking=false;syncIndex(rail);});},{capture:true,passive:true});
  document.addEventListener('click',event=>{
    const b=event.target.closest('.my-courses [data-program]');if(!b||!context)return;
    const p=programsFor(context.state).find(p=>p.id===b.dataset.program);if(!p)return;
    sessionStorage.setItem(key(context.user().id),p.id);
    document.querySelectorAll('.mc-program').forEach(n=>{n.setAttribute('aria-selected',String(n===b));n.tabIndex=n===b?0:-1;});
    const target=document.getElementById('mc-courses-panel');target.setAttribute('aria-labelledby',b.id);target.innerHTML=panel(context,p);
    centre(b.parentElement,b,matchMedia('(prefers-reduced-motion:reduce)').matches?'instant':'smooth');
  });
  document.addEventListener('keydown',event=>{
    const b=event.target.closest('.mc-program');if(!b||!['ArrowDown','ArrowUp','Home','End'].includes(event.key))return;
    event.preventDefault();const tabs=[...b.closest('[role=tablist]').querySelectorAll('.mc-program')],i=tabs.indexOf(b);
    const next=tabs[event.key==='Home'?0:event.key==='End'?tabs.length-1:(i+(event.key==='ArrowDown'?1:-1)+tabs.length)%tabs.length];next.focus({preventScroll:true});next.click();
  });
})(globalThis);

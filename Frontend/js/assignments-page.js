/* Instructor assignments (Program Leader): one card per course in the same row-and-pill language as the student cards.
   Reads only existing records; "Change assignment" keeps using the existing assign action. */
(function(root){
  'use strict';
  const tones={DB:'#16a34a','C#':'#2563eb',API:'#f97316',DS:'#0d9488'};
  const svg=p=>'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">'+p+'</svg>';
  const ic={
    user:svg('<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>'),
    people:svg('<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6 6 0 0 1 3.5 6"/>'),
    chart:svg('<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>'),
    list:svg('<path d="M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01"/>'),
    cal:svg('<rect x="4" y="5" width="16" height="15" rx="2"/><path d="M4 10h16M9 3v4M15 3v4"/>'),
    edit:svg('<path d="M4 20h4L19 9l-4-4L4 16Z"/>'),
    mail:svg('<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/>')
  };
  function page(ctx){
    const {state,esc,person,ownCourses,date,btn,link}=ctx,courses=ownCourses();
    const initials=n=>esc(String(n||'?').split(' ').map(x=>x[0]).slice(0,2).join(''));
    const rows=courses.map(c=>{
      const program=state.programs.find(p=>p.id===c.programId),instructor=c.instructorId?person(c.instructorId):null,es=state.enrollments.filter(e=>e.courseId===c.id);
      const completion=es.length?Math.round(es.reduce((n,e)=>n+SkillData.progress(c,e),0)/es.length):0,questions=c.quiz?.questions?.length||0;
      const status=c.status||'Draft',live=status==='Published',tone=tones[String(c.icon||'').toUpperCase()]||'#7c3aed';
      return {c,program,instructor,es,completion,questions,status,live,tone};
    });
    const people=new Set(rows.map(r=>r.c.instructorId).filter(Boolean)).size,unassigned=rows.filter(r=>!r.instructor).length;
    const card=r=>{const {c,program,instructor,es,completion,questions,status,live,tone}=r;
      return `<article class="asg-card" data-asg-search="${esc((c.title+' '+(program?.name||'')+' '+(instructor?.name||'unassigned')).toLowerCase())}">
<header><span class="asg-glyph" style="--tone:${tone}" aria-hidden="true">${esc(String(c.icon||c.title).slice(0,2).toUpperCase())}</span><div><h2>${esc(c.title)}</h2><p>${esc(program?.name||'No program')} · ${esc(c.difficulty||'')}</p></div><span class="asg-status ${live?'is-live':status==='Pending review'?'is-wait':''}">${esc(status)}</span></header>
<dl class="asg-rows">
<div><dt>${ic.user}Instructor</dt><dd>${instructor?`<span class="asg-chip"><span class="asg-avatar">${initials(instructor.name)}</span>${esc(instructor.name)}</span>`:'<span class="asg-chip is-warn">Unassigned</span>'}</dd></div>
<div><dt>${ic.people}Learners</dt><dd><span class="asg-chip">${es.length} enrolled</span></dd></div>
<div><dt>${ic.chart}Completion</dt><dd><span class="asg-meter" role="img" aria-label="${completion}% average completion"><i style="--p:${completion}%"></i></span><b>${completion}%</b></dd></div>
<div><dt>${ic.list}Activities</dt><dd><span class="asg-chip">${c.lessons.length} · ${questions} ${questions===1?'question':'questions'}</span></dd></div>
<div><dt>${ic.cal}Due date</dt><dd><span class="asg-chip">${esc(c.deadline?date(c.deadline):'Not set')}</span></dd></div>
</dl>
<footer>${btn('Change assignment','assign',{id:c.id},'asg-primary')}${link('View course','course-edit/'+encodeURIComponent(c.id),'asg-secondary')}${instructor?link('Message','messages/'+encodeURIComponent(c.instructorId),'asg-secondary'):''}</footer></article>`;};
    return `<section class="asg-page" aria-labelledby="page-title"><header class="asg-head"><div><h1 id="page-title" tabindex="-1">Instructor assignments</h1><p>Keep each course connected to the right instructor.</p></div><label class="asg-search"><span class="sr-only">Search assignments</span><input type="search" id="asg-search" placeholder="Search courses or instructors…" autocomplete="off"></label></header>
<div class="asg-summary"><span><b>${rows.length}</b> courses</span><span><b>${people}</b> ${people===1?'instructor':'instructors'}</span><span class="${unassigned?'is-warn':''}"><b>${unassigned}</b> unassigned</span></div>
${rows.length?`<div class="asg-grid" id="asg-grid">${rows.map(card).join('')}</div><p class="asg-empty" id="asg-empty" hidden>No matching courses or instructors.</p>`:'<div class="asg-empty"><h2>No courses to assign yet</h2><p>Courses in your programs will appear here.</p></div>'}</section>`;
  }
  if(typeof document!=='undefined')document.addEventListener('input',e=>{
    if(e.target.id!=='asg-search')return;const q=e.target.value.trim().toLowerCase();let n=0;
    document.querySelectorAll('.asg-card').forEach(c=>{const show=!q||c.dataset.asgSearch.includes(q);c.hidden=!show;if(show)n++;});
    const none=document.getElementById('asg-empty');if(none)none.hidden=n>0;
  });
  root.SkillAssignmentsPage=page;
})(globalThis);

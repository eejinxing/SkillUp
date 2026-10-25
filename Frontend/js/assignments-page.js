/* Program Leader assignment cards. Course data and the existing assign action remain the source of truth. */
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
    mail:svg('<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/>'),
    book:svg('<rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 7h6M9 11h6M9 15h4"/>'),
    lab:svg('<path d="M9 3h6M10 3v7L5 19a1 1 0 0 0 1 2h12a1 1 0 0 0 1-2l-5-9V3M8 15h8"/>'),
    quiz:svg('<path d="m6 12 4 4 8-8"/>'),
    status:svg('<circle cx="12" cy="12" r="8"/><path d="m8 12 3 3 5-6"/>'),
    chevron:svg('<path d="m8 14 4-4 4 4"/>')
  };
  function page(ctx){
    const {state,esc,person,ownCourses,date,btn,link}=ctx;
    const initials=n=>esc(String(n||'?').split(' ').filter(Boolean).map(x=>x[0]).slice(0,2).join(''));
    const rows=ownCourses().map(c=>{
      const program=state.programs.find(p=>p.id===c.programId),instructor=c.instructorId?person(c.instructorId):null,es=state.enrollments.filter(e=>e.courseId===c.id);
      const completion=es.length?Math.round(es.reduce((n,e)=>n+SkillData.progress(c,e),0)/es.length):0;
      return {c,program,instructor,es,completion,questions:c.quiz?.questions?.length||0,status:c.status||'Draft',tone:tones[String(c.icon||'').toUpperCase()]||'#7c3aed'};
    });
    const row=(icon,label,value)=>`<div><dt>${ic[icon]}${label}</dt><dd>${value}</dd></div>`;
    const chip=(text,cls='')=>`<span class="asg-chip ${cls}" title="${esc(text)}"><span class="asg-chip-text">${esc(text)}</span></span>`;
    const card=r=>{
      const {c,program,instructor,es,completion,questions,status,tone}=r,lessons=c.lessons||[];
      const segments=Array.from({length:10},(_,i)=>`<i style="--fill:${Math.min(100,Math.max(0,completion*10-i*100))}%"></i>`).join('');
      const next=!instructor?'Assign an instructor to manage this course and support its learners.':status==='Pending review'?'This course is awaiting publication review. Open the course to check its content.':status==='Draft'?'Review the course with its instructor before submitting it for publication.':status==='Hidden'?'This course is hidden from learners. Open the course to review its availability.':`Contact ${instructor.name} for a course update, or change the assignment below.`;
      return `<details class="asg-card" open data-asg-course="${esc(c.id)}" data-asg-search="${esc([c.title,program?.name,instructor?.name||'unassigned',instructor?.email].filter(Boolean).join(' ').toLowerCase())}">
<summary class="asg-card-head"><span class="asg-face" aria-hidden="true">${initials(instructor?.name)}<span class="asg-course-badge" style="--tone:${tone}">${esc(String(c.icon||'UP').slice(0,2))}</span></span><span class="asg-heading"><span class="asg-title" title="${esc(c.title)}">${esc(c.title)}</span><span class="asg-subtitle" title="${esc((program?.name||'No program')+' · '+(instructor?.name||'Unassigned'))}">${esc(program?.name||'No program')} · ${esc(instructor?.name||'Unassigned')}</span></span><span class="asg-ring ${completion===100?'is-complete':''}" style="--progress:${completion}%" role="img" aria-label="${completion}% average completion"><b>${completion}%</b></span><span class="asg-toggle">${ic.chevron}</span></summary>
<div class="asg-card-body"><dl class="asg-rows">
${row('user','Instructor',instructor?`<span class="asg-chip"><span class="asg-avatar">${initials(instructor.name)}</span><span class="asg-chip-text">${esc(instructor.name)}</span></span>`:chip('Unassigned','is-warn'))}
${row('mail','Email',chip(instructor?.email||'Not assigned'))}
${row('book','Course',`<span class="asg-chip"><span class="asg-course-dot" style="--tone:${tone}">${ic.book}</span><span class="asg-chip-text" title="${esc(c.title)}">${esc(c.title)}</span></span>`)}
${row('people','Learners',chip(es.length+' enrolled'))}
${row('chart','Completion',`<span class="asg-meter ${completion===100?'is-complete':''}" role="img" aria-label="${completion}% average completion">${segments}</span><b class="asg-percent">${completion}%</b>`)}
${row('list','Activities',chip(lessons.length+' '+(lessons.length===1?'activity':'activities')+' · '+questions+' '+(questions===1?'question':'questions')))}
${row('cal','Due date',chip(c.deadline?date(c.deadline):'Not set'))}
${row('status','Status',chip(status,status==='Published'?'is-live':status==='Pending review'?'is-warn':''))}
</dl><section class="asg-activities" aria-label="Course activities"><h3>Activities</h3><div class="asg-activity-list">${lessons.map(l=>`<span class="asg-activity" title="${esc(l.title)} · ${esc(l.type||'article')}"><span class="asg-activity-icon is-${l.type==='lab'?'lab':l.type==='quiz'?'quiz':'article'}">${ic[l.type==='lab'?'lab':l.type==='quiz'?'quiz':'book']}</span>${esc(l.title)}</span>`).join('')||'<span class="asg-muted">No activities added yet.</span>'}</div></section><section class="asg-next"><h3>Next step</h3><p>${esc(next)}</p></section></div>
<footer class="asg-footer"><strong>Follow up?</strong><div>${link('View','course-edit/'+encodeURIComponent(c.id),'asg-view')}${instructor?link('Message','messages/'+encodeURIComponent(instructor.id),'asg-action'):''}${btn('Change assignment','assign',{id:c.id},'asg-action')}</div></footer></details>`;
    };
    const people=new Set(rows.map(r=>r.c.instructorId).filter(Boolean)).size,unassigned=rows.filter(r=>!r.instructor).length;
    return `<section class="asg-page" aria-labelledby="page-title"><header class="asg-head"><div><h1 id="page-title" tabindex="-1">Instructor assignments</h1><p>Keep each course connected to the right instructor.</p></div><label class="asg-search"><span class="sr-only">Search assignments</span><input type="search" id="asg-search" placeholder="Search courses or instructors…" autocomplete="off"></label></header><div class="asg-summary"><span><b>${rows.length}</b> courses</span><span><b>${people}</b> ${people===1?'instructor':'instructors'}</span><span class="${unassigned?'is-warn':''}"><b>${unassigned}</b> unassigned</span></div>${rows.length?`<div class="asg-grid" id="asg-grid">${rows.map(card).join('')}</div><p class="asg-empty" id="asg-empty" hidden>No matching courses or instructors.</p>`:'<div class="asg-empty"><h2>No courses to assign yet</h2><p>Courses in your programs will appear here.</p></div>'}</section>`;
  }
  if(typeof document!=='undefined')document.addEventListener('input',e=>{
    if(e.target.id!=='asg-search')return;
    const q=e.target.value.trim().toLowerCase();let n=0;
    document.querySelectorAll('.asg-card').forEach(c=>{const show=!q||c.dataset.asgSearch.includes(q);c.hidden=!show;if(show)n++;});
    const none=document.getElementById('asg-empty');if(none)none.hidden=n>0;
  });
  root.SkillAssignmentsPage=page;
})(globalThis);

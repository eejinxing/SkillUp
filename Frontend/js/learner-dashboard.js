/* Registered learner home. All values come from the shared SkillAPI workspace. */
window.SkillLearnerDashboard=function(ctx){
  const {state,user,esc,person,date}=ctx,me=user(),mine=state.enrollments.filter(e=>e.userId===me.id);
  const enrolled=mine.map(e=>state.courses.find(c=>c.id===e.courseId&&c.status==='Published')).filter(Boolean);
  const available=state.courses.filter(c=>c.status==='Published'),paths=[...enrolled,...available.filter(c=>!enrolled.includes(c))].slice(0,2);
  const resumes=[...enrolled].sort((a,b)=>(mine.find(e=>e.courseId===b.id)?.lastActive||'').localeCompare(mine.find(e=>e.courseId===a.id)?.lastActive||''));
  const tiles=[...resumes,...available.filter(c=>!resumes.includes(c))].slice(0,3);
  const done=enrolled.reduce((sum,c)=>sum+c.lessons.filter(l=>mine.find(e=>e.courseId===c.id).completed.includes(l.id)).length,0),total=enrolled.reduce((sum,c)=>sum+c.lessons.length,0),completion=total?Math.round(done/total*100):0,level=Math.floor(done/5)+1;
  const next=c=>c?.lessons.find(l=>!mine.find(e=>e.courseId===c.id)?.completed.includes(l.id));
  const target=c=>!c?'catalog':mine.some(e=>e.courseId===c.id)&&next(c)?'lesson/'+c.id+'/'+next(c).id:'course/'+c.id;
  const recentAi=state.ai.filter(a=>a.userId===me.id).at(-1),inbox=state.messages.filter(m=>m.to===me.id),last=inbox.at(-1);
  const announcements=state.announcements.filter(a=>enrolled.some(c=>c.id===a.courseId)),primary=resumes[0];
  const updates=[announcements[0]?{label:'Course update',title:announcements[0].title,route:'course/'+announcements[0].courseId,tag:'New'}:{label:'Welcome',title:'Find your first course',route:'catalog',tag:'Start'},
    {label:'Next activity',title:next(primary)?.title||'Explore a new topic',route:target(primary),tag:'Next'},
    {label:'Coming up',title:primary?.deadline?'Due '+date(primary.deadline):'Set your learning pace',route:primary?'course/'+primary.id:'catalog',tag:'Plan'}];
  const global=(state.globalMessages||[]).slice(-4),session=state.sessions.find(s=>enrolled.some(c=>c.id===s.courseId)),joined=session?.attendees.includes(me.id);
  const safe=s=>esc(s??''),formatTime=d=>d?new Date(d).toLocaleTimeString('en-GB',{hour:'2-digit',minute:'2-digit'}):'';
  const data={GREETING:'<span>Hi, '+safe(me.name)+'</span><br><span>Your Dashboard</span>',COMPLETION:completion+'%',ACTIVITIES:done+' / '+total+' activities',PROGRESS_LABEL:'',ENGAGE:'Your week',AI_PROMPT:recentAi?safe(recentAi.response):'Ready for your next question?',
    AI_FORM:'<form data-form="ai" class="lh-ai-form"><label class="sr-only" for="dashboard-ai">Ask your AI companion</label><input id="dashboard-ai" name="text" required maxlength="500" placeholder="Ask your companion…"><button type="submit" aria-label="Send question">↗</button><p class="form-error" role="alert"></p></form>',
    CHAT_PREVIEW:'<a href="#/messages/'+safe(last?.from||'u2')+'" aria-label="Open latest message">'+safe(last?.text.slice(0,38)||'Ask your instructor')+'</a>',CHAT_LINK:'<a href="#/messages">Open inbox ↗</a>',
    GLOBAL_FORM:'<form data-form="globalMessage" class="lh-global-form"><label for="global-message">Message Global</label><textarea id="global-message" name="text" required maxlength="500" rows="2" placeholder="Share a question or a learning win…"></textarea><div><small>Community · '+(state.globalMessages||[]).length+' messages</small><button type="submit">Send ↗</button></div><p class="form-error" role="alert"></p></form>',
    CHALLENGE:'Keep your momentum',CHALLENGE_DETAIL:done+' '+(done===1?'activity':'activities')+' completed',CHALLENGE_LABEL:'Learning level',LEVEL:level,LEVEL_LABEL:'Level',SESSION_HOST:safe(session?person(session.hostId).name:'Skill UP'),SESSION_ROLE:session?'Instructor':'Community',SESSION_TITLE:safe(session?.title||'Find your next learning session'),SESSION_TIME:session?safe(date(session.startsAt)):'',SESSION_JOIN:session?(joined?'Joined':'Join Now'):'Explore',HIT_LINKS:''};
  updates.forEach((u,i)=>{data['UPDATE_TAG'+i]=safe(u.tag);data['UPDATE_TITLE'+i]=safe(u.title);data['UPDATE_LABEL'+i]=safe(u.label);});
  [0,1,3,4].forEach((slot,i)=>{const m=global[i],author=m?person(m.userId):me;data['GLOBAL_NAME'+slot]=safe(m?author.name:'Skill UP');data['GLOBAL_ROLE'+slot]=safe(m?author.role:'Community');data['GLOBAL_TEXT'+slot]=safe(m?.text||'Share what you are learning today.');data['GLOBAL_TIME'+slot]=safe(formatTime(m?.createdAt));});
  for(let i=0;i<3;i++){const c=tiles[i],e=mine.find(e=>e.courseId===c?.id);data['RESUME_TITLE'+i]=safe(c?.title||'Your next course');data['RESUME_DETAIL'+i]=safe(e?(next(c)?.title||'Completed · Review course'):'Explore this course');}
  paths.forEach((c,i)=>data['PATH_TITLE'+i]=safe(c.title.startsWith('C#')?'C#':c.title.split(' ')[0].toUpperCase()));for(let i=paths.length;i<2;i++)data['PATH_TITLE'+i]='EXPLORE';
  const hit=(route,label,left,top,width,height)=>'<a class="lh-hit" href="#/'+safe(route)+'" aria-label="'+safe(label)+'" style="left:'+left+'%;top:'+top+'%;width:'+width+'%;height:'+height+'%"></a>';
  data.HIT_LINKS=updates.map((u,i)=>hit(u.route,u.label+': '+u.title,26.9,13.8+i*5.22,22.5,5.22)).join('')+hit('learning','View completion details',1.29,13.43,11.91,9.1)+hit('learning','View your learning activity',13.65,13.43,11.91,9.1)+hit('leaderboard','View learning level and leaderboard',1.29,34.87,24.52,7.67)+paths.map((c,i)=>hit('course/'+c.id,'Open learning path: '+c.title,i===0?0:24.2,49.3,25.6,29.3)).join('')+tiles.map((c,i)=>hit(target(c),'Continue '+c.title,.87+i*31.25,82.5,29.13,15.5)).join('');
  let native=SkillLearnerTemplate.replace(/@@([A-Z_0-9]+)@@/g,(_,key)=>data[key]??'');
  native=native.replace('data-learner-action="join"',session?'data-action="joinSession" data-id="'+safe(session.id)+'"':'data-learner-action="sessions"');
  const error='<p class="form-error" role="alert"></p>';
  return '<div class="registered-home" data-figma-node="250:466" data-session-id="'+safe(session?.id||'')+'">'+SkillDashboardComponents({native,state,me,tiles,target,next,esc,person,date})+'</div>';
};
document.addEventListener('click',e=>{const b=e.target.closest('[data-learner-action]');if(!b)return;const root=b.closest('.registered-home');if(b.dataset.learnerAction==='close-global'){root.classList.add('is-global-closed');root.querySelector('.lh-reopen').focus();}if(b.dataset.learnerAction==='open-global'){root.classList.remove('is-global-closed');root.querySelector('[data-node-id="250:722"]').focus();}if(b.dataset.learnerAction==='sessions')location.hash='/sessions';});

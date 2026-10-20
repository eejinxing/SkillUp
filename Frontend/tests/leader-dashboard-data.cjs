const assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path'),ctx={window:null};ctx.window=ctx;vm.createContext(ctx);for(const f of ['seed','my-courses-data','leader-dashboard'])vm.runInContext(fs.readFileSync(path.join(__dirname,'../js/'+f+'.js'),'utf8'),ctx);const state=ctx.SkillData.seed();ctx.SkillProgramDemo.apply(state);const now=new Date('2026-10-17T12:00:00Z');let m=ctx.SkillLeaderData.build(state,'u3',now,14);assert.equal(m.programs.length,3);assert.equal(m.courses.length,8);assert.equal(m.active,3);assert.equal(m.completion,25);assert.equal(m.averageProgress,44);assert.equal(m.completed,1);assert.equal(m.rows.find(r=>r.c.id==='c1').value,58);assert.equal(m.rows.find(r=>r.c.id==='c2').value,0);assert.equal(m.rows.find(r=>r.c.id==='c3').value,null);assert.equal(m.pending.length,1);assert.equal(m.pending[0].courseId,'c4');assert.equal(m.series.length,14);assert.equal(m.series.reduce((n,d)=>n+d.value,0),1);assert.equal(m.contacts.length,4);
 state.courseRequests=[{id:'r1',courseId:'c4',courseTitle:'API Design',kind:'Create',requesterId:'u2',programId:'p1',status:'Pending review',createdAt:'2026-10-16T10:00:00Z'},{id:'r2',courseId:'demo-hardware',courseTitle:'Hardware',kind:'Delete',requesterId:'u2',programId:'demo-systems',status:'Pending review',createdAt:'2026-10-17T10:00:00Z'}];state.programs.push({id:'foreign',leaderId:'elsewhere'});state.courses.push({...state.courses[0],id:'foreign-course',programId:'foreign',status:'Pending review'});state.enrollments.push({id:'foreign-e',userId:'u7',courseId:'foreign-course',completed:[],lastActive:'2026-10-17'});state.attempts.push({courseId:'foreign-course',userId:'u7',createdAt:'2026-10-17T10:00:00Z'},{courseId:'c1',userId:'u1',createdAt:'2026-11-17T10:00:00Z'},{courseId:'c1',userId:'u1',createdAt:'invalid'},{courseId:'c1',userId:'u1',createdAt:'2026-10-02T10:00:00Z'});state.announcements.push({id:'foreign-notice',courseId:'foreign-course',createdAt:'2026-10-17'});m=ctx.SkillLeaderData.build(state,'u3',now,14);assert.equal(m.courses.length,8);assert.equal(m.active,3);assert.equal(m.pending.length,2);assert.equal(m.pending.filter(r=>r.courseId==='c4').length,1);assert.match(m.pending.find(r=>r.kind==='Delete').href,/approvals/);assert.equal(m.announcements.length,1);assert.equal(m.series.reduce((n,d)=>n+d.value,0),1);assert.equal(m.series.reduce((n,d)=>n+d.prior,0),1);for(const days of [1,7,24])assert.equal(ctx.SkillLeaderData.build(state,'u3',now,days).series.length,days);assert.equal(ctx.SkillLeaderData.build(state,'u3',now,99).days,14);m=ctx.SkillLeaderData.build(state,'nobody',now);assert.equal(m.courses.length,0);assert.equal(m.completion,0);assert.equal(m.active,0);assert.equal(m.pending.length,0);assert.equal(m.announcements.length,0);assert.equal(m.series.reduce((n,d)=>n+d.value,0),0);console.log('Program Leader data checks passed: scoped programs/courses/announcements, activity periods, unique active learners, completion and pending creation/deletion requests.');


const board=ctx.SkillLeaderData.reach(ctx.SkillLeaderData.build(state,'u3',now));
assert.equal(board.total,4);
assert.equal(board.courses.reduce((n,s)=>n+s.value,0),4);
assert.equal(board.courses.reduce((n,s)=>n+s.percent,0),100);
assert.equal(board.courses.find(s=>s.label==='MongoDB Management').percent,75);
assert.equal(board.learning.find(s=>s.label==='Completed').value,1);
assert.equal(board.publishing.reduce((n,s)=>n+s.value,0),8);
assert.equal(ctx.SkillLeaderData.reach(ctx.SkillLeaderData.build(state,'nobody',now)).courses.length,0);
const local=new Map();ctx.localStorage={getItem:key=>local.get(key)||null};
const nc={state,user:()=>({id:'u3'})};
state.messages.push({id:'late',from:'u2',to:'u3',text:'Future',createdAt:'2026-11-01',read:false});
let notes=ctx.SkillLeaderData.notifications(nc,ctx.SkillLeaderData.build(state,'u3',now),now);
assert.ok(!notes.some(n=>n.id==='msg-late'),'future notifications excluded');
assert.ok(!notes.some(n=>n.object==='foreign-course'),'other programs excluded');
assert.equal(notes.find(n=>n.id==='approval-r1').tab,'unread');
local.set('skillup.leader.read.u3',JSON.stringify(['approval-r1']));
notes=ctx.SkillLeaderData.notifications(nc,ctx.SkillLeaderData.build(state,'u3',now),now);
assert.equal(notes.find(n=>n.id==='approval-r1').tab,'read','pending request can be marked read without approving it');
local.set('skillup.leader.archived.u3',JSON.stringify(['approval-r1']));
assert.equal(ctx.SkillLeaderData.notifications(nc,ctx.SkillLeaderData.build(state,'u3',now),now).find(n=>n.id==='approval-r1').tab,'archived');
local.set('skillup.leader.archived.u3','{}');
assert.equal(ctx.SkillLeaderData.notifications(nc,ctx.SkillLeaderData.build(state,'u3',now),now).find(n=>n.id==='approval-r1').tab,'read','malformed saved data is tolerated');
assert.equal(state.courseRequests.find(r=>r.id==='r1').status,'Pending review','notification actions do not change request status');
console.log('Performance distribution and explicit notification read/archive state passed.');

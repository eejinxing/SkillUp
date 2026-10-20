/* Additional fictional learners so the leaderboard has a full field: enrolments, completed activities, quiz attempts
   (some within the current week) and activity history. One-time, additive migration; existing records are never changed. */
(function(root){
  const learners=[
    // id, name, username, location, [courseId, completed lesson count], quiz scores [courseId, score, daysAgo]
    ['lb-u1','Priya Nair','priya','Penang',[['c1',4],['c2',3],['c3',2]],[['c1',96,2],['c2',88,1],['c3',100,4]]],
    ['lb-u2','Lucas Martin','lucas','Kuala Lumpur',[['c1',4],['c2',3]],[['c1',84,1],['c2',92,3]]],
    ['lb-u3','Hana Sato','hana','Johor Bahru',[['c1',4],['c3',2],['demo-data',1]],[['c1',78,5],['c3',90,0]]],
    ['lb-u4','Omar Haddad','omar','Ipoh',[['c2',3],['c3',1]],[['c2',74,2]]],
    ['lb-u5','Sofia Rossi','sofia','Melaka',[['c1',4],['demo-models',1]],[['c1',67,6]]],
    ['lb-u6','Arjun Mehta','arjun','Kuching',[['c1',2],['c2',2],['c3',1]],[]],
    ['lb-u7','Chloe Wong','chloe','Kota Kinabalu',[['c3',2],['demo-data',1]],[['c3',81,1]]]
  ];
  const daysAgo=n=>{const d=new Date();d.setDate(d.getDate()-n);d.setHours(10+n%6,15,0,0);return d.toISOString();};
  function apply(db){
    db.migrations ||= {};
    if(db.migrations.leaderboardDemoV1)return db;
    const added=[];
    learners.forEach(([id,name,username,location,enrols,quizzes],i)=>{
      if(db.users.some(u=>u.id===id||u.username===username))return;
      const user={id,name,email:username+'@skillup.demo',username,role:'Learner',status:'Active',password:'Demo1234!',location,bio:'Demo learner.'};
      db.users.push(user);added.push(user);
      for(const [cid,count] of enrols){
        const c=db.courses.find(c=>c.id===cid);if(!c||db.enrollments.some(e=>e.userId===id&&e.courseId===cid))continue;
        const completed=c.lessons.slice(0,Math.min(count,c.lessons.length)).map(l=>l.id);
        db.enrollments.push({id:'e-'+id+'-'+cid,userId:id,courseId:cid,completed,joinedAt:daysAgo(20+i).slice(0,10),lastActive:daysAgo(i%5).slice(0,10),minutes:c.lessons.filter(l=>completed.includes(l.id)).reduce((n,l)=>n+(l.minutes||0),0)});
      }
      for(const [cid,score,ago] of quizzes){
        const c=db.courses.find(c=>c.id===cid),lesson=c?.lessons.find(l=>l.type==='quiz');if(!lesson)continue;
        db.attempts.push({id:'a-'+id+'-'+cid,userId:id,courseId:cid,lessonId:lesson.id,type:'quiz',score,answers:[],wrongTopics:[],createdAt:daysAgo(ago)});
      }
    });
    // Existing workspaces already have an activity log; give the new learners theirs too.
    if(db.xpLog&&added.length&&root.SkillXpHistory)db.xpLog.push(...root.SkillXpHistory.build({users:added}));
    db.migrations.leaderboardDemoV1=true;
    return db;
  }
  const originalSeed=SkillData.seed;
  SkillData.seed=()=>apply(originalSeed());
  root.SkillLeaderboardDemo={apply};
})(globalThis);

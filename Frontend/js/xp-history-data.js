/* Fictional demo XP history for learner accounts, so the dashboard's XP chart, total and level have a year of activity.
   Entries are generated deterministically per learner (same data on every reload) and dated relative to today. */
(function(root){
  const sources=['Practice quiz','Lesson review','Lab exercise','Reading','Discussion reply'];
  function rng(seed){return()=>{seed|=0;seed=seed+0x6D2B79F5|0;let t=Math.imul(seed^seed>>>15,1|seed);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};}
  // Practice scores drift upward over the year (55% → 95%), with a little noise; stable per entry id.
  function practiceScore(id,back){const h=[...id].reduce((n,c)=>(n*31+c.charCodeAt(0))>>>0,7);return Math.max(40,Math.min(100,Math.round(95-back/364*40+(h%15)-7)));}
  function ensureScores(log){for(const x of log||[])if(x.source==='Practice quiz'&&x.score==null){const back=Math.max(0,Math.round((Date.now()-new Date(x.day+'T12:00:00'))/864e5));x.score=practiceScore(x.id,back);}return log;}
  function history(user,index){
    const r=rng(97+index*131),log=[],today=new Date();
    for(let back=364;back>=1;back--){
      // Activity grows a little more frequent towards the present, like a learner building a habit.
      const chance=.2+.55*Math.pow(1-back/364,2);
      if(r()>chance)continue;
      const d=new Date(today);d.setDate(d.getDate()-back);
      const day=d.toISOString().slice(0,10);
      const source=sources[Math.floor(r()*sources.length)];log.push({id:'xp-'+user.id+'-'+day,userId:user.id,day,xp:15+Math.round(r()*8)*5,source,...(source==='Practice quiz'?{score:practiceScore('xp-'+user.id+'-'+day,back)}:{})});
    }
    return log;
  }
  function build(db){return (db.users||[]).filter(u=>u.role==='Learner').flatMap((u,i)=>history(u,i));}
  const originalSeed=SkillData.seed;
  SkillData.seed=()=>{const db=originalSeed();db.xpLog=build(db);return db;};
  root.SkillXpHistory={build,ensureScores};
})(globalThis);

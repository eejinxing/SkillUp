/* Writes Database/demo-seed.json from the browser demo fixtures (seed.js, my-courses-data.js, leaderboard-demo-data.js).
   The C# backend loads that file into an empty database on first start (Backend/DemoSeed.cs).
   Run from Frontend/: node scripts/export-demo-seed.cjs */
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
const ctx={console,Date,crypto:require('node:crypto').webcrypto};
vm.createContext(ctx);
for(const f of ['seed','my-courses-data','leaderboard-demo-data'])vm.runInContext(fs.readFileSync(path.join(__dirname,'../js/'+f+'.js'),'utf8'),ctx);
const seed=JSON.parse(JSON.stringify(ctx.SkillData.seed()));
delete seed.migrations;delete seed.xpLog;
const out=path.join(__dirname,'../../Database/demo-seed.json');
fs.writeFileSync(out,JSON.stringify(seed,null,1)+'\n');
console.log('Wrote '+path.relative(process.cwd(),out)+': '+seed.users.length+' users, '+seed.courses.length+' courses, '+seed.enrollments.length+' enrolments.');

const {spawnSync}=require('node:child_process'),path=require('node:path');
const tests=["domain","course-requests-domain","leader-applications-data","program-management-data","admin-activity-data","admin-dashboard-data","leader-dashboard-data","leaderboard-data","user-editor-data"];
for(const t of tests){const r=spawnSync(process.execPath,[path.join(__dirname,'../tests/'+t+'.cjs')],{stdio:'inherit'});if(r.status!==0)process.exit(r.status||1);}
console.log(tests.length+' domain suites passed.');

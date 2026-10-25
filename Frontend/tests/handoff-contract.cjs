const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict'),crypto=require('node:crypto');
(async()=>{
const calls=[],storage=new Map(),workspace={currentUser:null,users:[],courses:[]};let status=200,result=workspace;
const ctx={console,Date,setTimeout,clearTimeout,crypto:crypto.webcrypto,localStorage:{getItem:k=>storage.get(k)||null,setItem:(k,v)=>storage.set(k,v)},SkillRuntimeConfig:{mode:'http',baseUrl:'/school/api',latency:0},fetch:async(url,init)=>{calls.push({url,init});return{ok:status>=200&&status<300,status,json:async()=>result};}};
vm.createContext(ctx);for(const f of ['seed','api'])vm.runInContext(fs.readFileSync(path.join(__dirname,'../js/'+f+'.js'),'utf8'),ctx);
assert.equal(ctx.SkillAPI.config.mode,'http');assert.deepEqual(JSON.parse(JSON.stringify(await ctx.SkillAPI.request('state'))),workspace);
assert.equal(calls[0].url,'/school/api/workspace');assert.equal(calls[0].init.method,'GET');assert.equal(calls[0].init.credentials,'include');assert.ok(!('body' in calls[0].init));
result={data:{id:'result-id'},state:workspace};await ctx.SkillAPI.request('enquirySave',{subject:'Question',message:'Help'});
assert.equal(calls[1].url,'/school/api/commands/enquirySave');assert.equal(calls[1].init.method,'POST');assert.equal(JSON.parse(calls[1].init.body).subject,'Question');
status=403;result={detail:'Not allowed.'};await assert.rejects(ctx.SkillAPI.request('userSave',{}),e=>e.status===403&&e.message==='Not allowed.');assert.equal(calls.length,3);
console.log('HTTP adapter contract: configured base URL, GET workspace, JSON POST commands, credentials and error propagation passed.');
})().catch(e=>{console.error(e);process.exitCode=1;});

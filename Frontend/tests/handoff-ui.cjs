const {chromium}=require('playwright'),path=require('node:path'),fs=require('node:fs'),assert=require('node:assert/strict'),{pathToFileURL}=require('node:url');
(async()=>{const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{})});try{
const base=process.env.SKILLUP_BASE_URL||pathToFileURL(path.join(__dirname,'../index.html')).href,errors=[],roles=[];fs.mkdirSync(path.join(__dirname,'../.test-output'),{recursive:true});
for(const role of ['Learner','Instructor','Program Leader','Admin']){
const page=await browser.newPage({viewport:{width:1440,height:1000},reducedMotion:'reduce'});page.on('pageerror',e=>errors.push(role+': '+e.message));page.on('response',r=>{if(r.status()>=400)errors.push(role+': '+r.status()+' '+r.url());});
await page.goto(base);await page.locator('.home-refined').waitFor();await page.evaluate(async role=>{SkillAPI.config.latency=0;await SkillAPI.request('demo',{role});location.hash='/dashboard';},role);
await page.waitForFunction(cls=>document.body.classList.contains(cls),'role-'+role.toLowerCase().replaceAll(' ','-'));await page.locator('.skill-global-header').waitFor();
assert.equal(await page.locator('.gn-notifications,a[href^="#/announcements"],a[href^="#/notifications"]').count(),0);
await page.evaluate(()=>location.hash='/settings');await page.waitForFunction(()=>document.querySelector('main')?.textContent.includes('Account security'));
await page.evaluate(()=>location.hash='/announcements');await page.waitForFunction(()=>location.hash==='#/dashboard');
await page.evaluate(()=>location.hash='/notifications');await page.waitForFunction(()=>location.hash==='#/dashboard');
if(role==='Learner'){await page.locator('.lc-velo').waitFor();assert.equal(await page.locator('.vd-metrics>.vd-card').count(),3);await page.locator('[data-vd-filter]').selectOption('progress');assert.equal(await page.locator('[data-vd-course]:visible').count(),1);await page.locator('[data-vd-filter]').selectOption('all');await page.setViewportSize({width:1280,height:900});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));}
await page.screenshot({path:path.join(__dirname,'../.test-output',role.replaceAll(' ','-')+'.png'),fullPage:true});roles.push(role);await page.close();}
const page=await browser.newPage({viewport:{width:1280,height:900},reducedMotion:'reduce'});page.on('pageerror',e=>errors.push(e.message));await page.goto(base);await page.locator('.home-enquiry-form').waitFor();await page.evaluate(()=>SkillAPI.config.latency=0);
await page.locator('#home-enquiry-name').fill('Handoff Preview');await page.locator('#home-enquiry-email').fill('handoff@example.com');await page.locator('#home-enquiry-subject').fill('Backend handoff test');await page.locator('#home-enquiry-message').fill('This record exists only in this isolated test browser.');
assert.equal(await page.locator('.home-enquiry-ready').textContent(),'Ready to send.');await page.locator('.home-enquiry-submit').click();await page.locator('#dialog[open]').waitFor();assert.ok((await page.locator('#dialog').textContent()).includes('Enquiry saved'));
await page.locator('#dialog [data-action=close]').click();await page.locator('.home-enquiry-portal').click();await page.locator('.ep-entry').first().waitFor();assert.ok((await page.locator('.ep-entry').first().textContent()).includes('Backend handoff test'));await page.close();
assert.deepEqual(errors,[]);console.log(JSON.stringify({roles,notificationRemoval:true,desktopOnly:true,enquirySubmitAndPortal:true,pageErrors:errors},null,2));
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});

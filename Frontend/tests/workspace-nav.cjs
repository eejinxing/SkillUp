const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{}),args:['--use-fake-device-for-media-stream','--use-fake-ui-for-media-stream']});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1000},reducedMotion:'reduce'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(pathToFileURL(path.join(__dirname,'../index.html')).href);await page.locator('.skill-global-header').waitFor();
  for(const role of ['Learner','Instructor','Program Leader','Admin']){
   await page.evaluate(async role=>{SkillAPI.config.latency=0;await SkillAPI.request('demo',{role});location.hash='/dashboard';},role);
   await page.reload();await page.locator('.skill-global-header').waitFor();await page.evaluate(()=>SkillAPI.config.latency=0);
   assert.equal(await page.locator('.skill-global-header').count(),1,role+' has one shared navbar');
   assert.ok(await page.locator('.gn-links>a').count()>0);assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  }
  await page.evaluate(async()=>{await SkillAPI.request('demo',{role:'Learner'});location.hash='/lesson/c1/l2';});await page.locator('.answer-page').waitFor();
  await page.waitForFunction(()=>[...document.querySelectorAll('.answer-glass img')].every(i=>i.complete&&i.naturalWidth));
  assert.equal(await page.locator('.answer-glass img').count(),2);
  assert.equal(await page.locator('.answer-glass.answer-hint img').isVisible(),true);
  assert.equal(await page.locator('.answer-glass.answer-submit img').isVisible(),true);
  assert.ok((await page.locator('.answer-glass.answer-submit').boundingBox()).width>60);
  await page.getByRole('button',{name:'Hint',exact:true}).click();await page.locator('#dialog[open]').waitFor();await page.getByRole('button',{name:'Close dialog',exact:true}).click();
  await page.evaluate(()=>{window.previewMediaRequests=0;const get=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);navigator.mediaDevices.getUserMedia=c=>{window.previewMediaRequests++;return get(c);};});
  await page.getByRole('button',{name:'Test video call',exact:true}).click();await page.locator('#skill-incoming-call').waitFor();
  assert.equal(await page.evaluate(()=>window.previewMediaRequests),0,'ringing does not activate camera or microphone');
  assert.equal(await page.locator('#skill-call-dock').count(),0);
  await page.screenshot({path:'previews/incoming-video-call.png',fullPage:true});
  await page.getByRole('button',{name:'Decline',exact:true}).click();assert.equal(await page.locator('#skill-incoming-call').count(),0);assert.equal(await page.locator('.gn-capsule.is-calling').count(),0);
  await page.getByRole('button',{name:'Test video call',exact:true}).click();await page.getByRole('button',{name:'Accept video call',exact:true}).click();
  await page.waitForFunction(()=>document.querySelector('#skill-call-video')?.srcObject?.getVideoTracks().length===1);
  assert.equal(await page.locator('.gn-capsule.is-calling').count(),1);
  assert.equal(await page.locator('.gn-call-strip .cs-bar').count(),1);assert.equal(await page.locator('.cs-bar .cs-time').count(),2);assert.equal(await page.locator('.cs-rail .gn-playhead').count(),1);assert.equal(await page.locator('.cs-bar').evaluate(n=>getComputedStyle(n).borderTopLeftRadius),'999px');assert.equal(await page.locator('.call-self.is-live #skill-call-video').count(),1,'camera shows in the You tile');
  assert.equal(await page.locator('.call-demo-photo').isVisible(),true);assert.equal(await page.locator('.call-paper').count(),2);
  await page.getByRole('button',{name:'Open course outline',exact:true}).first().click();await page.locator('#skill-call-document[open]').waitFor();assert.match(await page.locator('#skill-call-document').textContent(),/MongoDB Management/);
  await page.getByRole('button',{name:'Close document',exact:true}).click();
  await page.getByRole('button',{name:'Hide summary',exact:true}).click();assert.equal(await page.locator('.call-documents').isVisible(),false);await page.getByRole('button',{name:'Show summary',exact:true}).click();assert.equal(await page.locator('.call-documents').isVisible(),true);
  await page.waitForFunction(()=>document.querySelector('.call-demo-photo').complete&&document.querySelector('.call-demo-photo').naturalWidth);
  await page.screenshot({path:'previews/incoming-video-call-accepted.png',fullPage:true});
  await page.locator('.skill-global-header').screenshot({path:'previews/schedule-navbar-reference.png'});
  await page.locator('#skill-call-dock').screenshot({path:'previews/video-call-reference.png'});
  await page.locator('#skill-call-dock [data-call-control=end]').click();assert.equal(await page.getByRole('button',{name:'Test video call',exact:true}).isEnabled(),true);
  await page.evaluate(async()=>{await SkillAPI.request('demo',{role:'Learner'});location.hash='/messages/u2';});await page.reload();await page.getByRole('button',{name:'Start video call preview'}).waitFor();
  await page.getByRole('button',{name:'Start video call preview'}).click();
  await page.waitForFunction(()=>document.querySelector('#skill-call-video')?.srcObject?.getVideoTracks().length===1);
  assert.equal(await page.locator('.gn-capsule.is-calling').count(),1);
  assert.match(await page.locator('.call-status').textContent(),/no remote connection/);
  await page.evaluate(()=>window.testCallTracks=document.querySelector('#skill-call-video').srcObject.getTracks());
  await page.getByRole('button',{name:'Mute microphone',exact:true}).click();assert.equal(await page.getByRole('button',{name:'Enable microphone',exact:true}).getAttribute('aria-pressed'),'false');
  await page.getByRole('button',{name:'Turn camera off',exact:true}).click();assert.equal(await page.locator('#skill-call-video').isVisible(),false);
  await page.getByRole('button',{name:'Turn camera on',exact:true}).click();assert.equal(await page.locator('#skill-call-video').isVisible(),true);
  await page.locator('#skill-call-notes').fill('Review aggregation pipelines');
  await page.evaluate(()=>location.hash='/dashboard');await page.locator('.learning-console,.learner-dashboard,.learner-home').first().waitFor({timeout:1000}).catch(()=>page.locator('.gn-links [aria-current=page]').waitFor());
  assert.equal(await page.locator('#skill-call-notes').inputValue(),'Review aggregation pipelines');assert.equal(await page.locator('#skill-call-video').evaluate(v=>v.srcObject.active),true);
  await page.getByRole('button',{name:'Minimize call',exact:true}).click();assert.equal(await page.locator('#skill-call-dock').isVisible(),false);await page.locator('.gn-call-strip button').first().click();assert.equal(await page.locator('#skill-call-dock').isVisible(),true);
  await page.screenshot({path:'previews/workspace-nav-call-desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);await page.screenshot({path:'previews/workspace-nav-call-mobile.png',fullPage:true});
  await page.locator('#skill-call-dock [data-call-control=end]').click();assert.equal(await page.locator('#skill-call-dock').count(),0);assert.equal(await page.locator('.gn-capsule.is-calling').count(),0);assert.equal(await page.evaluate(()=>window.testCallTracks.every(t=>t.readyState==='ended')),true);
  await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:'previews/workspace-nav-desktop.png',fullPage:true});
  await page.evaluate(()=>location.hash='/messages/u2');await page.getByRole('button',{name:'Start voice call preview'}).click();await page.waitForFunction(()=>document.querySelector('#skill-call-video')?.srcObject?.getAudioTracks().length===1);
  await page.evaluate(()=>window.testCallTracks=document.querySelector('#skill-call-video').srcObject.getTracks());await page.evaluate(async()=>{await SkillAPI.request('demo',{role:'Admin'});location.hash='/dashboard';});await page.locator('body.role-admin').waitFor();assert.equal(await page.locator('#skill-call-dock').count(),0);assert.equal(await page.evaluate(()=>window.testCallTracks.every(t=>t.readyState==='ended')),true);
  await page.evaluate(async()=>{await SkillAPI.request('demo',{role:'Learner'});location.hash='/messages/u2';});await page.getByRole('button',{name:'Start video call preview'}).waitFor();
  await page.evaluate(()=>{navigator.mediaDevices.getUserMedia=()=>Promise.reject(new DOMException('Denied','NotAllowedError'));});
  await page.getByRole('button',{name:'Start video call preview'}).click();await page.waitForFunction(()=>document.querySelector('.call-status')?.textContent.includes('Device access unavailable'));
  assert.equal(await page.locator('#skill-call-video').isVisible(),false);await page.locator('#skill-call-dock [data-call-control=end]').click();
  await page.locator('.gn-all-pages').click();await page.locator('#skill-edge-drawer[open]').waitFor();await page.keyboard.press('Escape');assert.equal(await page.locator('#skill-edge-drawer[open]').count(),0);
  assert.deepEqual(errors,[]);console.log('Shared navigation passed for visitor and all four roles; local video/audio, mute, camera, route persistence, notes, minimize/end, track cleanup and mobile layout passed.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});


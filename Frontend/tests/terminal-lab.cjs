const {chromium}=require('playwright');
const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const out=process.env.SKILLUP_TERMINAL_OUTPUT||path.join(__dirname,'../.test-output/terminal-lab');fs.mkdirSync(out,{recursive:true});
(async()=>{
 const base=process.env.SKILLUP_BASE_URL||require('url').pathToFileURL(path.join(__dirname,'../index.html')).href;
 const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{})}),errors=[],checks=[];
 try{
  const p=await browser.newPage({viewport:{width:1440,height:1000},reducedMotion:'reduce'});p.on('pageerror',e=>errors.push(e.message));
  await p.goto(base);await p.locator('.home-refined').waitFor();
  await p.evaluate(async()=>{SkillAPI.config.latency=0;await SkillAPI.request('demo',{role:'Learner'});location.hash='/lesson/c1/l2';});
  const lab=p.locator('.lab-shell'),editor=p.locator('#code-editor'),output=p.locator('#code-output');await lab.waitFor();
  assert.equal(await p.locator('.lab-sidebar').count(),0);assert.match(await p.locator('.shell-question').textContent(),/Find your first document/);
  assert.equal(await lab.locator('.shell-segments i').count(),48);assert.equal(await p.locator('.answer-submit img').count(),2);assert.equal(await p.locator('.answer-hint img').count(),2);assert.equal(await lab.locator('.answer-task-label').textContent(),'Task 1');assert.equal(await p.locator('form.answer-task .answer-task-label').textContent(),'Task 2');
  const execute=p.locator('[data-action=runCode]');
  async function run(code,status='success'){await editor.fill(code);await execute.click();await p.waitForFunction(status=>document.querySelector('.lab-shell').dataset.execution===status,status);assert.equal(await execute.isEnabled(),true);assert.equal(await lab.getAttribute('aria-busy'),'false');}
  await run("console.log('Hello, Skill UP!'); console.log({completed: 1});");assert.equal(await output.textContent(),'Hello, Skill UP!\n{"completed":1}');checks.push('multiline JavaScript output');
  await lab.screenshot({path:path.join(out,'terminal-1440.png')});
  await run("console.log('before error'); throw new Error('Try again');",'error');assert.equal(await output.textContent(),'before error\nError: Try again');
  await run('const =','error');assert.match(await output.textContent(),/SyntaxError/);checks.push('runtime and syntax errors');
  await run('const value=1;');assert.match(await output.textContent(),/no output/);
  await editor.fill('');await execute.click();assert.equal(await lab.getAttribute('data-execution'),'idle');assert.match(await output.textContent(),/Enter a JavaScript/);checks.push('empty and no-output commands');
  await editor.fill("const start=Date.now(); while(Date.now()-start<1000){} console.log('Done');");await execute.click();await p.waitForFunction(()=>document.querySelector('.lab-shell').dataset.execution==='running');
  assert.equal(await execute.isDisabled(),true);assert.equal(await lab.getAttribute('aria-busy'),'true');assert.equal(await p.locator('.shell-progress').isHidden(),false);assert.equal(await lab.locator('[data-lab-action=reset]').isDisabled(),true);
  await lab.screenshot({path:path.join(out,'terminal-running.png')});await p.waitForFunction(()=>document.querySelector('.lab-shell').dataset.execution==='success');assert.equal(await output.textContent(),'Done');checks.push('actual running state, loading segments, disabled duplicate execution');
  await run('while(true){}','timeout');assert.match(await output.textContent(),/2-second time limit/);checks.push('worker timeout');
  await editor.fill('while(true){}');await execute.click();await p.locator('[data-action=cancelCode]').click();await p.waitForFunction(()=>document.querySelector('.lab-shell').dataset.execution==='cancelled');assert.equal(await execute.isEnabled(),true);checks.push('cancel and re-execute');
  await editor.fill("console.log('Keyboard execution');");await editor.press('Control+Enter');await p.waitForFunction(()=>document.querySelector('.lab-shell').dataset.execution==='success');assert.equal(await output.textContent(),'Keyboard execution');
  assert.equal(await lab.locator('[role=tab]').count(),0);assert.equal(await output.isVisible(),true);checks.push('keyboard execute, single view without tabs');
  await p.reload();await lab.waitFor();assert.equal(await editor.inputValue(),"console.log('Keyboard execution');");assert.equal(await output.textContent(),'Keyboard execution');checks.push('draft and output survive reload');
  await p.locator('.lab-menu summary').click();await p.locator('[data-lab-action=clear]').click();assert.equal(await output.textContent(),'Console cleared.');
  await editor.fill('console.log(123);');await p.locator('.lab-menu summary').click();await p.locator('[data-lab-action=reset]').click();assert.equal(await editor.inputValue(),"console.log('Hello, Skill UP!');");checks.push('clear and reset');
  await run("console.log('<img src=x onerror=alert(1)>');");assert.equal(await output.locator('img').count(),0);checks.push('output is escaped text');
  await p.evaluate(()=>{window.savedLabWorker=window.Worker;window.Worker=class{constructor(){throw new Error('Runtime unavailable');}};});await run("console.log('test');",'error');assert.match(await output.textContent(),/Unable to start/);await p.evaluate(()=>{window.Worker=window.savedLabWorker;delete window.savedLabWorker;});checks.push('worker startup failure restores controls');
  await editor.fill('while(true){}');await execute.click();await p.evaluate(()=>location.hash='/course/c1');await p.waitForFunction(()=>!document.querySelector('.lab-shell'));await p.evaluate(()=>location.hash='/lesson/c1/l2');await lab.waitFor();assert.equal(await lab.getAttribute('data-execution'),'idle');assert.notEqual(await output.textContent(),'Stopped: the command exceeded the 2-second time limit.');await run("console.log('Hello, Skill UP!');\nconsole.log('Ready for the next task.');");checks.push('navigation stops worker without stale results');
  for(const width of [1280,1440]){await p.setViewportSize({width,height:1000});await lab.scrollIntoViewIfNeeded();assert.equal(await p.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);const layout=await lab.evaluate(el=>{const q=el.querySelector('.shell-question').getBoundingClientRect(),e=el.querySelector('.shell-editor').getBoundingClientRect(),f=el.querySelector('.shell-execution').getBoundingClientRect();return {ordered:q.bottom<=e.top&&e.bottom<=f.top};});assert.equal(layout.ordered,true);await lab.screenshot({path:path.join(out,`terminal-${width}.png`)});}checks.push('1280 and 1440 desktop layout');
  await p.emulateMedia({reducedMotion:'reduce'});assert.equal(await p.locator('.shell-segments').evaluate(el=>getComputedStyle(el).animationName),'none');assert.match(await editor.evaluate(el=>getComputedStyle(el).fontFamily),/Consolas/);checks.push('reduced motion and terminal typography');
  const guest=await browser.newPage();await guest.goto(base.split('#')[0]+'#/lesson/c1/l1');await guest.locator('.lab-shell').waitFor();assert.equal(await guest.locator('[data-action=runCode]').isDisabled(),true);assert.equal(await guest.locator('#code-editor').getAttribute('readonly'),'');checks.push('guest preview remains read-only');await guest.close();
  assert.deepEqual(errors,[]);const result={checks,errors};fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});

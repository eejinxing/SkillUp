/* Activity page extras: the practice terminal runs the learner's JavaScript in the browser (it cannot run on the
   server), and the quiz timer counts down. Answers, grading and progress are posted to the server. */
(function(){
 'use strict';
 const lab=document.querySelector('.lab-shell');
 if(lab){
  const output=lab.querySelector('#code-output'),editor=lab.querySelector('#code-editor'),runButton=lab.querySelector('[data-action=runCode]'),cancelButton=lab.querySelector('[data-action=cancelCode]'),status=lab.querySelector('.shell-execution-status'),progress=lab.querySelector('.shell-progress');
  const guest=lab.dataset.guest==='true';let active=null;
  const labels={success:'Execution complete',error:'Execution failed',timeout:'Execution stopped · time limit reached',cancelled:'Execution cancelled'};
  function state(name,message){const running=name==='running';lab.dataset.execution=name;lab.setAttribute('aria-busy',String(running));status.textContent=message;progress.hidden=!running;runButton.disabled=running||guest;cancelButton.hidden=!running;}
  // A worker keeps synchronous practice code off the page and can be stopped after 2 seconds.
  function workerMain(){self.onmessage=function(event){const lines=[],format=v=>{if(typeof v==='string')return v;try{return typeof v==='object'?JSON.stringify(v):String(v);}catch(e){return String(v);}};const write=(...args)=>{if(lines.length<1000)lines.push(args.map(format).join(' ').slice(0,10000));};const console={log:write,info:write,warn:write,error:write,debug:write};try{new Function('console',event.data)(console);self.postMessage({output:lines.join('\n')||'Command ran successfully (no output).',status:'success'});}catch(error){self.postMessage({output:[...lines,error.name+': '+error.message].join('\n'),status:'error'});}};}
  function run(){
   if(active||guest)return;
   const code=editor.value;
   if(!code.trim()){output.textContent='Enter a JavaScript command first.';state('idle','Waiting for a command');editor.focus();return;}
   let worker,url,timeout,settled=false;
   const job={finish(text,result){if(settled)return;settled=true;clearTimeout(timeout);if(worker)worker.terminate();if(url)URL.revokeObjectURL(url);active=null;output.textContent=text;state(result,labels[result]||'Ready to execute');}};
   active=job;state('running','Executing command…');output.textContent='Running…';
   try{
    url=URL.createObjectURL(new Blob(['('+workerMain.toString()+')()'],{type:'application/javascript'}));worker=new Worker(url);
    timeout=setTimeout(()=>job.finish('Stopped: the command exceeded the 2-second time limit.','timeout'),2000);
    worker.onmessage=e=>job.finish(String(e.data.output),e.data.status==='error'?'error':'success');
    worker.onerror=e=>{e.preventDefault();job.finish(e.message||'The JavaScript runtime could not execute this command.','error');};
    worker.postMessage(code);
   }catch(error){job.finish('Unable to start the JavaScript runtime: '+error.message,'error');}
  }
  runButton.addEventListener('click',run);
  cancelButton.addEventListener('click',()=>{if(active)active.finish('Execution cancelled.','cancelled');});
  editor.addEventListener('keydown',e=>{if((e.ctrlKey||e.metaKey)&&e.key==='Enter'){e.preventDefault();run();}});
  lab.querySelectorAll('[data-lab-action]').forEach(b=>b.addEventListener('click',()=>{
   if(active||guest)return;
   if(b.dataset.labAction==='clear'){output.textContent='Console cleared.';state('idle','Ready to execute');}
   if(b.dataset.labAction==='reset')editor.value="console.log('Hello, Skill UP!');";
   lab.querySelector('.lab-menu').open=false;
  }));
 }
 // Quiz countdown. The server also checks the time when answers are submitted.
 const timer=document.getElementById('quiz-timer');
 if(timer&&timer.dataset.secondsLeft){
  const end=Date.now()+Number(timer.dataset.secondsLeft)*1000;
  const tick=()=>{const left=Math.max(0,Math.round((end-Date.now())/1000));timer.textContent=Math.floor(left/60)+':'+String(left%60).padStart(2,'0')+' remaining';if(!left){clearInterval(id);const submit=document.querySelector('.answer-quiz .answer-submit');if(submit)submit.disabled=true;}};
  const id=setInterval(tick,1000);tick();
 }
})();

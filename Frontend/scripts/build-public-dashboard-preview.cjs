/* Capture the real learner dashboard in an isolated demo browser for the public hero. */
const {chromium}=require('playwright');
const sharp=require('sharp');
const fs=require('node:fs'),path=require('node:path'),{pathToFileURL}=require('node:url');
(async()=>{
 const root=path.resolve(__dirname,'..'),dir=path.join(root,'assets/public-dashboard');fs.mkdirSync(dir,{recursive:true});
 const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{})});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1000},reducedMotion:'reduce'});
  await page.goto(pathToFileURL(path.join(root,'index.html')).href);
  await page.locator('.home-refined h1').waitFor();
  await page.evaluate(async()=>{SkillAPI.config.latency=0;await SkillAPI.request('demo',{role:'Learner'});location.hash='/dashboard';});
  await page.locator('.lc-velo .vd-card').first().waitFor();
  await page.evaluate(()=>document.fonts.ready);
  await page.addStyleTag({content:'.gn-demo-call{display:none!important}'});
  await page.waitForFunction(()=>[...document.querySelectorAll('.lc-x img')].every(i=>i.complete&&i.naturalWidth));
  for(const [name,width,height] of [['desktop',1440,1000]]){
   await page.setViewportSize({width,height});
   const bounds=await page.locator('.lc-velo').boundingBox(),bottom=await page.locator('.vd-courses').boundingBox();
   const full=await page.locator('.lc-x').screenshot(),meta=await sharp(full).metadata();
   const image=await sharp(full).extract({left:0,top:0,width:meta.width,height:Math.min(meta.height,Math.ceil(bottom.y+bottom.height-bounds.y+24))}).png().toBuffer();
   fs.writeFileSync(path.join(dir,'learner-'+name+'.png'),image);
   await sharp(image).webp({quality:92}).toFile(path.join(dir,'learner-'+name+'.webp'));
  }
  fs.writeFileSync(path.join(dir,'source.json'),JSON.stringify({source:'Actual SkillLearningConsole at #/dashboard',capturedAt:new Date().toISOString(),mode:'isolated seeded Learner demo',variants:['desktop']},null,2));
  console.log('Captured actual learner dashboard: desktop.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});

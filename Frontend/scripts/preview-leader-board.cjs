const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url');
(async()=>{
 const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{})});
 try {
  const page=await browser.newPage({viewport:{width:1440,height:1100},reducedMotion:'reduce'});
  await page.goto(pathToFileURL(process.cwd()+'/index.html').href);
  await page.evaluate(async()=>{SkillAPI.config.latency=0;await SkillAPI.request('demo',{role:'Program Leader'});location.hash='/dashboard';});
  await page.locator('.leader-dashboard').waitFor();
  await page.evaluate(()=>document.fonts.ready);
  await page.waitForFunction(()=>Array.from(document.querySelectorAll('.leader-dashboard img')).every(i=>i.complete&&i.naturalWidth));
  await page.screenshot({path:'previews/leader-board-desktop.png',fullPage:true});
  console.log(JSON.stringify(await page.locator('.ld-reach-chart,.ld-reach-rows,.ld-notifications,.ld-orb,.ld-board').evaluateAll(nodes=>nodes.map(n=>({class:n.className,width:n.clientWidth,scroll:n.scrollWidth,height:n.clientHeight})))));
  for(const width of [960,390]){
   await page.setViewportSize({width,height:1000});
   console.log(JSON.stringify({width,overflow:await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)}));
   await page.screenshot({path:`previews/leader-board-${width}.png`,fullPage:true});
  }
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});

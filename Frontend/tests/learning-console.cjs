const {chromium}=require('playwright');
const assert=require('node:assert/strict'),path=require('node:path');
(async()=>{const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{})});try{const p=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];p.on('pageerror',e=>errors.push(e.message));await p.goto(process.env.SKILLUP_TEST_URL||require('node:url').pathToFileURL(path.join(__dirname,'../index.html')).href);await p.waitForSelector('h1');await p.evaluate(async()=>{SkillAPI.config.latency=0;await SkillAPI.request('demo',{role:'Learner'});location.hash='/dashboard'});await p.locator('.lc-orbit').waitFor();
 // Orbit bento: welcome + next step, level ring, activity segments, XP chart, announcements, course cards.
 for(const tile of ['.ob-hero','.ob-level','.ob-progress','.ob-chart','.ob-news','.ob-courses'])assert.equal(await p.locator(tile).count(),1,tile+' tile');
 assert.equal(await p.locator('.ob-next').getAttribute('href'),'#/lesson/c1/l2');
 assert.equal(await p.locator('.ob-news h2').innerText(),'Announcements');
 assert.equal(await p.locator('.ob-course:not(.is-suggested)').count(),2);assert.equal(await p.locator('.ob-course.is-suggested').count(),1);
 assert.equal(await p.locator('.ob-segments li').count(),7);assert.equal(await p.locator('.ob-segments li.is-done').count(),1);
 assert.ok(await p.locator('[data-chart=month] .ob-bars span.on').count()>=5,'monthly chart should show XP history');
 // The shared capsule header replaces the dashboard's own top bar.
 assert.equal(await p.locator('.skill-global-header').isVisible(),true);assert.equal(await p.locator('.lx-top').isVisible(),false);
 // Count-up settles on the real totals.
 await p.waitForTimeout(1600);const xp=await p.locator('.ob-level dd [data-count]').first();assert.equal((await xp.textContent()).replace(/,/g,''),await xp.getAttribute('data-count'));
 assert.equal(await p.locator('[data-chart=month]').isVisible(),true);await p.locator('[data-lc-range=year]').click();assert.equal(await p.locator('[data-chart=year]').isVisible(),true);assert.equal(await p.locator('[data-chart=year] .ob-bars span').count(),12);
 await p.screenshot({path:path.join(__dirname,'../previews/learning-console-desktop.png'),fullPage:true});
 for(const width of [960,390]){await p.setViewportSize({width,height:900});assert.equal(await p.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false,'Overflow at '+width)}
 await p.screenshot({path:path.join(__dirname,'../previews/learning-console-mobile.png'),fullPage:true});
 await p.evaluate(()=>location.hash='/dashboard');await p.locator('.lc-orbit').waitFor();await p.locator('.ob-course a',{hasText:'MongoDB Management'}).click();await p.waitForURL(/course\/c1/);
 assert.deepEqual(errors,[]);console.log('Learning console (Orbit): six tiles, next step link, course cards, activity segments, count-up, chart range, course link and responsive checks passed.')}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});

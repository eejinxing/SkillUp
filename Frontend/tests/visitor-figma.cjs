/* Visitor homepage: shared capsule header, hero, topics, instructors, FAQ, call to action and the techmier-style footer. */
const {chromium}=require('playwright'),assert=require('node:assert/strict'),path=require('node:path'),fs=require('node:fs');
(async()=>{const browser=await chromium.launch({headless:true,...(process.env.BROWSER_EXECUTABLE_PATH?{executablePath:process.env.BROWSER_EXECUTABLE_PATH}:{})});try{
 const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 const home=async()=>{await page.evaluate(()=>location.hash='/home');await page.locator('.home-refined h1').waitFor();};
 await page.goto(process.env.SKILLUP_TEST_URL||require('node:url').pathToFileURL(path.join(__dirname,'../index.html')).href+'#/home');await page.locator('.home-refined h1').waitFor();
 await page.evaluate(()=>{localStorage.clear();sessionStorage.clear();});await page.reload();await page.locator('.home-refined h1').waitFor();
 await page.evaluate(()=>{document.querySelectorAll('.home-refined img').forEach(i=>i.loading='eager');return document.fonts.ready;});
 await page.waitForFunction(()=>[...document.querySelectorAll('.home-refined img')].every(i=>i.complete&&i.naturalWidth));
 const geometry=await page.locator('.home-refined').boundingBox();assert.ok(geometry.height>3500&&geometry.width>1200,'full-length homepage');

 // Header: shared capsule navigation for visitors, no workspace rail
 const header=page.locator('.skill-global-header');await header.waitFor();
 assert.deepEqual(await header.locator('.gn-links a').allTextContents(),['Home','Courses','Instructors','FAQ']);
 assert.equal(await header.getByRole('link',{name:'Log in',exact:true}).count(),1);
 assert.equal(await header.getByRole('link',{name:'Sign up',exact:true}).count(),1);
 assert.equal(await page.locator('#skill-edge-nav:visible').count(),0,'no workspace rail for visitors');

 // Hero, topics, instructors, FAQ and call to action
 assert.equal((await page.locator('.home-hero h1').textContent()).trim(),'Unlock Your Potential Future');
 assert.deepEqual(await page.locator('.home-hero .home-actions a').evaluateAll(ns=>ns.map(n=>n.getAttribute('href'))),['#/login','#/register']);
 assert.equal(await page.getByRole('heading',{name:'What You’ll Learn',exact:true}).count(),1);
 assert.equal(await page.locator('.home-topic').count(),8);assert.equal(await page.locator('.home-topic-art').count(),8);
 assert.equal(await page.locator('.instructor-card').count(),4);
 await page.locator('[data-instructor-step="1"]').click();assert.equal(await page.locator('.instructor-card').nth(1).getAttribute('aria-expanded'),'true');
 await page.locator('.instructor-card').nth(0).click();assert.equal(await page.locator('.instructor-card').nth(0).getAttribute('aria-expanded'),'true');
 const faq=page.locator('.home-accordion details');assert.equal(await faq.count(),5);
 await faq.nth(1).locator('summary').click();assert.equal(await faq.nth(1).getAttribute('open'),'');await faq.nth(1).locator('summary').click();assert.equal(await faq.nth(1).getAttribute('open'),null);
 assert.equal(await page.locator('.home-cta').count(),0,'red call-to-action removed');
 await page.getByRole('link',{name:'Explore Programming courses',exact:true}).click();await page.waitForURL(/catalog/);await page.locator('#search').waitFor();await home();

 // Footer: notched panel, subscribe card with contact and social links, legal links and the giant wordmark
 const footer=page.locator('.tm-footer');await footer.scrollIntoViewIfNeeded();
 assert.equal(await footer.locator('svg.tm-edge path').count(),1,'smooth tabbed top edge');assert.equal(await footer.evaluate(n=>Math.round(n.getBoundingClientRect().width)),await page.evaluate(()=>document.documentElement.clientWidth),'footer spans the window');assert.equal(await footer.locator('.tm-perks li').count(),3);
 assert.equal(await footer.locator('.tm-top,.home-footer-links').count(),0,'link columns removed');
 assert.equal(await footer.locator('.tm-mail').getAttribute('href'),'mailto:hello@skillup.demo');
 assert.equal(await footer.locator('.tm-wordmark .tm-ch').count(),7);
 assert.deepEqual(await footer.locator('.tm-social a').evaluateAll(ns=>ns.map(n=>n.getAttribute('aria-label'))),['Facebook','X','Instagram','LinkedIn']);
 assert.deepEqual(await footer.locator('.home-footer-base nav a').allTextContents(),['Privacy information','Terms of use','Help center']);
 assert.match(await footer.locator('.home-footer-base small').textContent(),new RegExp('© '+new Date().getFullYear()+' Skill UP'));
 assert.equal(await footer.locator('.tm-wordmark').getAttribute('aria-hidden'),'true');
 assert.equal(await footer.locator('.home-footer-base>a:first-child').isVisible(),false,'no duplicate logo in the base row');

 // Subscribe form: validation, consent and local confirmation
 const form=footer.locator('[data-home-subscribe]'),note=form.locator('.tm-note'),email=form.locator('[name=email]'),consent=form.locator('[name=consent]');
 await form.getByRole('button',{name:'Submit',exact:true}).click();assert.equal(await note.getAttribute('data-state'),'error');assert.match(await note.textContent(),/valid email/);
 await email.fill('not-an-email');await form.getByRole('button',{name:'Submit',exact:true}).click();assert.match(await note.textContent(),/valid email/);
 await email.fill('visitor@example.test');await form.getByRole('button',{name:'Submit',exact:true}).click();assert.match(await note.textContent(),/tick the box/);
 await consent.check();await form.getByRole('button',{name:'Submit',exact:true}).click();
 assert.equal(await note.getAttribute('data-state'),'ok');assert.match(await note.textContent(),/on the list/);
 assert.equal(await page.evaluate(()=>localStorage.getItem('skillup.newsletter')),'visitor@example.test');
 assert.equal(await email.inputValue(),'');assert.equal(await consent.isChecked(),false);
 await page.screenshot({path:path.join(__dirname,'../previews/visitor-desktop.png'),fullPage:true});

 // Mobile: no horizontal overflow, footer links in two columns, subscribe card stacks
 await page.setViewportSize({width:390,height:844});await home();
 assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
 assert.equal(await page.locator('.tm-footer .tm-subscribe').evaluate(n=>getComputedStyle(n).flexDirection),'column');
 await page.screenshot({path:path.join(__dirname,'../previews/visitor-mobile.png'),fullPage:true});

 // Sign up from the header goes to registration
 await page.setViewportSize({width:1440,height:1000});await home();
 await page.locator('.skill-global-header').getByRole('link',{name:'Sign up',exact:true}).click();await page.waitForURL(/register/);await page.locator('[data-form=register]').waitFor();
 assert.deepEqual(errors,[]);
 fs.writeFileSync(path.join(__dirname,'../previews/visitor-verification.json'),JSON.stringify({checkedAt:new Date().toISOString(),geometry,errors,checks:['capsule header for visitors','hero actions','8 topics with art','instructor carousel','FAQ accordion','notched footer with subscribe card','subscribe validation and local confirmation','social and legal links','mobile layout','sign up route']},null,2));
 console.log('Visitor checks passed: capsule header, hero, topics, instructors, FAQ, no CTA, footer subscribe card and form, mobile layout and sign up.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});

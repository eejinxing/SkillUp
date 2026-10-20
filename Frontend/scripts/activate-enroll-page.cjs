const fs=require('node:fs'),path=require('node:path'),base=path.join(__dirname,'..');
function edit(file,from,to){const p=path.join(base,file),s=fs.readFileSync(p,'utf8');if(s.includes(to))return;if(!s.includes(from))throw Error('Missing integration marker: '+file);fs.writeFileSync(p,s.replace(from,to));}
edit('index.html','</head>','<link rel="stylesheet" href="enroll-page.css"></head>');
edit('index.html','<script src="js/pages.js">','<script src="js/enroll-art.js"></script><script src="js/enroll-page.js"></script><script src="js/pages.js">');
const p=path.join(base,'js/app.js');let app=fs.readFileSync(p,'utf8');const start=app.indexOf('  function coursePage(cid){'),end=app.indexOf('  function reviewCard(',start);if(start<0||end<0)throw Error('Course page markers missing');app=app.slice(0,start)+"  function coursePage(cid){const c=course(cid);if(!c||c.status!=='Published')return empty('Course unavailable','This course is not currently open for enrolment.');return SkillEnrollPage({state,user,esc,person,avatar,date,link,btn,form,hidden,textarea,resourceFiles},c);}\n"+app.slice(end);fs.writeFileSync(p,app);
edit('js/app.js',"course:()=>coursePage(route[1])+resourceFiles(route[1]),lesson:","course:()=>coursePage(route[1]),lesson:");
edit('js/app.js',"isLearnerHome=role==='Learner'&&route[0]==='dashboard'","isLearnerHome=role==='Learner'&&['dashboard','course'].includes(route[0])");
edit('js/app.js',"if(kind==='login'){go('dashboard');}","if(kind==='login'){const target=sessionStorage.getItem('skillup.enrol.return');sessionStorage.removeItem('skillup.enrol.return');go(user().role==='Learner'&&state.courses.some(c=>c.id===target&&c.status==='Published')?'course/'+target:'dashboard');}");
console.log('Figma course enrolment page integrated.');

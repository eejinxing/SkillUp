const fs=require('node:fs'),path=require('node:path'),base=path.join(__dirname,'..');
function edit(file,from,to){const p=path.join(base,file),s=fs.readFileSync(p,'utf8');if(s.includes(to))return;if(!s.includes(from))throw Error('Missing integration marker: '+file);fs.writeFileSync(p,s.replace(from,to));}
edit('index.html','</head>','<link rel="stylesheet" href="learner-dashboard.css"></head>');
edit('index.html','<script src="js/pages.js">','<script src="js/learner-template.js"></script><script src="js/learner-dashboard.js"></script><script src="js/pages.js">');
edit('js/app.js',"const role=user().role,collapsed=localStorage.getItem('skillup.nav.collapsed')==='true';","const role=user().role,isLearnerHome=role==='Learner'&&route[0]==='dashboard',collapsed=!isLearnerHome&&localStorage.getItem('skillup.nav.collapsed')==='true';");
edit('js/app.js',"return '<div class=\"shell '+(collapsed?'is-collapsed':'')+'\">","return '<div class=\"shell '+(isLearnerHome?'learner-home-shell ':'')+(collapsed?'is-collapsed':'')+'\">");
edit('js/app.js',"function dashboard(){const u=user(),learner=","function dashboard(){if(user().role==='Learner')return SkillLearnerDashboard({state,user,esc,person,date});const u=user(),learner=");
console.log('Registered learner dashboard integrated.');

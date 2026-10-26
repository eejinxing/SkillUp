/* Fictional request history for the instructor "My requests" grid, so the table shows a realistic backlog.
   Rows are generated deterministically (same data on every reload), dated relative to today and never stored. */
(function(root){
  const programs=['Data & Development','Cloud & DevOps','Design & UX','Cyber Security','AI & Machine Learning','Mobile Apps','Business Analytics','Web Foundations'];
  const titles=['Advanced SQL Joins','Docker for Beginners','Figma Prototyping Basics','Network Security Essentials','Intro to Neural Networks','Kotlin Android Starter','Excel Dashboards','Accessible HTML Forms','REST API Testing','Kubernetes in Practice','Design Systems 101','Threat Modelling Workshop','Prompt Engineering Lab','SwiftUI Layouts','Power BI Reporting','CSS Grid Mastery','Python Data Cleaning','Terraform Fundamentals','User Research Methods','Secure Coding in C#','Computer Vision Primer','Flutter State Management','Forecasting with Python','JavaScript Async Patterns','PostgreSQL Indexing','CI/CD with GitHub Actions','Motion Design for UI','Incident Response Basics','LLM Evaluation Methods','React Native Navigation','A/B Testing Essentials','Semantic HTML Deep Dive','GraphQL Schema Design','AWS Lambda Patterns','Usability Testing Lab','Cryptography Foundations','Recommendation Systems','Mobile Offline Sync','SQL for Analysts','TypeScript Generics','MongoDB Aggregations','Azure Networking','Colour and Typography','Penetration Testing Intro','Feature Engineering','Push Notifications 101','Data Storytelling','Web Performance Audits','Unit Testing with xUnit','Linux Command Line','Information Architecture','Zero Trust Networks','Time Series Models','App Store Release Prep','KPI Dashboards','Progressive Web Apps','Clean Architecture in .NET','Serverless Data Pipelines','Wireframing Workshop','Identity and Access Basics','Transformers Explained','Mobile Accessibility','Customer Segmentation','Browser DevTools Tour'];
  const reviewers=['Sarah Chen','Omar Haddad','Priya Nair','Lucas Martin','Hana Sato'];
  const next={
    'Pending review':['Await leader review','Answer reviewer questions','Prepare demo walkthrough','Confirm launch date'],
    'Approved':['Publish to learners','Announce course launch','Schedule first cohort','Collect early feedback'],
    'Changes requested':['Update quiz questions','Reorder lesson sequence','Add lab instructions','Fix broken resource links'],
    'Rejected':['Revise course outline','Merge with existing course','Rewrite learning outcomes'],
    'Withdrawn':['Archive draft','Reuse lessons elsewhere']
  };
  const feedback={
    'Approved':'Clear structure and good practice tasks. Approved for the next intake.',
    'Changes requested':'Strong start. Please tighten the quiz wording and add a worked example before resubmitting.',
    'Rejected':'This overlaps heavily with an existing course. Consider extending that course instead.',
    'Withdrawn':'Withdrawn by the instructor to rework the scope.'
  };
  function rng(seed){return()=>{seed|=0;seed=seed+0x6D2B79F5|0;let t=Math.imul(seed^seed>>>15,1|seed);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};}
  function build(userId){
    const r=rng(4021),rows=[],day=864e5,today=Date.now();
    titles.forEach((title,i)=>{
      const back=Math.floor(4+i*5.6+r()*4),created=new Date(today-back*day-Math.floor(r()*8)*36e5),pick=r(),
        status=back<18?(pick<.75?'Pending review':'Changes requested'):pick<.5?'Approved':pick<.68?'Changes requested':pick<.84?'Rejected':pick<.94?'Pending review':'Withdrawn',
        kind=r()<.84?'Create':'Delete',priority=status==='Pending review'&&r()<.35?4:1+Math.floor(r()*3),
        steps=next[status],reviewer=reviewers[Math.floor(r()*reviewers.length)],activities=4+Math.floor(r()*14),
        events=[{action:'Submitted',actorId:userId,createdAt:created.toISOString(),comment:kind==='Delete'?'Course is superseded and has no active learners.':'Ready for review. Please check the course structure.'}];
      if(status!=='Pending review'||r()<.5)events.push({action:'Review started',actorId:'u3',createdAt:new Date(+created+(1+Math.floor(r()*2))*day).toISOString(),comment:'Review started'});
      if(feedback[status])events.push({action:status,actorId:status==='Withdrawn'?userId:'u3',createdAt:new Date(+created+(3+Math.floor(r()*4))*day).toISOString(),comment:feedback[status]});
      rows.push({id:'hist-'+String(i+1).padStart(3,'0'),archive:true,kind,courseId:null,courseTitle:title,programId:null,programName:programs[(i*3+Math.floor(r()*2))%programs.length],requesterId:userId,status,createdAt:created.toISOString(),reason:events[0].comment,priority,reviewer,activities,hours:Math.round(activities*(18+r()*20)/60*10)/10,nextStep:steps[Math.floor(r()*steps.length)],events});
    });
    return rows;
  }
  root.SkillRequestHistory={build,enabled:()=>{if(root.SkillRuntimeConfig?.mode==='http')return false;try{return localStorage.getItem('skillup.requests.demoHistory')!=='off';}catch{return true;}}};
})(globalThis);

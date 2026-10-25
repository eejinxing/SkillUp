/* Additional fictional program records for the program/course switcher. */
(function(root){
  function apply(db){
    db.migrations ||= {};
    if(db.migrations.programSwitcherV1)return db;
    const definitions=[
      {id:'demo-systems',name:'Computer Systems',description:'Understand the hardware and networks behind everyday computing.',courses:[
        ['demo-hardware','Inside a Computer','Recognise the CPU, memory, storage and the role of an operating system.','CPU carries out instructions. RAM holds data currently in use. Storage retains files when the power is off. The operating system coordinates these resources.','Computer essentials'],
        ['demo-networks','Networks & the Web','Trace how a browser reaches a website through a network.','A client sends a request to a server. DNS translates a domain name to an IP address. HTTPS protects the connection while data travels between the browser and server.','Networking']
      ]},
      {id:'demo-ai',name:'AI & Data',description:'Build a thoughtful foundation in datasets, models and responsible AI.',courses:[
        ['demo-data','Working with Data','Explore datasets and ask clear questions before analysing them.','Start with a question and inspect each column. Check missing values, units and duplicates. Keep an unchanged copy of the original dataset before cleaning it.','Data literacy'],
        ['demo-models','AI Fundamentals','Understand how models learn from examples and how to evaluate them.','Training data teaches a model patterns. Separate test data checks whether those patterns generalise to new examples. Evaluate errors and bias before using predictions.','AI foundations']
      ]}
    ];
    for(const p of definitions){
      if(db.programs.some(x=>x.id===p.id))continue;
      const courses=p.courses.map(([id,title,description,body,topic])=>({id,title,description,programId:p.id,instructorId:'u2',difficulty:'Beginner',status:'Published',outcomes:description,duration:2,deadline:'2026-12-20',color:'violet',icon:'UP',lessons:[{id:id+'-intro',title:topic,type:'article',body,minutes:15,preview:true}],quiz:{published:false,timeLimit:5,questions:[]}}));
      db.programs.push({id:p.id,name:p.name,description:p.description,leaderId:'u3',courseIds:courses.map(c=>c.id),published:true});
      db.courses.push(...courses);
    }
    db.migrations.programSwitcherV1=true;
    return db;
  }
  const originalSeed=SkillData.seed;
  SkillData.seed=()=>apply(originalSeed());
  root.SkillProgramDemo={apply};
})(globalThis);

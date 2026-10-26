/* End-to-end test of the C# API (Backend/) for every role: grading, permissions, the course request workflow,
   account management, CSRF header and sign-in cookie. It CHANGES DATA (deletes and deactivates demo accounts),
   so run it only against a fresh database created by Database/setup-db.ps1 and seeded with the demo data:
     node tests/backend-api.cjs http://localhost:PORT --fresh-db */
if(!process.argv.includes('--fresh-db')){console.error('Refusing to run: this test changes data. Use a fresh demo database and pass --fresh-db.');process.exit(1);}
const assert = require('node:assert/strict');
const BASE = process.argv[2] || 'http://localhost:8099';
let passed = 0;
const ok = (cond, msg) => { assert.ok(cond, msg); passed++; };

class Client {
  constructor() { this.cookie = ''; }
  async raw(method, path, body, headers = {}) {
    const res = await fetch(BASE + path, { method, headers: { 'Content-Type': 'application/json', ...(this.cookie ? { Cookie: this.cookie } : {}), ...headers }, body: body === undefined ? undefined : JSON.stringify(body) });
    for (const c of res.headers.getSetCookie()) {
      const [pair] = c.split(';'); const [name, value] = pair.split('=');
      if (name === 'SkillUp.Auth') this.cookie = value ? 'SkillUp.Auth=' + value : '';
    }
    const json = await res.json().catch(() => ({}));
    return { status: res.status, json };
  }
  async state() { const r = await this.raw('GET', '/api/workspace'); assert.equal(r.status, 200, JSON.stringify(r.json)); return r.json; }
  async cmd(action, payload = {}, expect = 200) {
    const r = await this.raw('POST', '/api/commands/' + action, payload, { 'X-SkillUp-Request': '1' });
    assert.equal(r.status, expect, action + ' -> ' + r.status + ' ' + JSON.stringify(r.json).slice(0, 300));
    passed++;
    return r.json;
  }
}

(async () => {
  const visitor = new Client();
  let s = await visitor.state();
  ok(s.currentUser === null, 'visitor has no user');
  ok(s.courses.length > 0 && s.courses.every(c => c.status === 'Published'), 'visitor sees only published courses');
  ok(s.courses.every(c => c.lessons.every(l => !('answer' in l)) && c.quiz.questions.every(q => !('answer' in q))), 'visitor gets no answer keys');
  ok(s.users.every(u => !('email' in u) && !('password' in u)), 'visitor gets no emails or passwords');
  ok(s.courses.every(c => c.lessons.every(l => l.preview || l.body === '')), 'visitor sees only preview bodies');
  ok(s.enquiries.length === 0 && s.audit.length === 0 && s.messages.length === 0, 'visitor gets no private collections');

  // CSRF header required
  const noHeader = await visitor.raw('POST', '/api/commands/login', { email: 'alex', password: 'Demo1234!' });
  ok(noHeader.status === 403, 'command without header is rejected');
  await visitor.cmd('login', { email: 'alex', password: 'wrong' }, 401);
  await visitor.cmd('login', { email: 'nora', password: 'Demo1234!' }, 403);
  await visitor.cmd('nope', {}, 401);

  // ---------- Learner ----------
  const alex = new Client();
  let r = await alex.cmd('login', { email: 'alex@skillup.demo', password: 'Demo1234!' });
  ok(r.state.currentUser.username === 'alex' && r.state.currentUser.email === 'alex@skillup.demo', 'learner signed in');
  s = await alex.state();
  ok(s.currentUser.role === 'Learner', 'cookie keeps the session');
  ok(s.users.filter(u => u.id !== s.currentUser.id).every(u => !('email' in u)), 'learner cannot see other emails');
  ok(s.courses.every(c => c.lessons.every(l => !('answer' in l))), 'learner gets no lab answers');
  const mongo = s.courses.find(c => c.title === 'MongoDB Management');
  const [l1, l2, l3, l4] = mongo.lessons;
  let enr = s.enrollments.find(e => e.userId === s.currentUser.id && e.courseId === mongo.id);
  ok(enr && enr.completed.includes(l1.id) && enr.completed.length === 1, 'seeded completion of first lesson');
  await alex.cmd('complete', { courseId: mongo.id, lessonId: l3.id }, 400); // locked
  r = await alex.cmd('complete', { courseId: mongo.id, lessonId: l2.id, answer: 'find' });
  ok(r.data.correct === false, 'wrong lab answer graded on server');
  r = await alex.cmd('complete', { courseId: mongo.id, lessonId: l2.id, answer: '  FindOne ' });
  ok(r.data.correct === true, 'correct lab answer');
  r = await alex.cmd('complete', { courseId: mongo.id, lessonId: l3.id });
  enr = r.state.enrollments.find(e => e.userId === s.currentUser.id && e.courseId === mongo.id);
  ok(enr.completed.length === 3, 'progress saved: ' + enr.completed.length);
  ok(r.state.attempts.filter(a => a.userId === s.currentUser.id && a.type === 'lab').length === 2, 'lab attempts recorded');
  await alex.cmd('quizSubmit', { courseId: mongo.id, lessonId: l4.id, answers: [0, 1] }, 400);
  r = await alex.cmd('quizSubmit', { courseId: mongo.id, lessonId: l4.id, answers: [0, 0, 0] });
  ok(r.data.score === 67 && r.data.feedback.length === 3 && r.data.feedback[1].correct === false, 'quiz graded 67% with feedback');
  ok(!r.state.enrollments.find(e => e.id === enr.id).completed.includes(l4.id), '67% does not complete');
  r = await alex.cmd('quizSubmit', { courseId: mongo.id, lessonId: l4.id, answers: [0, 1, 0] });
  ok(r.data.score === 100 && r.state.enrollments.find(e => e.id === enr.id).completed.includes(l4.id), 'quiz passed completes course');
  ok(r.state.attempts.some(a => a.type === 'quiz' && a.userId !== s.currentUser.id && a.answers.length === 0), 'other learners attempts stripped for leaderboard');
  await alex.cmd('enrol', { courseId: mongo.id }, 409);
  const ds = s.courses.find(c => c.title === 'Data Science Essentials');
  await alex.cmd('enrol', { courseId: ds.id });
  await alex.cmd('courseReview', { courseId: mongo.id, rating: 4, text: 'Great labs.' });
  r = await alex.cmd('courseReview', { courseId: mongo.id, rating: 5, text: 'Great labs, updated.' });
  ok(r.state.reviews.filter(x => x.userId === s.currentUser.id).length === 1, 'review updated not duplicated');
  await alex.cmd('post', { courseId: mongo.id, text: 'How do indexes work? #MongoDB #Lab1' });
  r = await alex.state();
  const post = r.posts.find(p => p.text.startsWith('How do indexes'));
  await alex.cmd('reply', { id: post.id, kind: 'post', text: 'Following.' });
  const john = s.users.find(u => u.username === 'john');
  await alex.cmd('message', { to: john.id, text: 'Hi John' });
  await alex.cmd('message', { to: s.currentUser.id, text: 'self' }, 400);
  await alex.cmd('readMessages', { from: john.id });
  r = await alex.cmd('ai', { text: 'How do I group results?' });
  ok(r.data.response.includes('$group') && r.state.ai.length === 1, 'AI history stored');
  await alex.cmd('globalMessage', { text: 'Hello community' });
  await alex.cmd('feedback', { rating: 5, text: 'Love it' });
  await alex.cmd('preferences', { section: 'notifications', courseUpdates: true, deadlines: false, messages: true, reviewUpdates: true });
  r = await alex.cmd('profile', { name: 'Alex Morgan', username: 'alex', email: 'alex@skillup.demo', location: 'KL', bio: 'Updated bio', theme: 'light' });
  ok(r.state.currentUser.theme === 'light' && r.state.currentUser.preferences.deadlines === false, 'profile and preferences saved');
  await alex.cmd('password', { current: 'wrong', password: 'NewPass123', confirm: 'NewPass123' }, 400);
  await alex.cmd('courseSave', { title: 'x' }, 403);
  const session = r.state.sessions[0];
  r = await alex.cmd('sessionJoin', { id: session.id });
  ok(r.state.sessions[0].attendees.includes(s.currentUser.id), 'joined session');
  r = await alex.cmd('sessionJoin', { id: session.id, leave: true });
  ok(!r.state.sessions[0].attendees.includes(s.currentUser.id), 'left session');

  // ---------- Instructor ----------
  const jc = new Client();
  r = await jc.cmd('login', { email: 'john', password: 'Demo1234!' });
  s = r.state;
  ok(s.courses.find(c => c.id === mongo.id).lessons[1].answer === 'findOne', 'assigned instructor sees lab answer');
  ok(s.messages.some(m => m.text === 'Hi John'), 'instructor received message');
  const program = s.programs[0];
  r = await jc.cmd('courseSave', { title: 'SQL Basics', description: 'Learn SQL', outcomes: 'Write SELECT', programId: program.id, difficulty: 'Beginner', deadline: '2026-12-31', duration: 2, startDate: '2026-11-01' });
  const sql = r.data;
  ok(sql.id && sql.status === 'Draft' && sql.startDate === '2026-11-01', 'course created');
  await jc.cmd('courseSave', { id: sql.id, title: 'SQL Basics', description: 'Learn SQL', outcomes: 'Write SELECT', programId: program.id, difficulty: 'Beginner', deadline: '2026-10-01', startDate: '2026-11-01' }, 400);
  await jc.cmd('lessonSave', { courseId: sql.id, title: 'Intro', type: 'article', body: 'SELECT reads rows.', minutes: 10, preview: true });
  await jc.cmd('lessonSave', { courseId: sql.id, title: 'Try it', type: 'lab', body: 'Keyword to read rows?', answer: 'SELECT', minutes: 15 });
  await jc.cmd('lessonSave', { courseId: sql.id, title: 'Check', type: 'quiz', body: 'Quiz time', minutes: 10 });
  await jc.cmd('courseAction', { id: sql.id, action: 'submit' }, 400); // quiz not published
  r = await jc.cmd('quizSave', { courseId: sql.id, timeLimit: 5, published: true, questions: [{ text: 'Reads rows?', options: ['SELECT', 'DROP'], answer: 0, topic: 'SQL' }, { text: 'Removes table?', options: ['DROP', 'SELECT', 'JOIN'], answer: 0, topic: 'DDL' }] });
  let sqlCourse = r.state.courses.find(c => c.id === sql.id);
  ok(sqlCourse.quiz.questions.length === 2 && sqlCourse.quiz.questions[1].answer === 0, 'quiz saved');
  const q1 = sqlCourse.quiz.questions[0];
  r = await jc.cmd('quizSave', { courseId: sql.id, timeLimit: 5, published: true, questions: [{ id: q1.id, text: 'Reads rows (edited)?', options: ['DROP', 'SELECT', 'UPDATE'], answer: 1, topic: 'SQL' }] });
  sqlCourse = r.state.courses.find(c => c.id === sql.id);
  ok(sqlCourse.quiz.questions.length === 1 && sqlCourse.quiz.questions[0].id === q1.id && sqlCourse.quiz.questions[0].answer === 1, 'quiz edit keeps question id');
  await jc.cmd('resourceSave', { courseId: sql.id, name: 'notes.txt', size: 5, dataUrl: 'data:text/plain;base64,SGVsbG8=' });
  await jc.cmd('resourceSave', { courseId: sql.id, name: 'x.exe', dataUrl: 'data:application/x-msdownload;base64,AA==' }, 400);
  r = await jc.cmd('courseRequest', { kind: 'Create', courseId: sql.id, reason: 'Ready for review' });
  const createReq = r.data;
  ok(createReq.kind === 'Create' && createReq.status === 'Pending review' && r.state.courses.find(c => c.id === sql.id).status === 'Pending review', 'create request submitted');
  await jc.cmd('lessonSave', { courseId: sql.id, title: 'More', type: 'article', body: 'x' }, 400); // locked while pending
  await jc.cmd('announcementSave', { courseId: mongo.id, title: 'Office hours', body: 'Friday 3pm' });
  await jc.cmd('sessionSave', { courseId: mongo.id, title: 'Live Q&A', startsAt: '2026-11-05T15:00' });
  r = await jc.cmd('moderate', { id: post.id, status: 'Hidden', reason: 'Off topic' });
  ok(r.state.posts.find(p => p.id === post.id).status === 'Hidden', 'post hidden');
  r = await jc.cmd('enrollmentSave', { courseId: mongo.id, userId: s.users.find(u => u.username === 'daniel').id, remove: true });
  r = await jc.cmd('enrollmentSave', { courseId: mongo.id, userId: s.users.find(u => u.username === 'daniel').id });
  await jc.cmd('courseAction', { id: ds.id, action: 'hide' });

  // learner no longer sees hidden post; sees announcement
  s = await alex.state();
  ok(!s.posts.some(p => p.id === post.id) || s.posts.find(p => p.id === post.id).userId === s.currentUser.id, 'hidden post only visible to its author');
  ok(s.announcements.some(a => a.title === 'Office hours'), 'learner sees announcement of enrolled course');
  ok(s.sessions.some(x => x.title === 'Live Q&A' && x.startsAt === '2026-11-05T15:00'), 'learner sees session');

  // ---------- Program Leader ----------
  const sarah = new Client();
  r = await sarah.cmd('login', { email: 'sarah', password: 'Demo1234!' });
  s = r.state;
  ok(s.courseRequests.some(x => x.id === createReq.id), 'leader sees request');
  ok(s.courses.find(c => c.id === sql.id).quiz.questions[0].answer === 1, 'leader sees answer key for review');
  await sarah.cmd('courseRequestSeen', { id: createReq.id });
  await sarah.cmd('review', { courseId: sql.id, requestId: createReq.id, decision: 'Approved', comment: '' }, 400);
  r = await sarah.cmd('review', { courseId: sql.id, requestId: createReq.id, decision: 'Approved', comment: 'Looks good' });
  ok(r.state.courses.find(c => c.id === sql.id).status === 'Published', 'course published');
  const reqAfter = r.state.courseRequests.find(x => x.id === createReq.id);
  ok(reqAfter.status === 'Approved' && reqAfter.events.map(e => e.action).join() === 'Submitted,Review started,Approved', 'request timeline: ' + reqAfter.events.map(e => e.action).join());
  ok(r.state.approvals.some(a => a.courseId === sql.id && a.decision === 'Approved'), 'approvals derived');
  await sarah.cmd('review', { courseId: sql.id, requestId: createReq.id, decision: 'Approved', comment: 'again' }, 409);
  r = await sarah.cmd('programSave', { name: 'Cloud', description: 'Cloud skills', published: true });
  const cloud = r.data.id;
  await sarah.cmd('pathSave', { id: program.id, courseIds: [sql.id, mongo.id] });
  r = await sarah.cmd('pathSave', { id: program.id, courseIds: [sql.id, sql.id] }, 400);
  r = await sarah.cmd('programCourseMove', { courseId: sql.id, programId: cloud, includeInPath: true });
  s = r.state;
  ok(s.courses.find(c => c.id === sql.id).programId === cloud && s.programs.find(p => p.id === cloud).courseIds[0] === sql.id && !s.programs.find(p => p.id === program.id).courseIds.includes(sql.id), 'course moved with path');
  await sarah.cmd('programDelete', { id: cloud }, 400);
  await sarah.cmd('assign', { courseId: sql.id, instructorId: john.id });
  // pending review course c4 from seed: request changes
  const api = s.courses.find(c => c.title === 'API Design with ASP.NET');
  const apiReq = s.courseRequests.find(x => x.courseId === api.id && x.status === 'Pending review');
  ok(apiReq, 'seeded pending request present');
  r = await sarah.cmd('review', { courseId: api.id, requestId: apiReq.id, decision: 'Changes requested', comment: 'Add a quiz' });
  ok(r.state.courses.find(c => c.id === api.id).status === 'Changes requested', 'changes requested');

  // ---------- Instructor deletion request flow ----------
  r = await jc.cmd('courseSave', { title: 'Temp', description: 'Temp', outcomes: 'Temp', programId: program.id, difficulty: 'Advanced' });
  const temp = r.data.id;
  await jc.cmd('courseAction', { id: temp, action: 'delete' }, 400); // reason required
  r = await jc.cmd('courseRequest', { kind: 'Delete', courseId: temp, reason: 'Duplicate' });
  const delReq = r.data.id;
  await jc.cmd('lessonSave', { courseId: temp, title: 'x', type: 'article', body: 'x' }, 409);
  r = await sarah.cmd('courseRequestReview', { id: delReq, decision: 'Approved', comment: 'OK' });
  ok(!r.state.courses.some(c => c.id === temp) && r.state.courseRequests.find(x => x.id === delReq).status === 'Approved', 'deletion approved removes course, keeps history');

  // ---------- Admin ----------
  const admin = new Client();
  r = await admin.cmd('login', { email: 'admin', password: 'Demo1234!' });
  s = r.state;
  ok(s.users.some(u => u.status === 'Pending') && s.users.every(u => !('password' in u)), 'admin sees all users without passwords');
  ok(s.audit.length > 10, 'audit log: ' + s.audit.length);
  const nora = s.users.find(u => u.username === 'nora');
  r = await admin.cmd('userSave', { id: nora.id, name: nora.name, email: nora.email, username: nora.username, role: 'Instructor', status: 'Active', bio: nora.bio });
  ok(r.state.users.find(u => u.id === nora.id).status === 'Active', 'instructor approved');
  const nr = new Client();
  await nr.cmd('login', { email: 'nora', password: 'Demo1234!' });
  r = await admin.cmd('userSave', { name: 'Temp User', email: 'temp@x.com', username: 'tempuser', role: 'Learner', status: 'Active', password: 'Temp12345' });
  const tempUser = r.data.id;
  await admin.cmd('userSave', { name: 'Dup', email: 'TEMP@x.com', username: 'other', role: 'Learner', status: 'Active', password: 'Temp12345' }, 409);
  await admin.cmd('userSave', { id: s.currentUser.id, name: s.currentUser.name, email: s.currentUser.email, username: 'admin', role: 'Learner', status: 'Active' }, 400);
  await admin.cmd('userDelete', { id: john.id }, 400);
  const daniel = s.users.find(u => u.username === 'daniel');
  r = await admin.cmd('userDelete', { id: daniel.id });
  ok(!r.state.users.some(u => u.id === daniel.id), 'learner with history deleted');
  r = await admin.cmd('userDelete', { id: tempUser });
  ok(r.state.audit.some(a => a.action === 'User deleted'), 'deletion audited');
  await admin.cmd('moderate', { id: post.id, status: 'Flagged', reason: 'Spam check' });

  // ---------- Public: register, reset, enquiries, demo ----------
  const pub = new Client();
  r = await pub.cmd('register', { name: 'New Learner', username: 'newbie', email: 'newbie@x.com', role: 'Learner', password: 'Passw0rd!', confirm: 'Passw0rd!' });
  ok(r.data.pending === false, 'registered');
  r = await pub.cmd('register', { name: 'New Teacher', username: 'teach', email: 'teach@x.com', role: 'Instructor', password: 'Passw0rd!', confirm: 'Passw0rd!' });
  ok(r.data.pending === true, 'instructor pending');
  await pub.cmd('register', { name: 'Bad', username: 'bad', email: 'bad@x.com', role: 'Admin', password: 'Passw0rd!', confirm: 'Passw0rd!' }, 400);
  r = await pub.cmd('forgot', { email: 'newbie@x.com' });
  await pub.cmd('resetPassword', { token: 'nope', password: 'Another123', confirm: 'Another123' }, 400);
  await pub.cmd('resetPassword', { token: r.data.token, password: 'Another123', confirm: 'Another123' });
  await pub.cmd('resetPassword', { token: r.data.token, password: 'Another123', confirm: 'Another123' }, 400);
  await pub.cmd('login', { email: 'newbie', password: 'Another123' });
  r = await visitor.cmd('enquirySave', { name: 'Vis', email: 'vis@x.com', subject: 'Q', message: 'Hello' });
  const enq = r.data;
  ok(enq.id && enq.token, 'enquiry created with token');
  await visitor.cmd('enquiryLookup', { id: enq.id, token: 'bad' }, 404);
  r = await visitor.cmd('enquirySave', { id: enq.id, token: enq.token, name: 'Vis', email: 'vis@x.com', subject: 'Q2', message: 'Hello again' });
  ok(r.data.subject === 'Q2', 'enquiry updated');
  r = await visitor.cmd('enquiryLookup', { id: enq.id, token: enq.token });
  ok(r.data.message === 'Hello again', 'enquiry lookup');
  await visitor.cmd('enquirySave', { id: enq.id, token: 'bad', name: 'Vis', email: 'vis@x.com', subject: 'X', message: 'X' }, 403);
  await visitor.cmd('enquiryDelete', { id: enq.id, token: enq.token });
  await visitor.cmd('enquiryLookup', { id: enq.id, token: enq.token }, 404);
  const demo = new Client();
  r = await demo.cmd('demo', { role: 'Program Leader' });
  ok(r.state.currentUser.role === 'Program Leader', 'demo login');
  r = await demo.cmd('logout');
  ok(r.state.currentUser === null, 'logout');
  s = await demo.state();
  ok(s.currentUser === null, 'cookie cleared');

  // deactivated user loses access at once
  const mei = new Client();
  await mei.cmd('login', { email: 'mei', password: 'Demo1234!' });
  s = await admin.state();
  const meiRow = s.users.find(u => u.username === 'mei');
  await admin.cmd('userSave', { id: meiRow.id, name: meiRow.name, email: meiRow.email, username: 'mei', role: 'Learner', status: 'Inactive' });
  s = await mei.state();
  ok(s.currentUser === null, 'deactivated account signed out');

  console.log('API end-to-end test passed: ' + passed + ' checks.');
})().catch(e => { console.error('FAILED after ' + passed + ' checks:', e.message); process.exitCode = 1; });

const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..'),html=fs.readFileSync(path.join(root,'index.html'),'utf8');
const scripts=[...html.matchAll(/<script src="([^"]+)"/g)].map(m=>m[1]),styles=[...html.matchAll(/<link rel="stylesheet" href="([^"]+)"/g)].map(m=>m[1]);
for(const rel of [...scripts,...styles])assert.ok(fs.existsSync(path.join(root,rel)),'Missing '+rel);
let cssResources=0;for(const rel of styles){const p=path.join(root,rel),s=fs.readFileSync(p,'utf8');for(const m of s.matchAll(/url\(\s*(['"]?)(.*?)\1\s*\)/g)){const url=m[2].trim();if(!url||/^(data:|https?:|\/\/|#)/i.test(url))continue;const file=path.resolve(path.dirname(p),decodeURIComponent(url.split(/[?#]/)[0]));assert.ok(fs.existsSync(file),rel+' missing '+url);cssResources++;}}
let parsed=0;for(const f of fs.readdirSync(path.join(root,'js')).filter(f=>f.endsWith('.js'))){new vm.Script(fs.readFileSync(path.join(root,'js',f),'utf8'),{filename:f});parsed++;}
const pages=JSON.parse(fs.readFileSync(path.join(root,'../docs/WEBFORMS-PAGES.json'),'utf8'));
for(const p of pages){const aspx=fs.readFileSync(path.join(root,'..',p.page),'utf8'),cs=fs.readFileSync(path.join(root,'..',p.codeBehind),'utf8');assert.ok(aspx.includes('MasterPageFile="~/Frontend/SkillUp.Master"'));assert.ok(cs.includes('Page_Load'));assert.ok(cs.includes('SetInitialRoute'));}
console.log(JSON.stringify({scripts:scripts.length,styles:styles.length,cssResources,jsSyntax:parsed,webFormsPairs:pages.length,missing:0},null,2));

/* Backend integration switch. SkillUp.Master sets data-runtime-mode="http" on <body> so the Web Forms pages use the C# API
   (Backend/ApiHandler.cs) and the database. index.html (node server.cjs) has no API and keeps the localStorage mock. */
(function(root){
 'use strict';
 root.SkillRuntimeConfig={
  mode:document.body&&document.body.getAttribute('data-runtime-mode')==='http'?'http':'mock',
  baseUrl:new URL('../api',document.baseURI).pathname.replace(/\/$/,''),
  latency:160
 };
})(window);

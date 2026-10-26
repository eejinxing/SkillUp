/* Backend integration switch. SkillUp.Master sets SkillRuntimeMode='http' so the Web Forms pages use the C# API
   (Backend/ApiHandler.cs) and the database. index.html (node server.cjs) has no API and keeps the localStorage mock. */
(function(root){
 'use strict';
 root.SkillRuntimeConfig={
  mode:root.SkillRuntimeMode==='http'?'http':'mock',
  baseUrl:new URL('../api',document.baseURI).pathname.replace(/\/$/,''),
  latency:160
 };
})(window);

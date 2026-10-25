/* Backend integration switch. The UI runs in mock mode until the API is implemented. */
(function(root){
 'use strict';
 root.SkillRuntimeConfig={
  mode:'mock', // Change to 'http' only after GET workspace and POST commands are ready.
  baseUrl:new URL('../api',document.baseURI).pathname.replace(/\/$/,''),
  latency:160
 };
})(window);

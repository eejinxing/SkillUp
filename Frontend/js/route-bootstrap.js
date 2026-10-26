/* Starting route for the Web Forms pages. Each .aspx page sets data-initial-route on <body> (see SkillUp.Master.cs);
   when the address has no #hash yet, it becomes the first route. Loaded before the other scripts so the router sees it. */
(function(){
 'use strict';
 var route=document.body&&document.body.getAttribute('data-initial-route');
 if(route&&!location.hash)history.replaceState(null,'','#/'+route);
})();

<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TerminalLab.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.TerminalLab" %>
<%-- Practice terminal on the activity page. js/activity.js runs the code in the browser; nothing here is graded. --%>
<section class="answer-task answer-lab terminal-lab lab-shell" data-view="editor" data-execution="idle" data-guest="<%= Guest ? "true" : "false" %>" aria-label="Virtual lab terminal" aria-busy="false">
  <header class="shell-header"><div class="shell-brand">SKILL UP<span>TERMINAL</span></div><details class="lab-menu"><summary aria-label="Terminal options">&#8942;</summary><div><button type="button" data-lab-action="clear"<%= Guest ? " disabled" : "" %>>Clear output</button><button type="button" data-lab-action="reset"<%= Guest ? " disabled" : "" %>>Reset command</button></div></details></header>
  <div class="shell-body">
    <div class="shell-session"><span><i aria-hidden="true"></i>LOCAL WORKSPACE</span><span>JavaScript &middot; UTF-8</span></div>
    <section class="shell-question" aria-label="Activity instructions"><div class="answer-task-heading shell-task-heading"><span class="answer-task-label">Task <%= TaskNumber %></span><small>Code &middot; JavaScript in the browser</small></div><h3><%: Lesson.Title %></h3><p><%: BodyText ?? Lesson.Body %></p></section>
    <div class="shell-editor" id="lab-editor-panel">
      <label class="shell-prompt" for="code-editor"><span><%: Username %>@skillup:~</span> $ <b>command</b></label>
      <div class="shell-command"><textarea id="code-editor" aria-label="JavaScript command" aria-describedby="lab-runtime-note" spellcheck="false" autocapitalize="off" autocomplete="off"<%= Guest ? " readonly" : "" %> data-starter="<%: Starter %>"><%: Starter %></textarea></div>
      <div class="shell-actions"><span id="lab-runtime-note"><%= Guest ? "Log in to execute commands." : "Local JavaScript &middot; Ctrl / &#8984; + Enter to run" %></span><button type="button" class="shell-cancel" data-action="cancelCode" hidden>Stop</button><button type="button" class="answer-glass answer-submit shell-execute" data-action="runCode" aria-label="Run code" title="Run code &middot; Ctrl / &#8984; + Enter"<%= Guest ? " disabled" : "" %>><img src="assets/answer/imgRectangle1.png" alt="" draggable="false"></button></div>
    </div>
    <section class="shell-output" id="lab-console-panel" aria-label="Output"><h3>OUTPUT <span>JavaScript runtime</span></h3><pre id="code-output" aria-live="polite" aria-atomic="true">Ready. Execute your command to see the output.</pre></section>
    <footer class="shell-execution"><div class="shell-execution-heading"><span class="shell-execution-status" role="status"><%= Guest ? "Read-only preview" : "Ready to execute" %></span><span class="shell-execution-meta">2 s execution limit</span></div><div class="shell-segments" aria-hidden="true"><%= string.Concat(System.Linq.Enumerable.Repeat("<i></i>", 48)) %></div><div class="shell-progress sr-only" hidden role="progressbar" aria-label="Executing command"></div></footer>
  </div>
</section>

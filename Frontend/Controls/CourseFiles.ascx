<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CourseFiles.ascx.cs" Inherits="SKILL_UP.Frontend.Controls.CourseFiles" %>
<section class="panel course-files" style="margin-top:24px">
  <div class="panel-head"><h2>Course files</h2></div>
  <asp:Panel ID="UploadPanel" runat="server" CssClass="file-upload" DefaultButton="UploadButton">
    <asp:PlaceHolder ID="CoursePicker" runat="server">
      <label>Course <asp:DropDownList ID="UploadCourse" runat="server" DataTextField="Title" DataValueField="Id" /></label>
    </asp:PlaceHolder>
    <label>File (PDF, TXT or image, up to 2 MB) <asp:FileUpload ID="UploadFile" runat="server" accept=".pdf,.txt,.png,.jpg,.jpeg,.webp" /></label>
    <skill:SubmitButton ID="UploadButton" runat="server" CssClass="primary" CausesValidation="false" OnClick="Upload_Click">&uarr; Upload file</skill:SubmitButton>
    <p class="notice">Uploading or deleting a resource returns the course to Draft for review.</p>
  </asp:Panel>
  <div class="grid">
    <asp:Repeater ID="Files" runat="server">
      <ItemTemplate>
        <article class="panel">
          <div class="eyebrow"><%#: Eval("Extension") %> document</div>
          <h3 style="margin:15px 0"><%#: Eval("Name") %></h3>
          <small><%# Eval("Kb") %> KB &middot; <%#: Eval("Course") %></small>
          <div class="actions">
            <a class="button" href="<%#: Eval("DataUrl") %>" download="<%#: Eval("Name") %>">Download</a>
            <skill:SubmitButton runat="server" CssClass="danger ghost" Text="Delete" CausesValidation="false" Visible="<%# Manage %>" CommandArgument='<%# Eval("Id") %>' OnCommand="Delete_Command" data-confirm="Delete this file? The course returns to Draft for review." />
          </div>
        </article>
      </ItemTemplate>
    </asp:Repeater>
    <asp:PlaceHolder ID="NoFiles" runat="server"><p>No files have been added yet.</p></asp:PlaceHolder>
  </div>
</section>

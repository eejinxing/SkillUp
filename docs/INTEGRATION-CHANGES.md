# ASP.NET / Web Forms integration changes

- Existing .NET Framework 4.8.1 Web Forms project and original template pages are retained.
- Default.aspx.cs redirects to ~/Frontend/Home.aspx.
- 46 actual .aspx/.aspx.cs pairs share Frontend/SkillUp.Master; each Page_Load supplies an initial client route.
- The user's confirmed requirement permits existing JavaScript behaviours. Forms were not rewritten as postback controls.
- The master does not wrap the JavaScript form markup in a nested server form.
- .csproj includes the .aspx/.master runtime as Content and their C# code-behind/designer files as Compile with DependentUpon.
- Frontend/Web.config adds IIS static MIME types for WebP, SVG, TTF and JSON.
- The optional .csproj.user start page is Default.aspx.
- Existing integration files will be backed up under App_Data/HandoffBackups/20261020 before copying.
- No database, real authentication service or production backend is claimed.

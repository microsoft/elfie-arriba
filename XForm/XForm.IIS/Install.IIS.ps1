# To Run:
#  - Open an Elevated Prompt
#  - PowerShell -File Install.IIS.ps1

# Install IIS with features required for Arriba/Elfie/XForm web hosting
function Install-IIS
{
   # Internet Information Services
      # Web Management Tools
         # IIS Management Console    
      # World Wide Web Services
         # Application Development Features  -> ASP.NET 4.5, WebSocket Protocol
         # Common HTTP Features              -> Default Document, Directory Browsing, HTTP Errors, HTTP Redirection, Static Content
         # Health and Diagnostics            -> HTTP Logging, Tracing
         # Performance Features              -> Dynamic Content Compression, Static Content Compression
         # Security                          -> IP Security, Request Filtering, Windows Authentication
   
    Enable-WindowsOptionalFeature -Online -All -FeatureName IIS-ManagementConsole, IIS-ASPNET45, IIS-WebSockets, IIS-DefaultDocument, IIS-DirectoryBrowsing, IIS-HttpErrors, IIS-HttpRedirect, IIS-StaticContent, IIS-HttpLogging, IIS-HttpTracing, IIS-HttpCompressionDynamic, IIS-HttpCompressionStatic, IIS-IPSecurity, IIS-RequestFiltering, IIS-WindowsAuthentication
}

Install-IIS
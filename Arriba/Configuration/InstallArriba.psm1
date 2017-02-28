# Install IIS with features required for Arriba.Web.
function Install-IIS
{
    # Arriba.Web requires:
      # Web Server
        # Common HTTP Features     -> All except WebDAV
        # Health and Diagnostics   -> Http Logging
        # Performance              -> All [Static and Dynamic Compression]
        # Security                 -> Request Filtering and Windows Authentication
        # Application Development  -> .NET Extensibility 4.5, ASP.NET 4.5, ISAPI Extensions, ISAPI Filters
      # Management Tools
        # IIS Management Console    
    Install-WindowsFeature -Name Web-Server, Web-Common-Http, Web-Default-Doc, Web-Dir-Browsing, Web-Http-Errors, Web-Static-Content, Web-Http-Redirect, Web-Health, Web-Http-Logging, Web-Performance, Web-Stat-Compression, Web-Dyn-Compression, Web-Security, Web-Filtering, Web-Windows-Auth, Web-App-Dev, Web-Net-Ext45, Web-Asp-Net45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-Mgmt-Tools, Web-Mgmt-Console

    # Install IIS URL Rewrite2 component
    Invoke-WebRequest "http://download.microsoft.com/download/C/F/F/CFF3A0B8-99D4-41A2-AE1A-496C08BEB904/WebPlatformInstaller_amd64_en-US.msi" -OutFile WebPlatformInstaller_amd64_en-US.msi
    Start-Process "WebPlatformInstaller_amd64_en-US.msi" "/qn" -PassThru | Wait-Process
    Push-Location "C:/Program Files/Microsoft/Web Platform Installer"
    .\WebpiCmd.exe /Install /Products:'UrlRewrite2' /AcceptEULA
    Pop-Location
}

# Add IIS WebSites for the service, website, and HTTP redirect
function Add-Websites
{
    [CmdletBinding()]
    param (
        [string]$ProductionFolder,
        [string]$CertificateThumbprint
    )

    Import-Module WebAdministration
    
    # Remove the Default Website binding to port 80
    Remove-WebBinding -Name "Default Web Site" 

    # Add Arriba.Web site bound to default https port [443]
    $websiteFolder = Join-Path $ProductionFolder "Arriba.Web"
    New-WebSite -Name "Arriba.Web" -PhysicalPath $websiteFolder -Port 443 -Ssl -Force
    Add-WebSiteSslBinding -WebSiteName "Arriba.Web" -CertificateThumbprint $CertificateThumbprint

    # Add Arriba Service bound to https port 42785
    $serviceFolder = Join-Path $ProductionFolder "Arriba.IIS"
    New-WebSite -Name "ArribaService" -PhysicalPath $serviceFolder -Port 42785 -Ssl -Force
    Add-WebSiteSslBinding -WebSiteName "ArribaService" -CertificateThumbprint $CertificateThumbprint

    # Set Arriba Service to use Windows Authentication
    Set-WebConfigurationProperty -filter /system.WebServer/security/authentication/AnonymousAuthentication -name enabled -value false -location "ArribaService"
    Set-WebConfigurationProperty -filter /system.WebServer/security/authentication/WindowsAuthentication -name enabled -value true -location "ArribaService"
    
    # Add a Redirect site bound to port 80
    $redirectFolder = Join-Path $ProductionFolder "Redirect"
    New-WebSite -Name "Redirect" -PhysicalPath $redirectFolder -Port 80 -Force
}

# Install an SSL certificate [from a PFX file with the secret included, locked with a user credential] and return the thumbprint.
function Install-SslCertificate
{
    [CmdletBinding()]
    param (
        [string]$CertificateFilePath
    )

    $pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
    $pfx.Import($CertificateFilePath,$null,"Exportable,PersistKeySet") 

    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("My","LocalMachine") 
    $store.Open('ReadWrite')
    $store.Add($pfx) 
    $store.Close() 

    $certificateThumbprint = $pfx.Thumbprint

    $certificateThumbprint
}

# Configure a Website to use the requested SSL certificate (by thumbprint)
function Add-WebSiteSslBinding
{
    [CmdletBinding()]
    param (
        [string]$WebSiteName,
        [string]$CertificateThumbprint
    )

    $config = Get-WebConfiguration "//sites/site[@name='$WebSiteName']"
    $binding = $config.bindings.Collection[0]
    $method = $binding.Methods["AddSslCertificate"]
    $addCertificate = $method.CreateInstance()
    $addCertificate.Input.SetAttributeValue("certificateHash", $CertificateThumbprint)
    $addCertificate.Input.SetAttributeValue("certificateStoreName", "My")
    $addCertificate.Execute()
}

# Open Firewall Ports for the service, site, and redirect site
function Open-Firewall
{
    New-NetFirewallRule -DisplayName "Arriba Service" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 42785
    New-NetFirewallRule -DisplayName "Arriba Website" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443
    New-NetFirewallRule -DisplayName "Redirect Site" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80
}

function Install-Arriba
{
    [CmdletBinding()]
    param (
        [string]$ProductionFolder,
        [string]$CertificateFilePath
    )

    Write-Output "Installing IIS..."
    Install-IIS

    Write-Output "Installing SSL Certificate..."
    $certificateThumbprint = Install-SslCertificate -CertificateFilePath $CertificateFilePath

    Write-Output "Creating and Configuring IIS Websites..."
    Add-Websites -ProductionFolder $ProductionFolder -CertificateThumbprint $certificateThumbprint

    Write-Output "Configuring Firewall..."
    Open-Firewall

    Write-Output "Setting ACLs..."
    .\SetArribaACLs.cmd
}
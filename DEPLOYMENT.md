# 🚀 Microsoft Azure Live Deployment Guide

This guide walks you through deploying the **HRM System** live to Microsoft Azure.

---

## 📋 Resource Details Summary

| Setting | Value |
|---|---|
| **Resource Group** | `rg-hrmsystem-prod` |
| **Region** | `Central India` |
| **Azure SQL Server** | `sql-hrmsystem-prod.database.windows.net` |
| **Database Name** | `HRMSystemDB` |
| **Admin Login** | `hrmadmin` |
| **Connection String** | `Server=tcp:sql-hrmsystem-prod.database.windows.net,1433;Initial Catalog=HRMSystemDB;Persist Security Info=False;User ID=hrmadmin;Password=HrmSystem@2026!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;` |
| **Pre-built Deployment Package** | `HRMSystem-Azure-Deploy.zip` (in project root) |

---

## ✅ Completed Automated Actions

The following steps have already been completed and verified:
1. **Azure SQL Connectivity Verified**: Connected to `sql-hrmsystem-prod.database.windows.net:1433`.
2. **EF Core Migrations Applied**: All database tables, schema, foreign keys, and indexes have been generated on `HRMSystemDB`.
3. **Database Seeded**: System Administrator user, standard roles (`Admin`, `Manager`, `HR`, `Employee`), leave types, and office location have been created.
4. **Integration Tests Passed**: Verified Leave Types, Profile Requests, Employee Validation, Designation Hierarchy, and SMTP Email Sending.
5. **Codebase Hardened**: Added `EnableRetryOnFailure` (handling Azure SQL error 40613), `ForwardedHeaders`, HTTPS/HSTS redirection, and `/health` probe.
6. **Release Package Created**: `HRMSystem-Azure-Deploy.zip` (25 MB ready for zip deployment).

---

## 🛠️ Step-by-Step Instructions to Go Live

### Step 1: Allow Azure Services in SQL Server Firewall
1. Open the [Azure Portal](https://portal.azure.com/).
2. Navigate to your SQL Server: **`sql-hrmsystem-prod`**.
3. In the left menu under **Security**, click **Networking**.
4. Under **Exceptions**, check the box:
   > **"Allow Azure services and resources to access this server"**
5. Click **Save** at the top.

---

### Step 2: Create the Azure App Service (Web App)
1. In the Azure Portal search bar, type **App Services** and click **+ Create** > **Web App**.
2. Fill out the **Basics** tab:
   - **Subscription**: Your Azure subscription.
   - **Resource Group**: Select `rg-hrmsystem-prod`.
   - **Name**: Choose a unique name (e.g. `hrm-live-central` or `hrmsystem-prod`).
     *(Your live URL will be `https://<your-app-name>.azurewebsites.net`)*.
   - **Publish**: **Code**.
   - **Runtime stack**: **.NET 9 (STS)**.
   - **Operating System**: **Linux** (recommended) or **Windows**.
   - **Region**: **Central India**.
   - **Pricing Plan**: **Basic B1** (or **Free F1** for testing).
3. Click **Review + create** -> **Create**.
4. Wait 1–2 minutes for the deployment to finish, then click **Go to resource**.

---

### Step 3: Configure Environment Variables & Connection String
1. On your App Service page, in the left menu, select **Configuration** (or **Environment variables** on newer Azure portal UI).
2. Go to the **Connection strings** tab:
   - Click **+ Add / + New connection string**:
     - **Name**: `DefaultConnection`
     - **Value**:
       ```
       Server=tcp:sql-hrmsystem-prod.database.windows.net,1433;Initial Catalog=HRMSystemDB;Persist Security Info=False;User ID=hrmadmin;Password=HrmSystem@2026!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
       ```
     - **Type**: `SQLAzure`
3. Go to the **Application settings** tab (or **App settings**):
   - Click **+ Add / + New application setting**:
     - **Name**: `ASPNETCORE_ENVIRONMENT`
     - **Value**: `Production`
   - Click **+ Add**:
     - **Name**: `EmailSettings__Password`
     - **Value**: `ofax jmmq mzuc ioai`
4. Click **Apply** or **Save** at the top.

---

### Step 4: Deploy `HRMSystem-Azure-Deploy.zip`

Choose **any one** of the following simple methods:

#### Method A: Browser Drag-and-Drop (Kudu / ZipDeployUI) — *No Tools Needed*
1. In your browser, open the Kudu deployment URL:
   ```
   https://<your-app-name>.scm.azurewebsites.net/ZipDeployUI
   ```
   *(Replace `<your-app-name>` with your actual app name, e.g. `https://hrmsystem-prod.scm.azurewebsites.net/ZipDeployUI`)*.
2. Drag and drop **`HRMSystem-Azure-Deploy.zip`** directly onto the file list in the browser window!
3. Kudu will automatically unzip and start the application.

#### Method B: VS Code Azure App Service Extension
1. Install the **Azure App Service** extension in VS Code.
2. Sign in to your Azure Account.
3. Expand **App Services** in the Azure tab, right-click your Web App -> **Deploy to Web App...**
4. Select the **`HRMSystem/publish`** folder or `HRMSystem-Azure-Deploy.zip`.

#### Method C: Azure CLI (if installed)
```bash
az webapp deploy \
  --resource-group rg-hrmsystem-prod \
  --name <your-app-name> \
  --src-path HRMSystem-Azure-Deploy.zip \
  --type zip
```

---

### Step 5: Verify Your Live System

1. Visit your live URL:
   ```
   https://<your-app-name>.azurewebsites.net
   ```
2. Verify Health Check:
   ```
   https://<your-app-name>.azurewebsites.net/health
   ```
   *(Should return `{"status":"Healthy"}`)*
3. **Log in to Admin Portal**:
   - **Username**: `admin`
   - **Password**: `Admin@123`
4. **Configure Real Office Location for Geofencing**:
   - Go to **Admin Portal** -> **Office Locations**.
   - Edit the default location or add your company office latitude & longitude.
   - Because the live site runs on **HTTPS**, your employees' mobile phones and laptops will now be able to grant GPS permissions and clock in within the geofenced office radius!

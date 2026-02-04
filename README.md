# Vue

Each Vue is specified by a set of survey ids, and some configuration of how to display those surveys on a dashboard.
BrandVue products are built on the Vue platform.

See [technical overview](https://github.com/Savanta-Tech/Vue/blob/master/doc/Technical%20Overview.md) for concepts and architecture

Also see [Runbook](https://github.com/Savanta-Tech/Vue/blob/master/Vue%20Runbook.md) for deployment/troubleshooting

The direction of Vue is to show useful information out of the box for any given survey. Read more in [where's vue going](where's-vue-going.md)

Readmes for sub-systems:
* https://github.com/Savanta-Tech/Vue/tree/master/src/DashboardBuilder/README.md
* https://github.com/Savanta-Tech/Vue/blob/master/src/CustomerPortal/README.md

There are also two prototype AI prototypes created by Ciklum here:
* https://dev.azure.com/savanta/AI-External/_git/BrandVue
* https://dev.azure.com/savanta/AI-External/_git/TrendVue

## Apps

The main uses of the Vue platform are:
* BrandVue tracker dashboards where we own the data so we can resell to many brands - Savanta owns the data so resells it many times as a product
* AllVue dashboards which can create a dashboard for any survey (for internal and/or external use) - client usually owns the data so only they and savanta are allowed to see it.

BrandVue EatingOut is an example
* Survey: I:\Shared\UK\Clients\Vue\BrandVue\Sector Eating Out\Surveys\201910 - October
* Map file: I:\Shared\Systems\Dashboards\BrandVue-Eating Out

The `appSettings.json` file contains a lot of useful config details, such as the name of package you're running for (e.g. EatingOut, retail etc.) and sql connection strings, among others.

## First time setup
1. Ensure you have the correct version of node specified in the new starters script, and set it up as documented here: https://github.com/Savanta-Tech/NewStarters#visual-studio-configuration
1. Install Visual Studio 2022+. Version 16.8.3 Latest confirmed configuration:
    * VS 2022 Professional
    * Workloads:
        * ASP.NET and web development
        * Node.js development
        * .NET desktop development
        * .NET Core cross platform development
    * Individual components (additionally to the preselected ones):
        * .NET Framework 4.7.1 targeting pack
		* .NET 6.0 SDK
    * Alternatively you can use Rider or VS Code or other suitable IDE
      * For VS Code, type `@recommended` into the extensions search bar before attempting debugging C#
2. Install SQL Server 2022 (Developer edition) *with instance name: "SQL2022"*:
    * Use Custom Installation, leave default values everywhere except for:
        * Feature Selection: Database Engine Services
        * Instance Coniguration: Named instance: SQL2022, Instance ID: SQL2022
        * Server Collation: Latin1_General_CI_AS (this is not the default collation)
            * Server Configuration > Collation > Customize > Windows Collation designator and sort order
               * Collection designator: Latin1_General
               * Click Accent-sensitive
        * Database Engine Configuration: Windows authentication mode, click on Add current user
    * Detailed walkthroughs:
        * https://www.youtube.com/watch?v=yasfZuou3zI
        * I:\Shared\Knowledge, Library and Templates\Technology Library\Systems information\Machine Setup
3. Optionally, but very usefully, install SQL Server Management Studio.
4. Install the [NPM Task Runner extension](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.NPMTaskRunner64) (need to close VS for installation to complete)
    * Using the Task Runner Explorer window that this populates is just a quick way of running npm commands from powershell in the `src\BrandVue.FrontEnd` directory
5. Restore [VueExport](#VueExport) DB locally
6. Restore [BrandVueMeta](#BrandVueMeta) DB locally
7. Get [Configurations/Weightings](#Configurations/Weightings)
8. Check appsettings.json file in BrandVue.Frontend.Core Project - if "loadConfigFromSql" is set to false then add "loadConfigFromSql": true into secrets.json to override
9. Run `npm run install-cached` from the `src\BrandVue.FrontEnd` directory (you can do it using Task Runner as well).
10. Build the `src\BrandVue.sln` or `src\AllVue.sln` solution in choosen IDE
11. You are now ready to start solution. Choose a [launch profile](#launch-profile) 

## Developing/Debugging
* If you're working on the front-end, Visual Studio Code has superior refactoring to VS/ReSharper

* **To use DashboardBuilder metadata**
  * Get [Configurations/Weightings](#Configurations-and-Weightings)
* **To use new Data Warehouse (answerTable) data**
  * Get [VueExport](#VueExport) database
* Build the solution
* Run `npm build:api`
* Set the UI continuously building by running `npm run build-delayed:hotdev` from the `src\BrandVue.FrontEnd` directory, or clicking Tools...Task Runner Explorer (if you have [NPM Task Runner extension](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.NPMTaskRunner) installed)
    * You do NOT need to restart the back-end to see front-end changes when using this
    * TS/JS is hot-reloaded but less/css changes currently require a manual refresh after compilation completes
    * This serves the an in-memory version of the dist folder to speed things up
* Set BrandVue.FrontEnd as the "startup project".
* Choose a [launch profile](#launch-profile)
* By default EatingOut will be loaded. If you want to run anything else, change the `productsToLoadDataFor` value in the `appSettings.json` file to the name of the package you want to load.
* To set up using the certificate, install the certificate development.pfx (the passphrase is `apples`) in your certificate store under"Trusted Root Certificate Authorities", and then you can run hotdev-cert
* Now pressing F5 will launch IIS and a browser.
* **To debug frontend tests**
* Run `npm run test:debug` and navigate to `chrome://inspect` in Chrome. A remote target will appear. Open the dev tools and click inspect. This will start a debugging session for the tests. If you want to hone in on some specific tests you can do so with the `-t '<describeString> <itString>'` argument. Don't forget to escape the npm parameters with `--`. For example `npm run test:debug -- -t 'Entity set management Changing SplitBy should select the last entity set for this entity type chosen by the user'`. See jest docs for more examples.

### Launch Profile 
Choose a launch profile based on what IDE you are using:
    * If running through Visual Studio, select the `VisualStudio: Connect to HotDev proxy` launch profile.
    * If running through JetBrains Rider (easyRider 🏍) then select the `Rider: Connect to HotDev proxy` launch profile.

### Configurations and Weightings

Run `local-scripts\Build-MetadataFromEgnyte.ps1` to populate your testdata folder for brandvue dashboards from the map files
Or, get the most recent metadata package from [Octopus library](https://savanta.octopus.app/app#/Spaces-1/library/builtinrepository) (e.g. EatingOut) - unzip into the "TestData" sub-folder of the repo matching the name of the package you downloaded (e.g. EatingOut)

### VueExport

Default: Dev/test/beta/uat use VueExport
* Its data is updated every morning to include (non-restricted) surveys with responses in the last 3 months - adjustable [here](https://github.com/Savanta-Tech/SurveyPlatform/blob/8610dc92e86271d140ee0ec049fea3b715d65752/DatabaseSchema/dbo/Stored%20Procedures/SqlAgent_DataWarehouseGenerateNightlyExport.sql#L19-L22).

  * Get the most recent (~10GB) data package from [ftp.savanta.com](https://savantaftp.egnyte.com/app/index.do#storage/files/1/Shared/Savanta/Backups) (covers all surveys)
    * If bandwidth constrained, try this 9MB single-survey backup: `I:\Shared\06 Tech\Vue\VueExportCharities-2020-12-only (use if bandwidth constrained).bak`, or generate your own for just the surveys you need using e.g. `https://fieldvue.savanta.com/api/surveyextract/generatebackup?surveyids=10820,11145`
    * Your will need to temporary premote the user to admin in order to allow the backup to happen
   
    * ![image](https://github.com/user-attachments/assets/10f5a25f-07be-4b8a-9101-5be1765a45e2)

  * If not done previously, create a master key and install the encryption certificate as described at https://github.com/Savanta-Tech/TechWiki/wiki/SQL-Server-Backup-Encryption 
  * In SSMS restore the .bak file onto your (local)\SQL2022 instance **with the name "VueExport"**
  * If you want to restore a backup directly from your downloads folder, you'll need to right click the downloads folder, select properties->security, then give `NT Service\MSSQL$SQL2022` permission
  * Update the appSettings.json connection strings to point to the specific database name if you forgot to override the name to VueExport

Exception: Survey product for test/beta points at the corresponding test/beta databases that can be published to via test/beta desktop tools, and completed on the test/beta survey site.

### BrandVueMeta

Contains the config for all dashboards (not just BrandVues)

📣 The config db for test/snapshot/uat is totally reset to a snapshot of live each morning
📣 Test and beta feature toggles are reset to enabled for all of Savanta each morning (by the daily sql job)
* (beta is not, because Ed uses it to create and test metrics/pages before putting them live, so resetting interrupts that)
* This can be restored by running the script in local-scripts: `Restore-LatestBrandVueMeta.ps1`, also available in Task Runner Explorer: `restore-brandvuemeta`. This can also be done manually:
* You can get a copy of it from the test box or from [Azure](https://portal.azure.com/#view/Microsoft_Azure_Storage/ContainerMenuBlade/~/overview/storageAccountId/%2Fsubscriptions%2F4e489b50-8ad1-4160-99d0-bc1b76e006b9%2FresourceGroups%2FSavanta_Tech_Backups%2Fproviders%2FMicrosoft.Storage%2FstorageAccounts%2Fsavantatechbackups/path/database-backups/etag/%220x8DBA89E76DFB9AE%22/defaultEncryptionScope/%24account-encryption-key/denyEncryptionScopeOverride~/false/defaultId//publicAccessVal/None) (Storage Accounts -> savantatechbackups -> containers -> database-backups -> live -> BrandVueMeta)


### Accessing the full data set on the live server during debugging
* If you need access to the full data set for debugging then it is possible to connect BV running locally in the debugger to the full data set on the live server;
* Please only do this if you need access to the full data set as it could cause performance problems on the server and also create potentially long running transactions;
* You will need to connect to the Azure VPN, use the "run as" trick on visual studio (same as using SSMS locally) and set the connection string with read only intent

## Data Warehouse

### Synchronising Survey Data and Answers tables

* The data and answers tables are synced every 5 minutes to ensure that the answers table is kept up to date with the latest survey response data.
* Under certain circumstances - back coding of data for example - it may be necessary to force sync the historic data between the data and answers tables.
* To force the sync, _log in to FieldVue_ and _then_ enter the URL: `https://fieldvue.savanta.com/api/surveyextract/forceresyncforsurveys?surveyIds=4410,4571,4682&resetTideMark=true`, replacing the survey Ids with a comma separated list of survey Ids that are to be synced.
  * ⚠️ Each data resync slightly degrades the organization/performance of the columnstore index. Try to avoid them as much as possible
* If you want to monitor the progress of the sync, query the ```[SurveyPortalMorar].vue.SyncStates``` and ```[SurveyPortalMorar].vue.SyncLogMessages``` tables.
* Setting the resetTideMark parameter to true results in the MostRecentResponse being set to null in the SyncStates table for the specified survey ids.


## Local testing
If you need the full data set for a set of surveys, it's possible to request one from the [SurveyExtractController](https://github.com/Savanta-Tech/SurveyPlatform/search?q=ExportExtract&unscoped_q=ExportExtract)

*(Optional - if you're using "IIS Express" you don't need to do this)*
1. Ensure you have IIS installed [with at least these features selected](https://user-images.githubusercontent.com/2490482/32330109-fee1dff6-bfd6-11e7-8cb1-af899951639f.png). You can get to this through the Windows "Turn Windows features on or off" panel, although Microsoft keep moving where this is.
2. For full IIS or using the "Run Dev from disk", run `npm build:dev` or `npm run watch` to create the dist folder.

## Performance
BrandVue crunches a lot of data. If you are changing a subclass of IMeasureTotalCalculator or IUnweightedTotaliser you are likely on a hot code path, and should consider checking the benchmark test results compared to master

## Deployment
* The master branch is deployed to test by [Azure Devops](https://dev.azure.com/savanta/tech/_build?definitionId=22) and promoted to other environments using [Octopus](https://savanta.octopus.app/app#/Spaces-1/projects/brandvue/deployments)
* The data is created by the [Dashboard Builder](https://github.com/Savanta-Tech/Vue/tree/master/src/DashboardBuilder#nightly-deployment-pipeline) which has more information about its deployment
* Use [this Audit page](https://savanta.octopus.app/app#/Spaces-1/configuration/audit?projects=Projects-103&eventCategories=DeploymentQueued&from=2022-04-18T00%3A00%3A00.%2B01%3A00&to=2022-05-18T23%3A59%3A59.%2B01%3A00&includeSystem=false) in Octopus to see BV deployments people have made in the last 30 days.


### Products

Each BrandVue product is defined as described in [auth server usage docs](https://github.com/Savanta-Tech/Auth-Server/blob/master/Usage-Docs.md).
There is also an [Octopus deploy step](https://savanta.octopus.app/app#/Spaces-1/projects/brandvue/deployments/process/steps?actionId=e55ff1be-b104-4623-928b-844452711690), which deploys a separate web app for each product. The step is identical in most cases, but the variable ProductsToLoadDataFor and a few others are configured to differ dependant on the step
Finally the survey ids are defined here: https://github.com/Savanta-Tech/Vue/blob/c6a3caa62be33e8dc35a5e10c4f71f0fd7c2b65f/src/BrandVue.FrontEnd/Services/ProductContextProvider.cs#L16-L243

### Hotfixes
A hotfix avoids releasing other changes that have gone into master since the live release.
1. Find the version currently on Live (or Beta) in Octopus, e.g. 1.0.999
1. Checkout master branch and pull
1. Find the full tag name, e.g. git tag -l "*1.0.999*"
1. Checkout the tag using the full tag name, e.g. git checkout release-1.0.999
1. Create a new branch from the tag starting with "hotfix-", e.g. git checkout -b "hotfix-sc-666-fix-the-world"
1. Push your automated tests and fix
1. Make sure your change has been built and deployed to the main Test app, **not a feature branch app!**. This is done by building the hotfix branch, not the PR branch, because it has the right 'hotfix' naming convention. We currently automatically build PR branches only, so you need to manually trigger a branch build by running a custom build in TC and selecting your hotfix branch in Changes tab. To ensure this has worked, check the version released to Test on Octopus Deploy.
1. Once it's built on Azure Devops, move that build through the Octopus environments with appropriate testing as normal.
1. At this point you can also merge it to master to ensure it gets into future releases.

### Tests

* Run NUnit tests within Visual Studio
* Run `npm run test` from command line for UI unit tests
  * See [this tech wiki article](https://github.com/Savanta-Tech/TechWiki/wiki/Testing-BrandVue-Front-End-with-Jest-and-React-Testing-Library) for more info on testing react components
* Run `npm run cy` for cypress tests - you'll need to enter a valid username/password/environment - [more here](https://github.com/Savanta-Tech/Vue/tree/master/src/BrandVue.FrontEnd/cypress)
  * You can get notified when the automated run fails here:
https://dev.azure.com/savanta/_usersSettings/notifications
![image](https://user-images.githubusercontent.com/2490482/216592468-948d5dcb-b94d-4319-acba-9a36d11fc006.png)

On Azure Devops they occasionally hang (printing nothing for half an hour or so until the build times out) for reasons that seem related to this: https://github.com/cypress-io/cypress/issues/8206#issuecomment-1099738769


#### Verification / characterization tests

See those .verified files? They're created by [VerifyTests](https://github.com/VerifyTests/Verify#snapshot-management)
Grab a resharper or rider addin from the link above to easily diff/update the files when the tests fail.

#### Coverage and mutation testing

1. Reset your local master to the base of your branch for maximum accuracy
2. Open powershell in src\Test.BrandVue.SourceData
3. Run `dotnet tool restore` (if this fails, temporarily untick the azure package source in visual studio options and try again)
4. Run `dotnet stryker`
5. Look at the generated report

### UAT

The UAT environment:
* **Code**: Copy of beta (deployed at same time)
* **Auth**: Uses beta auth site (so external users won't generally have accounts), but assigns all surveys to the savanta org (when restoring VueExport)
* **Config**: Snapshot of last nights live configuration
* **Product**: "Survey" product only
* **Data**: [VueExport](#VueExport)


For convenience, you can jump through to surveys from here: https://savanta.uat.all-vue.com/projects/?projectStatus=1

⚠️ There isn't enough info in VueExport to make UAT Customer Portal work properly. **Don't try to test Customer Portal stuff there** (e.g. the dates are wrong and the quotas/documents are missing), it's just there as a quick link to all the surveys.

#### How it is set up

*   There's a new website "UAT-Vue" (+certifytheweb setup) in test bound to *.uat.all-vue.com
*   The live db server task which saves VueExport nightly also now pushes BrandVueMeta backup into azure blob
*   The test db server task which restores VueExport each morning now overwrites BrandVueMetaUAT with that days backup from Azure blob
*   Octopus setup for Customer Portal and Vue survey both have an extra step
    *   They set "Environment" variable to UAT for that step (which is used in site name, etc.)
    *   Their answers connection string points at VueExport
    *   The MetaConnectionString points at BrandVueMetaUAT

​​​​​​​

## Troubleshooting
* Typescript build issues
  * Permissions sounding thing? In a non-elevated powershell window: `Set-ExecutionPolicy -Scope CurrentUser Unrestricted`
  * If this doesnt work, try: `Set-ExecutionPolicy Unrestricted -Scope LocalMachine` and `Set-ExecutionPolicy Unrestricted -Scope CurrentUser`, make sure python is installed and do a restart
  * Make sure you've done an `npm install`
  * Delete node_modules then `npm install` again
  * Ensure the C# built, and that the `BrandVueApi.ts` file has been build (`npm run build:api`)
* Unable to download images (e.g. a "Failed to capture image" error)
  * This functionality works using a chromedriver nupkg. The version of this needs to match the version of chrome installed on your computer, so updating the chromedriver package in Brandvue to the latest should allow it to work locally
* SSL error running locally:
  * Another site has stored an HSTS header against localhost, go to chrome://net-internals/#hsts and "Delete domain security policies" for "localhost" using the bottom box
## Migration Scripts
In BrandVue we use Code-First approach to create and modify DB. We use Entity Framework to do this.

For the activites below, you need dot net entity framework core installed. If you type `dotnet ef` at the command line and do _not_ see a unicorn, then you need to install/update this with `dotnet tool install --global dotnet-ef` or `dotnet tool update --global dotnet-ef`

You may need to suffix `--interactive` to the above and login to avoid a 401

```
                     _/\__
               ---==/    \\
         ___  ___   |.    \|\
        | __|| __|  |  )   \\\
        | _| | _|   \_/ |  //|\\
        |___||_|       /   \\\/\\

```
### How to Add/Modify DB objects - Package Manager.
If you want to do any db changes (e.g. add column to the existing DB or add new table) in Package Manager:

1. Open `Package Manager Console` in Visual Studio
1. Choose `BrandVue.EntityFramework` as **Default project**
1. For **adding** just type `Add-Migration NameOfNewMigration -c NameOfRequiredContext`. For **updating** just type `Update-Database -c NameOfRequiredContext`.
1. That's it!!

For more information you can read this article https://www.learnentityframeworkcore.com/migrations/commands/pmc-commands or use built in help command like `Add-Migration -h`

### How to Add/Modify DB objects - dotnet.exe.
If you want to do any db changes (e.g. add column to the existing DB or add new table) in dotnet.exe:

1. Open `Developer Powershell` in Visual Studio
1. Change directory to the `BrandVue.EntityFramework` project
1. For **adding** just type `dotnet ef migrations add NameOfMigration --context NameOfRequiredContext`.
1. For **listing** just type `dotnet ef migrations list --context NameOfRequiredContext`.
1. For **updating** just type `dotnet ef database update <name from list> --context NameOfRequiredContext`.
1. That's it!!

For more information on running EF core commands in Rider you can read this article https://blog.jetbrains.com/dotnet/2020/11/25/getting-started-with-entity-framework-core-5/ or use built in help command like `dotnet ef migrations add --help`

## Public API
The API provides: survey defintion, survey response data, and result aggregation (tailored towards reproducing BrandVue metric results).

The developer docs are available to users of each product (e.g. https://savanta.all-vue.com/retail/developers/docs) as well as anonymously at http://developers.savanta.com//docs/
For internal consumption, we have an example C# client https://github.com/Savanta-Tech/ApiDataPreparer/ and a python client for metric results consumption [get-all-metric-results.py](https://github.com/Savanta-Tech/Vue/blob/master/src/BrandVue.FrontEnd/developers/get-all-metric-results.py). Related to this script is the python metric results snapshot client [get-weighting-measure-results-snapshot.py](https://github.com/Savanta-Tech/Vue/blob/master/src/BrandVue.FrontEnd/developers/get-weighting-measure-results-snapshot.py). This can be run on any dashboard to take a "snapshot" of all metrics and their results for all averages and subsets as well the weighting strategy used. Previous runs are stored in I:\Shared\06 Tech\Vue\Metric Results Snapshots

### Build process
* Build BrandVue.FrontEnd in Visual Studio
* Run `npm run build:api` - NSwag converts the dll into an open api json file containing route details and xml-doc
  * The output can be totally customized using [NSwagBrandVueOpenApiDocumentProcessor](https://github.com/Savanta-Tech/BrandVue/blob/07578ef954fd607ea62080d9885730395929abca/src/BrandVue.FrontEnd/PublicApi/NSwag/NSwagBrandVueOpenApiDocumentProcessor.cs)
* Run `npm run build:docs`
  * Widdershins converts the open api spec into a markdown document
  * Shins converts that, and the "includes" markdown docs into an html doc using the "templates" folder

### Making changes

Locally, when running without an attached auth server, use `ThisIsTheDebugApiKey` as the api key

If you changed/removed something on the Public API:
* STOP. DON'T. You'll break external users. Only do so if necessary and with substantial (months of) warning to users.
* The best way is to add a new thing next to the old thing, then remove the old thing 6-12 months down the line. We'd want to email everyone who has an API key.

If you added something to the Public API:
  * You must update the [markdown](https://github.com/Savanta-Tech/Vue/tree/master/src/BrandVue.FrontEnd/developers/docs/source/includes) and endpoint (xmldoc) as appropriate.
  * If it supersedes something else, e.g. You needed to rename something, make it very clear in the xmldoc on what date the old thing will go away (it's totally possible to then hide it from the docs at some point but leave it in the API)

Doc guidelines:
* Xml doc should only explain implementation details and practical usage details. Conceptual info goes in the markdown docs.
* Our docs will tell people that the API is case sensitive. But wherever possible we should aim to be forgiving and use case-insensitive checks.

### API Keys
API keys are generated on the [auth server API keys page](https://monzo.test-vue-te.ch/auth/ApiKeys)
 * Always generate and use them on the TEST server since they're per environment.
 * They last for about 10 years unless revoked, and the claims within them never change from generation time.
 * The generation page is intentionally not linked from elsewhere for now, no-one outside the dev team needs to know about it *yet*.
 * Here's one you can use in live [SavantaPrivateTestingMonzoApiKey.txt](https://migglobal.egnyte.com/app/index.do#storage/files/1/Shared/06%20Tech/Vue/User%20Manager)
 * Please don't go crazy creating lots of keys - our management and revocation still needs work.
   * Once that and the UI is improved, we may allow users to create their own API keys, for now it's down to account managers to raise support requests as appropriate.

#### Technical details
* To generate the keys the auth server UI acts as a client for itself.
* It uses a custom grant type to delegate a *subset* of a user's permissions to a token.
* Those claims are then stored as JSON in the database against a hash of the generated token, and linked to the token owner.
* BrandVue uses the OAuth2 introspection API provided by identity server to retrieve the claims for an API key it receives.

### Model types
* A Controller should take PublicApi models rather than strings (and built-in types such as int and DateTime).
* A ModelBinder should be used to allow the caller to pass a string, but our code to receive a validated, typed model.
* The Models should be wrappers around the underlying system types that are:
  * IEquatable and implement ToString (for test purposes)
  * Have an internal property with the underlying SourceData type
  * Have an implicit conversion to that system type to return the property

### API Diagrams
There are one or more diagrams to show the API model. These are available in google drive - use draw.io to edit.
* https://drive.google.com/file/d/1AmBGbXhGwuE05pUtuHB4ioKH7GVAAAB5/view?usp=sharing API model & relationship between responses from endpoints


## VueReporting
Reporting for Vue. Uses a mixture of the internal Vue API and Selenium visiting the BrandVue pages and screenshotting them.

### User flow

The reporting system is presented to our (internal only) users as an iFrame dash pane within BrandVue.
A user can create a template powerpoint file (pptx), upload it to the system.
They can then generate reports for varying time periods and brands.
In addition to verbatim content, the template can contain:
* Tags: e.g. #Date#, #BrandName# will be replaced in text during generation
* Charts: Use the Save Chart button from a BrandVue page to download a chart, and drop it into the slide.
  * During generation this will be updated to match the selected date/brands, and the link replaced with a bookmark link
  * This works because the image properties contain a link to the generating page

### Development

Install the [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (if you don't the app may appear to compile but then not start).

So that Visual Studio uses the version of npm and any tools it installs rather that built-in Visual Studio versions:
Go to Tools...Options...Projects and Solutions...Web PackageManagement, then put `.\node_modules\.bin` at the top of the list
If you don't do this, very confusing errors will ensue.

You may also need to install the [.NET runtime windows hosting bundle](https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-aspnetcore-6.0.0-windows-hosting-bundle-installer).

If you wish to run in full IIS, you will probably need to install a .NET core component for that too.

### Locally embedding with brandvue
* Set a local copy of BrandVue to point at your local reporting instance, e.g.
  * Set the appSettings.json productsToLoadDataFor to "eatingout", ReportingApiAccessToken to the same as Reporting
  * In testdata/eatingout/config/DashPanes.csv the spec column of the reportin row should point to http://localhost:15943/reporting/ui/jobs (or wherever that page will be locally)
  * Set the appSettings.json for Reporting: ReportingApiAccessToken to the same as BrandVue, BrandVueOverride to your BrandVue root url (http://localhost:8082 for example), and AppendProductNameToRoot to false
* Allows iframes from different ports (temporarily!)
  * For firefox: https://addons.mozilla.org/en-US/firefox/addon/cross-domain-cors/
  * For chrome: "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" --disable-web-security --disable-gpu --disable-features=IsolateOrigins,site-per-process --user-data-dir="C://ChromeDev"

## SurveyVue (part of AllVue)

SurveyVue is the internal-tech name for the product with a subproduct per survey
* To run an instance of SurveyVue, update the following config file settings:
	* productsToLoadDataFor: "survey"
	* useSubProductPathPrefix: true
	* chartConfigurationEnabled:true

* It would also be advisable to update the launchUrl in the launch settings to include a valid surveyId (such as 11357) after the localhost root, otherwise you can update the url manually on launch to view a selected survey
* To view anything on the quotas or documents tab, refer to the CustomerPortal project in the SurveyPlatform repo

# Configuring the local user when not connecting auth server
By default when running BrandVue/AllVue locally it will run without Authentication and simulate a logged in user with 
* UserId='LocalUserId'
* Username = 'tech@savanta.com'
* Role = 'System Administrator'
* Access to all brandvues (eatingout, retail, finance etc))
* Access to all companies/surveys

Sometimes you may wish to use a different role, in which case you can follow the steps below.
1. Open the file `src/BrandVue.FrontEnd/AuthMiddleware/Local/LocalAuthenticationApiClient.cs`
2. Change the RoleName from 'System Administrator' to the role you wish to test with. eg User


3. Open the file `src/BrandVue.FrontEnd/AuthMiddleware/Local/LocalAuthenticationMiddleware.cs`
4. Change the Role from 'System Administrator' to the role you wish to test with. eg User

(AllVue only)
5. Open the file `src/BrandVue.FrontEnd/AuthMiddleware/SubProductSecurityRestrictionsProvider.cs`
6. Change _requireAdmin = false;

# Connecting to local auth server

By default, BrandVue bypasses authentication when running locally, and sets you up with a dummy "anon@local.com" admin account. If you want to test BrandVue locally with a local Auth Server, you need to make a few changes:

1. Change the following app settings:
    1. Set allowLocalToBypassConfiguredAuthServer to false
    2. Set authServerUrl to my local Auth Server url (default is https://localhost:44378)
    3. Set authServerClientSecret to the unhashed BrandVueClientSecret set in the Auth Server's appsettings.development.json 
2. Add a valid redirect uri to the ClientRedirectUris table for: http://localhost:8082/signin-oidc
3. Ensure the company in localOrganisationOverride and the company attached to the user in local auth are the same
4. Ensure the `authCompanyId` column in the [VueExport].[surveys] table matches the GUID of the current organisation from the [MIG.Auth.Server.Users].[dbo].[Organisations] table (e.g. for the Savanta org in live this id is "d570a72d-3ce4-4705-96fe-39a9b7ca132c", you'll need to update your [VueExport].[surveys] table to change those ids to match the id for "savanta" in your [MIG.Auth.Server.Users].[dbo].[Organisations] table)
5. Use Firefox as Chrome doesn't allow Auth server (running on HTTPS) to pass a cookie back to AllVue (running on HTTP) and you get a "Correlation failed." error.

# Customer Portal
(This app by default connects to the test auth server, so you can use your normal test credentials))
See the Clubhouse milestone<br />
https://app.clubhouse.io/mig-global/milestone/28413/savanta-customer-platform-v1-0-surveys-status

For current Ux designs see<br />
https://www.figma.com/file/6HSduDJ6RX9uZckRvzgtHs/Savanta-Customer-Platform-V1.0?node-id=0%3A1


## Prerequesites:

* NPM task runner extension for VS2022 (optional) <br />
  https://marketplace.visualstudio.com/items?itemName=MadsKristensen.NPMTaskRunner64

* VS 2022 (.Net 6)


## Steps to run:

1. Open CustomerPortal solution in VS2022
2. It should install your npm packages
   * You can run the npm install at any point by opening a cmd prompt in the `CustomerPortal` folder and running `npm install`
3. Build solution (this will also generate nswag typescript code)
4. Run the Webpack Dev Server task (will normally be run automatically by NPM task runner):
   * Go to the `Task Runner Explorer` window in Visual Studio, usually found along the bottom row of tabs
      * If it's hidden you can make it visible under `View` > `Other Windows` > `Task Runner Explorer`
   * Under the `Custom` folder from the tree on the left, right click `build:hotdev` and click `Run`
   * Make sure there are no errors and the task continues running (i.e. it says `build:hotdev (running)` in the tab)
5. **Alternatively**, open a cmd prompt in the `CustomerPortal` folder and run `npm run build:hotdev`
6. Run the project

Troubleshooting [here](https://github.com/Savanta-Tech/SurveyPlatform/blob/5342595062b125d2d15e5bd0f28005db469cbd7c/CustomerPortal/README.md#common-troubleshooting)

### If running for the first time locally you'll need to get a dev copy of the database

**Note:** If you've done this before but your backup isn't relatively recent you may need to get a new backup in case of db schema changes

1. RDP to the database server and make a full backup of the `SurveyPortalDev` database (save it to the `E:` drive)
2. Copy this backup to your local machine
3. Open SQL Server locally and connect to `./SQL2017` using your Windows Authentication
4. Restore the backup you copied over to a new database called `SurveyPortalMorar`
5. Now when you run locally you should be reading data from that database

---

# Package licenses

Before installing an npm package, we should always check its license. Many packages have [permissive licenses](https://en.wikipedia.org/wiki/Permissive_software_license#:~:text=Examples%20include%20the%20GNU%20All,is%20the%20permissive%20MIT%20license.) such as MIT, Apache, BSD which allow us to use and modify the software for commercial purposes as much as we please. Others require payment for commercial use. We must avoid "copy left" (e.g. GPL) licenses which could compel us to release all our source code for the app that uses the package.

We have licenses for:
* Highcharts - Annual subscription (all versions)
  * SA-HC SaaS: 4 developers can directly work with its API in simultaneously
  * Initial invoice date: 2023-03-23, Invoice no.: 300009889, , Cust. no.: 123721, Order number: 18507, Contact IT (Harry K) for any changes.

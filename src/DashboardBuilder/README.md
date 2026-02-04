# Dashboard Builder
Transforms survey data based on a "map file" (Map.xlsx"), pushing the output database+files as Octopus packages. These packages can be loaded by the Vue platform (see the BrandVue repository).
For more on map files refer to I:\Shared\Knowledge, Library and Templates\Technology Library\Systems information\PRIVATE_Map files.docx

## Setting up locally
* The app config defaults are set to look in your I:\ egnyte drive, validate the relevant map, and do a metadata-only build into the "output" folder which will be created next to the exe.


### With data

* Install developer edition of SQL Server 2017 as a named local instance SQL2017
Goto [DatabaseScriptsForDebug directory](https://github.com/MIG-Global/Dashboard-Builder/tree/master/DatabaseScriptsForDebug) in Dashboard-Builder repo
* Run the SQL Script `SurveyPortalMorarSchema.sql` on this instance(This will create the schema)
* Go onto the live box and run TakeData.ps1 - this will take a subset of the live data and generate some data files via BCP & zip it up (You can decide to take a thin section of all the surveys or a deeper section of just a few surveys, this will depend on what you want to do and test)
* Copy this zip file (e.g. `bcp100k.zip`) onto your local box, unzip this file
* Run the script `LoadData.ps1`
* You will now have all the database data required to run Dashboard builder locally, 
* NB. The first time you want to run dashboard builder for each map file, you will need to run Dasboard builder with Lift&Shift (ie SkipTransferAll=False and MetadataOnly=False) inorder to generate the required temp data

### Running a build

* Open a command line in the folder where DashboardBuilder.exe is
* Run a [command](https://github.com/MIG-Global/Dashboard-Builder/blob/master/DashboardBuilder/CommandLineActionType.cs#L6), e.g. `DashboardBuilder Build Barometer`
  *  The second part of the command is just the short code for the map file

### Testing map changes locally

1. Make a local copy of the dashboard folder you're interested in building from `I:\Shared\Systems\Dashboards`. Put it on you C: drive somewhere.
1. Change `Egnyte.Dashboards.LocalReadOnly` app.config of dashboard builder to where you made a local copy of the map file (so that you can edit the map file freely without editing the copy in egnyte for the entire company)
1. (Optional) Change `OverrideOutputPath` app.config of dashboard builder to `..\..\..\..\..\BrandVue\testdata` (or wherever the relative path of the brandvue testdata directory lives)
1. Add a `C:\brandvuedata\backups` folder
1. Run Dashboard builder with the parameters `build Retail` where Retail is the name of the dashboard you're building. Make sure your working directory is `{repo root}\DashboardBuilder\bin\Debug\net471`
1. Run the `PostDeploy.ps1` in the output folder for the particular dashboard you've just built. This will create the database that brandvue requires


### Get the latest exe
* Download the latest artifact called DashboardBuilder.*.nupkg [here](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_DashboardBuilder_Build&branch_Vue_DashboardBuilder=%3Cdefault%3E&tab=buildTypeStatusDiv)
* Unzip it

### If you don't have the Z:\ drive
On non-mig hardware, you shouldn't map the Z:\ drive, instead:
 * Download and unzip the latest artifact from [here on teamcity](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_DashboardBuilder_PushEgnyteMetadataToOctopus)
 * Set the `Egnyte.Dashboards.LocalReadOnly` in the [DashboardBuilder.exe.config](https://github.com/MIG-Global/Dashboard-Builder/blob/master/DashboardBuilder/App.config#L40) at the root unzipped folder (i.e. the folder that *contains* `_Base`, `Eating Out`, `WGSN`, etc.)

### Validate a random map file on disk
Run `DashboardBuilder validate "c:\path\to\Map.xlsx"`

### Building data & metadata locally
#### BrandVue
* set  MetadataOnly to false in [DashboardBuilder.exe.config]
* set SkipTransferAll to false in [DashboardBuilder.exe.config] (You will need to do this the first time you run locally dashboardbuilder locally for your map file)
* run DashboardBuilder
#### MyVue
* set  MetadataOnly to false in [DashboardBuilder.exe.config]
* set SkipTransferAll to false in [DashboardBuilder.exe.config] (You will need to do this the first time you run locally dashboardbuilder locally for your map file)
* Import myVue specific data for the map file you want to test. Which exact databases you'll need depends what you're building. Some trial and error and code reading will likely be involved.
* run DashboardBuilder

### Building metadata only locally
#### BrandVue And MyVue
* set  MetadataOnly to true in [DashboardBuilder.exe.config]
* set SkipTransferAll to true in [DashboardBuilder.exe.config]
* run DashboardBuilder


## Testing in Beta
1. Do a custom run of [this build](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_DashboardBuilder_DeployMetadata&branch_Vue_DashboardBuilder=%3Cdefault%3E&tab=buildTypeStatusDiv) and make sure you set:
   * "OctoTargetEnvironment" to "Beta" (this turns on the SkipTransferAll option in App.config, meaning it uses the same database data as the previous run on live (for dashboards which transfer out to a temp database))
2. Once the build is complete, see below for the octopackage names to download
3. (OPTIONAL) To see the data, create a [release](https://octopus.morar.co/app#/projects/brandvue/overview) from the new beta package and deploy it to the test or beta site

Another option would be to get a copy of the temp database from the production server's `D:\Adhoc\DbBackup` and run it locally - you'll need to grab the latest egnyte data nuget package using this command https://github.com/MIG-Global/Legacy-Dashboard-Builder/blob/master/DashboardMetadataBuilder/Create-FromEgnyte.ps1#L12

## Nightly Deployment pipeline
* The Egnyte metadata from the dashboards folder is put into a octo package [on TeamCity](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_DashboardBuilder_PushEgnyteMetadataToOctopus)
* This [build](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_DashboardBuilder_RunAndDeploy) runs the dashboard builder
  * Forces a rebuild of the egnyte metadata project
  * Triggers an [octopus deployment](https://octopus.morar.co/app#/Spaces-1/projects/dashboardbuilder-run-full/) which:
    * Pulls the latest egnyte metadata
    * Pulls the latest DashboardBuilder build on master
    * Runs the Dashboard Builder on dashboards that don't need access to the cloud database
    * Pushes the results as octo packages called MyVue.ClientSpecifics.* and BrandVue.ClientSpecifics.* downloadable from [Octopus library](https://octopus.morar.co/app#/library/packages)
    * If the [build](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_DashboardBuilder_RunAndDeploy) was triggered by its nightly Schedule Trigger rather than manually, the [octopus deployment](https://octopus.morar.co/app#/Spaces-1/projects/dashboardbuilder-run-full/) will then start [BrandVue nightly](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_BrandVue_RedeployWithLatestData) via a powershell script
* [BrandVue nightly](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_BrandVue_RedeployWithLatestData) and [MyVue nightly](https://teamcity.morar.co/viewType.html?buildTypeId=Vue_MyVue_NightlyTaskToDeployLatestDataToAllEnvironments) TeamCity builds deploy [brandvue.data](https://octopus.morar.co/app#/projects/brandvue-data) and [myvue.data](https://octopus.morar.co/app#/projects/brandvue-data) respectively to pull the latest data packages onto the websites

## Manually running a full BrandVue data build

In Team City Vue > DashboardBuilder > Consultant runnable tasks - Click "Run" , then under the "Run custom build" pop up window, under "Dashboard builder commands to run" tick the Dashbaord(s) you want to run a fun full build for, choose "MetadataOnly" = "False" (to run a full build) and Data rebuild type = "Prefer for metadata runs [Beta]".

Next, once the Team City run is complete, wait to see the DashboardBuilder.Run.Full has been completed in Octopus:

To confirm, the initial build gets run in https://octopus.morar.co/app#/Spaces-1/projects/dashboardbuilder-run-full/overview (runs in live).

One the DashboardBuilder.Run.Full process is complete, you can create a release in BrandVue.Data in Octopus which you can promote to Beta or Live (from test) as you please.  To do this, in BrandVue.Data in Octopus, click "Create release" > then click "EXPAND ALL" > then scroll down to find the .data build for the Dashboard you want to create a release for and click "SELECT VERSION":

Then choose the latest data build that should have been published around the same time your DashboardBuilder.Run.Full finished and click OK:

#### N.B. the "version" is a UTC time for when the data build began. The published time is the *local* time when the build finished. The UTC timestamp will be an hour behind the UK when the clocks go forward for British Summer time - this is so the latest build to start is always the latest version, even if started in a different timezone.

Now click "SAVE"

Then click "DEPLOY TO" and choose either "Test" or "Beta" depending on where you want to push your data to

Then click "Deploy"

Notice this starts the deployment process - this should not take more than a couple of minutes as all that is happening is the already built data package is being copied to the right location:

Once that process is complete, then you can promote the data build to Beta and or live depending on whether or not you built your data straight to Test or Beta.  To do this, just go back to the BrandVue.Data overview page by clicking on "Overview" on the left had side of the build page.

Vue platform Runbook
====================

Overview
--------

- The Vue platform takes a survey data db (output by dashboard builder), and a Map.xlsx file, and provides that data via a dashboard and public API (external to Savanta).

- BrandVue products are built on the Vue platform. BrandVue makes use of the Savanta auth server for user login and API authentication via reference bearer tokens.

Rolling back
------------

Using Octopus to deploy a previous version of BrandVue is highly reliable when there are no config db table schema changes during a Vue release.

Using Octopus to deploy a previous version of BrandVue.Data is also highly reliable. This will fix issues caused by map file changes. To deploy a previous version of BrandVue.Data using Octopus:

1) From the dashboard select the BrandVue.Data project;

2) From the project page press <Create Release>;

3) On the create release page expand the packages drop down;

4) By default the latest packages for release are selected.  This can be overridden for each dashboard by selecting the relevant version in the "specific" version column;

5) Press the <Save> button to complete the configuration and make it available for deployment;

6) On the release page select the environment to deploy to Test, Beta or both by pressing <Deploy To...>.  It is recommended to deploy to test first and then once this has been proven to work then to beta and live;

7) Then press the <Deploy> button to deploy the release;

8) The task progress page is then displayed which shows which steps have been successfully completed.  If any of the steps fail, review the task log, fix the cause and then re-deploy the release.

Deploying the release restarts the application pools for all of the dashboards so any changes will be reflected immediately in the Vue UI.

In general, it's best to keep all Vue products running the same version for simplicity. But it is possible to manually create an Octopus release for the code or data with EatingOut on a much later version than Barometer for example.  To deploy a previous version of BrandVue using Octopus:

1) From the dashboard select the BrandVue project;

2) From the project page press <Create Release>;

3) On the create release page expand the packages drop down;

4) By default the latest packages for release are selected.  This can be overridden for each dashboard by selecting the relevant version in the "specific" version column;

5) Press the <Save> button to complete the configuration and make it available for deployment;

6) On the release page press <Deploy to Test>.  It is recommended to deploy to test first and then once this has been proven to work then to beta and live;

7) Then press the <Deploy> button to deploy the release;

8) The task progress page is then displayed which shows which steps have been successfully completed.  If any of the steps fail, review the task log, fix the cause and then re-deploy the release.

Common issues and troubleshooting
---------------------------------

The link between BrandVue and Vue Reporting is fragile and has basically no tests. If there has been a change to the "internal API" of BrandVue, it's likely to break Vue Reporting. The quickest way to troubleshoot this would be to look at the BrandVue Service in Vue Reporting. The url's in this service should match those of BrandVue. The service makes API calls with a base url contained in the development appsettings json file. Debugging simply requires one to start BrandVue as well.  

Map files
---------

Map files can be found in "I:\Shared\Systems\Dashboards" under the relevant folder. They will be called "Map.xlsx"

Build and deploy
----------------

Documented on the readmes

- <https://github.com/MIG-Global/Vue>

Dependencies and prerequisites
------------------------------

- <https://github.com/MIG-Global/Vue>

Logs and alerting
-----------------

- Standard Savanta logging

Incident management and disaster recovery
-----------------------------------------

If the dashboard is completely broken for more than about a minute, it makes us look bad, but it's not the end of the world, it might be that no-one is even looking at the time. If it's out of action for a decent proportion of a month there may be financial penalties. People currently mainly use during UK working hours on workdays. API users may make calls to the API at any time of day.  This is likely to cover more hours as we expand around the globe. The only dashboard that has a contractual SLA is Barometer so fixing this quickly is a higher priority.

The first step to resolving a broken dashboard is to try to identify the root cause.  Reproducing the error on the env that it has been reported is a good first step.  Also looking at the error logs in Stackify may indicate what has caused the failure.  Consult with other members of the tech team if possible, to identify the best action to take.  It may be that a simple re-start of the app pool for the dashboard might solve the problem.  Failing this the next thing to try would be to roll back the application or the data. If all dashboards are broken, then this would point more towards an issue with the code so rolling back to the previous version of BrandVue may solve it.  If an individual dashboard is broken, then it would point more towards a data issue so rolling back to the previous data package for the affected dashboard may solve the problem.  If neither of these in isolation solve the issue, then rolling back both would be the next this to try.  Rollback data with caution though as an client's automated API calling session may span the old and new versions of the data and as a result may not get a completely integrated set of data.

Communication around outages is more important than 100% uptime. So, if rollback doesn't fix the issue, get an error message on the site. One way to do this is to go onto the live deployed site, edit Maintenance/app_offline.htm to have an appropriate message, then move it into the site root. IIS will immediately pick up the file for any new web requests.

People
------

- General: Adam G, Graham H, Mark R, Bart R, David C

- Who needs to know if something goes wrong? Data/ops, i.e. Pete L / Ed R / John L

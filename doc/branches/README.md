# Branches

This lets us see branches deployed in the test environment prior to merging:
https://savanta.test.all-vue.com/branches/

It currently points at the same database as test, but doesn't run any migrations.

### Setup

This is currently only set up for the main BrandVue process, not CustomerPortal/VueReporting etc.
It adds complexity, so it's only worth it where that is paid back. We are also considering Azure App Services, which will need a new setup for this anyway.

* Azure Devops sets the branch name as a version prefix for non master branches
* Octopus has [channels](https://savanta.octopus.app/app#/Spaces-1/projects/brandvue/deployments/channels) set up which ensure we never deploy such a branch to other environments (and vice versa)
* The Octopus process has an [initial step](https://savanta.octopus.app/app#/Spaces-1/projects/brandvue/deployments/process/steps?actionId=ead070df-53d3-407e-9ac3-67ea4ca40050&parentStepId=10b34636-b416-42cf-b42b-c583f49ab4f2) to get hold of the branch name
* The standard set of deployment steps run, but with the project variables virtualPathPrefix set to put it in a branches subdirectory, and AppDeploymentBranch set to ensure that db migrations aren't run.
* The final deployment step stores a text file with branch names in, and clears up any old apps/pools.

* The test server has a virtual directory called "branches" under the Test-Vue app. I've manually copied in branches.aspx (there's a copy in this folder for reference), which is just a basic page to list out the app branches.
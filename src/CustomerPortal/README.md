## Prerequisites
* check `TargetFramework` in CustomerPortal.csproj and install correlated SDK if necessary.
* node needs to be installed with those modules installed globally: webpack `https://www.npmjs.com/package/webpack`, node-sass `https://www.npmjs.com/package/node-sass`.

## Running Locally

* In the `Task Runner Explorer`, run the `install` and then the `build:hotdev` tasks.
* If you see an error in the `build:hotdev` task, it may be because there is already something running on the used port.
    * We initially used port 9001 which was unused by windows. Since we started using this however, windows now occasionally uses this port for other apps.
    * To solve, try changing the `HMRPort` value in the `appsettings.json` folder to 9002, and rerunning the `build:hotdev` task.
* If you want to use a local db, make sure the `DefaultConnection` string is correct. If you aren't concerned about having specific backend data linked up, you can replace the connection string with an empty string; this will populate the customer portal with random data.
* Run the `CustomerPortal` project.

## Uploading/downloading documents

* You can upload and download client documents through the "Documents" tab as though you were a client. The "Savanta" files come from files uploaded through the "Customer Portal" tab on the research portal.
* These files are uploaded/downloaded to an egynte ftp location, `https://savantaftp.egnyte.com/app/index.do#storage/files/1/Shared/Savanta/Service Assets/Customer Portal/<company>/<survey name> (<survey id>)/Client`
* To upload/download documents you will need to populate the `EgnyteAccessToken` value in `appsettings.json`. You can find this on the dev front-end vue server.

## Common Troubleshooting

* One common problem is an invalid or expired Security Certificate. 
   * To check this, go to `https://localhost:9001` (or port 9002 if you've had to update the dev port) while the `hotdev:build` task is running.
   * If you see an error about an invalid certificate, this is what's causing the issue.
   * To solve, click on the icon to the left of the addressbar in chrome. It should open up a popout showing various options, one of which is certificate (may not exactly match the image):
   
   ![image](https://user-images.githubusercontent.com/50362898/97296869-26f1eb80-1849-11eb-82b0-fa01e49446ff.png)

   * If you click on "Certificate", it should open a new window. Click on "details", and "Copy to file...":
   
   ![image](https://user-images.githubusercontent.com/50362898/97297059-6ae4f080-1849-11eb-866f-3382fe8744b9.png)
   
   * Save the certificate somewhere on your machine.
   * In windows explorer, navigate to the saved certificate and double-click on it; it should open up that certificate window again.
   * To install the certificate, go to "General" > "Install Certificate" which should open up a new popout.
   * Select "Local Machine" > "Place all certificates in the following store" > "Browse..." > "Trusted Root Certification Authorities"
   
   ![image](https://user-images.githubusercontent.com/50362898/97297477-04ac9d80-184a-11eb-9983-35828fd08772.png)
   
   * Pressing "Next" then "Finish" should install your certificate.
   * *Close all open chrome tabs, including local ones* - this is necessary to allow chrome to refresh the certificates.
   * Rerunning the customer portal should now pick up the new certificate.

* Problem: Application gets stuck on a page showing "Loading", after authenticating 

   * Check the console in dev tools (F12) for Status Code 431.
   * Clear application data / local cache & cookies in the browser.
   * Refresh the page and re-auth.
   * Success!

* Problem: Application does not update front-end after changes was made 

   * Stop `npm run build:hotdev`
   * Stop application
   * Run `npm cache clean --force`
   * Run `npm run build:dev`
   * Run `npm run build:hotdev` again
   * Start application
   * Success!

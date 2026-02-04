If you manually move app_offline.htm into the root of the app/site, IIS will detect it and display it, rather than loading the app.
Before doing this, you should ideally change the message about when we expect the site to be back.

This can be useful if you need to stop all access to the site for maintenance/security reasons.
It has no effect when it's in a folder as it is by default here.
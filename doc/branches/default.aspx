<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Linq" %>

<script runat="server">


            public static string[] GetTableRowsHtml()
            {
                var directoryGroups = File.ReadAllLines(@"C:\inetpub\wwwroot\branches\branches.txt")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .GroupBy(x => string.Join("-", x.Split('-').Skip(1)));
                var tableRows = directoryGroups.Select(g =>
                {
                    string nullableShortcutId = g.Key.Split('-').Skip(1).FirstOrDefault();
                    string shortcutId = nullableShortcutId != null ? nullableShortcutId : "";
                    int i = 0;
                    var shortcutLink = int.TryParse(shortcutId, out i) ? ("<a href='https://app.shortcut.com/mig-global/story/" + shortcutId + "'>" + g.Key + "</a>") : (g.Key);
                    var dashboardLinks = g.OrderBy(x => x).Select(d => "<a href='" + GetRelativePath(d) + "'>" + GetLinkText(d) + "</a>");

                    string html = "<tr>" +
                                  "<td style='padding: 10px; border: 1px solid #ddd;'>" + shortcutLink + "</td>" +
                                  "<td style='padding: 10px; border: 1px solid #ddd;'>" + string.Join(", ", dashboardLinks) + "</td>" +
                                  "</tr>";
                    return html;
                });
                return tableRows.ToArray();
            }
            
            private static string GetRelativePath(string d)
            {
                return IsSurveySnapshotBranch(d) ? d + "/26161" : d.Contains("/survey-") ? d + "/5774" : d;
            }

            private static string GetLinkText(string d)
            {
                string dashboard = d.Split('-').First().Split('/').Last();
                return IsSurveySnapshotBranch(d) ? "survey (live snapshot)" : dashboard;
            }

            private static bool IsSurveySnapshotBranch(string d)
            {
                return d.Contains("/snapshot/branches/survey-");
            }
</script>

<html><head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Branches for BrandVue/AllVue</title>
    <link rel="icon" class="favicons" href="https://savanta.test.all-vue.com/auth/favicon.png">
    <link rel="stylesheet" href="https://savanta.test.all-vue.com/auth/lib/bootstrap/css/bootstrap.min.css">
    <link rel="stylesheet" href="https://savanta.test.all-vue.com/auth/css/site.css">
    <link rel="stylesheet" href="https://savanta.test.all-vue.com/auth/css/buttons.css">
    <link rel="stylesheet" href="https://savanta.test.all-vue.com/auth/css/fonts.css">
    <link rel="stylesheet" href="https://savanta.test.all-vue.com/auth/api/theme/stylesheet.css">
    <link rel="stylesheet" href="https://savanta.test.all-vue.com/auth/lib/coloris/coloris.min.css">
<style>
      @keyframes rotate360 {
        0% {
          transform: rotate(0deg);
        }
        100% {
          transform: rotate(360deg);
        }
      }
    
        @font-face {
            font-family: 'Amazon Ember';
            src: url("chrome-extension://cgdjpilhipecahhcilnafpblkieebhea/fonts/AmazonEmber_Rg.ttf");
            font-weight: normal;
            font-style: normal;
            font-display: swap; 
        }
        @font-face {
            font-family: 'Amazon Ember';
            src: url("chrome-extension://cgdjpilhipecahhcilnafpblkieebhea/fonts/AmazonEmber_Bd.ttf");
            font-weight: bold;
            font-style: normal;
        }
    </style></head>
<body class="themed vsc-initialized">
    
<div class="navbar navbar-inverse navbar-expand">
        <div class="collapse container navbar-collapse d-flex justify-content-between" id="navbarNav">
            <a href="https://savanta.test.all-vue.com/auth">
                <span class="company-logo">
            </span></a>
            <div class="d-flex flex-row-reverse">
                <ul class="navbar-nav">
                        <a class="nav-link help-link" target="_blank" href="https://docs.savanta.com/allvue/Default.html">
                            <div class="circular-nav-button">
                                <div class="circle">
                                    <i class="material-icons">help</i>
                                </div>
                            </div>
                            <span class="my-account-dropdown">Help</span>
                        </a>
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" id="navbarDropdownMenuLink" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                            <div class="circular-nav-button">
                                <div class="circle">
                                    <i class="material-icons">account_circle</i>
                                </div>
                            </div>
                            <span class="my-account-dropdown">My Account</span>
                        </a>
                        <div class="dropdown-menu navbarDropdownMenuLink-menu" aria-labelledby="navbarDropdownMenuLink">
                            <div class="dropdown-divider"></div>

                                <a class="dropdown-item" href="https://savanta.test.all-vue.com/auth">Your projects</a>
                                <a class="dropdown-item" href="https://savanta.test.all-vue.com/auth/Account/Logout">Logout</a>
                                <div class="dropdown-divider"></div>
                                    <a class="dropdown-item" href="https://savanta.test.all-vue.com/auth/UsersPage">Manage users</a>
                                    <a class="dropdown-item" href="https://savanta.test.all-vue.com/auth/ProductsPage">Manage products</a>
                                    <a class="dropdown-item" href="https://savanta.test.all-vue.com/auth/ApiKeys">Manage API keys</a>
                        </div>
                    </li>
                </ul>
            </div>
        </div>
</div>

<div class="container body-content mt-3">
    <h1>
        BrandVue/AllVue pre-merge branches
    </h1>
    <p>All branches point to the same config database as the test environment, but they don't run any migrations (so don't try testing migrations here).</p>
    <p>So if master has moved on, you may need to merge/rebase to be compatible with its db.</p>
    <p>Octopus cleans these out after a week of not being updated</p>
    <table style="border-collapse: collapse; width: 100%;">
        <tr>
            <th style="padding: 10px; border: 1px solid #ddd; background-color: #f2f2f2;">Shortcut</th>
            <th style="padding: 10px; border: 1px solid #ddd; background-color: #f2f2f2;">Dashboards</th>
        </tr>
        <%
            Response.Write(string.Join(Environment.NewLine, GetTableRowsHtml()));
        %>
    </table>
    <br />
    <p>See information on how this is set up in <a href="https://github.com/Savanta-Tech/Vue/tree/master/doc/branches">GitHub</a></p>
</div>

<script src="https://savanta.test.all-vue.com/auth/lib/jquery/jquery.slim.min.js"></script>
<script src="https://savanta.test.all-vue.com/auth/lib/bootstrap/js/bootstrap.min.js"></script>
<script src="https://savanta.test.all-vue.com/auth/lib/coloris/coloris.min.js"></script>
    
</body></html>

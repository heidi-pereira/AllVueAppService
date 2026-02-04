$ErrorActionPreference = "Stop"

Remove-Item -Force -Recurse ./wwwroot/developers/docs -ErrorAction Ignore
New-Item -Type Directory ./wwwroot/developers/docs
Push-Location ./developers
widdershins ../wwwroot/developers/BrandVueApi.OpenApi3.json -e ./docs/source/markdownFromOpenApi.widdershins.json -o ../bin/docs.index.html.md
node ./docs/source/buildstyle.cjs
shins ../bin/docs.index.html.md --root ./docs -o ../wwwroot/developers/docs/index.html --attr --minify;
Copy-Item -recurse docs/pub ../wwwroot/developers/docs/pub
Copy-Item -recurse docs/source/images ../wwwroot/developers/docs/source/images
Copy-Item -recurse docs/source/fonts ../wwwroot/developers/docs/pub/fonts
Pop-Location
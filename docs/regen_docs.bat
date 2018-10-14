rem obliterate caches, just for safety's sake
rmdir /S /Q docs\obj
rmdir /S /Q docs\bin
rmdir /S /Q docs\packages
rmdir /S /Q docs\DROP
rmdir /S /Q docs\TEMP

rem get the repo, make sure there's no files
rmdir /S /Q docs\_site
git clone -b gh-pages . docs\_site
rmdir /S /Q docs\_site\*

rem generate docs
docfx docs\docfx.json

rem update repo
pushd docs\_site
git add .
git commit -a -m "Update documentation."
git push
popd

rem clear repo directory
rmdir /S /Q docs\_site

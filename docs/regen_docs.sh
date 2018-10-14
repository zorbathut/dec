#!/bin/bash

# obliterate caches, just for safety's sake
rm -rf docs/obj docs/bin docs/packages docs/DROP docs/TEMP

# get the repo, make sure there's no files
rm -rf docs/_site
git clone -b gh-pages . docs/_site
rm -rf docs/_site/*

# generate docs
docfx docs/docfx.json

# update repo
(cd docs/_site && git add . && git commit -a -m "Update documentation." && git push)

# clear repo directory
rm -rf docs/_site

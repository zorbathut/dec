#!/bin/bash

rm -rf docs/_site
git clone -b gh-pages . docs/_site
rm -rf docs/_site/*

docfx `cygpath -a -w docs/docfx.json`

(cd docs/_site && git add . && git commit -a -m "Update documentation." && git push)

rm -rf docs/_site

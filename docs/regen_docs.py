
import os
import shutil
import subprocess

# obliterate caches, just for safety's sake
if os.path.exists("docs/obj"): shutil.rmtree("docs/obj")
if os.path.exists("docs/bin"): shutil.rmtree("docs/bin")
if os.path.exists("docs/packages"): shutil.rmtree("docs/packages")
if os.path.exists("docs/DROP"): shutil.rmtree("docs/DROP")
if os.path.exists("docs/TEMP"): shutil.rmtree("docs/TEMP")

# get the repo, make sure there's no files in it, just git
if os.path.exists("docs/_site"): shutil.rmtree("docs/_site")
subprocess.check_call([
        "git",
        "clone",
        "-b", "gh-pages",
        ".",
        "docs/_site",
    ])
for f in [ f"docs/_site/{f}" for f in os.listdir("docs/_site") if f != ".git" ]:
    if os.path.isfile(f):
        os.remove(f)
    else:
        shutil.rmtree(f)

# generate docs
subprocess.check_call([
        "docfx",
        "docs/docfx.json",
    ])

if False:
    subprocess.check_call([
            "docfx",
            "serve", "docs/_site",
        ])

# update repo
os.chdir("docs/_site")

subprocess.check_call([
        "git",
        "add", ".",
    ])

subprocess.check_call([
        "git",
        "commit",
        "-a",
        "-m", "Update documentation.",
    ])

subprocess.check_call([
        "git",
        "push",
    ])
    
# ugh this is so ugly
os.chdir("../..");

# clear repo directory
shutil.rmtree("docs/_site")
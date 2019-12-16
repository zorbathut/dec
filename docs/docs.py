
import argparse
import os
import shutil
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--serve', action='store_true')
parser.add_argument('--deploy', action='store_true')
args = parser.parse_args()

if not args.serve and not args.deploy:
    print("Need at least one of serve or deploy")
    parser.print_help()
    exit(2);



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

if args.deploy:
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

if args.serve:
    subprocess.check_call([
            "docfx",
            "serve", "docs/_site",
        ])
        
# clear repo directory
shutil.rmtree("docs/_site")
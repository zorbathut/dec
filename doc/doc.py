
import argparse
import os
import shutil
import stat
import subprocess

def obliterate(path):
  if not os.path.exists(path):
    return
  
  if os.path.isfile(path):
    os.remove(path)
    return
  
  # guess we're a directory!

  # git really likes readonly'ing its files for some reason
  # ganked directly from https://stackoverflow.com/questions/4829043/how-to-remove-read-only-attrib-directory-with-python-in-windows/4829285#4829285
  def on_rm_error(func, path, exc_info):
    # path contains the path of the file that couldn't be removed
    # let's just assume that it's read-only and unlink it.
    os.chmod(path, stat.S_IWRITE)
    os.unlink(path)

  shutil.rmtree(path, onerror = on_rm_error)

parser = argparse.ArgumentParser()
parser.add_argument('--serve', action='store_true')
parser.add_argument('--deploy', action='store_true')
args = parser.parse_args()

if not args.serve and not args.deploy:
    print("Need at least one of serve or deploy")
    parser.print_help()
    exit(2);


# obliterate caches, just for safety's sake
obliterate("doc/obj")
obliterate("doc/bin")
obliterate("doc/packages")
obliterate("doc/DROP")
obliterate("doc/TEMP")

# get the repo, make sure there's no files in it, just git
obliterate("doc/_site")
subprocess.check_call([
        "git",
        "clone",
        "-b", "gh-pages",
        ".",
        "doc/_site",
    ])
for f in [ f"doc/_site/{f}" for f in os.listdir("doc/_site") if f != ".git" ]:
  obliterate(f)

# generate docs
subprocess.check_call([
        "docfx",
        "doc/docfx.json",
        "--warningsAsErrors",
    ])

if args.deploy:
    # update repo
    os.chdir("doc/_site")

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
            "serve", "doc/_site",
        ])
        
# clear repo directory
obliterate("doc/_site")

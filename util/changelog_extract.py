import itertools;
import os;

id = os.environ.get("GITHUB_REF").strip("refs/tags/")

with open("CHANGELOG.md", "r") as input:
  lines = input.readlines()

lines = [l.rstrip() for l in lines]
truncatefront = itertools.dropwhile(lambda line: id not in line, lines)
dropintro = itertools.islice(truncatefront, 1, None)
removeend = itertools.takewhile(lambda line: id in line or "## [" not in line, dropintro)

for line in removeend:
  print(line)

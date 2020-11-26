Support for sharpfuzz/AFL on def.

Setting this up is kind of a pain (and doesn't even work on Windows) so I'm using vagrant/ansible to set up a VM automatically.

----

vagrant up (or vagrant provision if you're just trying to update after code changes)

vagrant ssh

AFL_SKIP_BIN_CHECK=1 afl-fuzz -i /vagrant_build/util/sharpfuzz/testcase -o /vagrant_build/util/sharpfuzz/result -t 5000 -x /usr/local/share/afl/dictionaries/xml.dict -m 4000 dotnet /vagrant_build/util/sharpfuzz/bin/Debug/netcoreapp3.1/def-sharpfuzz.dll

cd /vagrant_build/util/sharpfuzz/result

ls
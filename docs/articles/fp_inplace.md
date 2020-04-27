# In-place data updating

There's nothing in def itself that prevents people from modifying values at runtime. While this is strongly discouraged in production code, it's a useful tool for rapid development; a good UI can change data tuning from a day-long process into a minute-long process.

Normally, the user then has to transcribe the changed value into the .xml files. But it sure would be cool if def could do that on its own! The XML library it uses provides enough metadata to regenerate the same file byte-for-byte; injecting modified data at the same time wouldn't be too hard.

Def now supports serializing the def database into an .xml file, via the Writer class. But it's limited and doesn't produce good human-readable output.

This is likely going to happen after I have the appropriate UI rewritten.
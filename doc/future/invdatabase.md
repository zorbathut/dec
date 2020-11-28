# Invalid database warnings

Right now Database<T> only requires that T inherit from Dec.Dec. It's perfectly valid to try accessing a database consisting of Dec.Dec itself, or of a subclass that doesn't define a hierarchy.

We should probably yell really loudly at the user whenever this happens.

This is on hold until someone makes the mistake and gets annoyed that they weren't warned about it.
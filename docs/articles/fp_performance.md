# Performance

A lot of the code in def was written with the intent of getting it working. It is *gratuitously* inefficient, simply because at the scale I'm currently working, inefficiencies are undetectable.

If you're using def on a larger-scale project, it's starting to get slow, and you're willing to share your code and XML with me, please get in touch; repro cases are necessary to get actual work done.

## Caching

C#'s reflection API is not terribly fast, and def currently queries it over and over. Simply storing these values before re-requesting them would go a long way to improve performance.

## Multithreading

Much of the parsing process is designed so it can be split into multiple tasks in a thread pool with little-to-no interference. Modern computers usually have 4 cores or more, and this would roughly quadruple loading speed.

## Baking

Parsing XML is intrinsically slow and will never be particularly fast. Baking the XML into a pre-parsed binary format could dramatically improve load time, at the cost of considerable implementation difficulty. This is likely worth doing only if the previous two steps prove insufficient.
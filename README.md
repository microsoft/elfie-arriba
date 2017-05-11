# Arriba

Arriba is an in-process C# data engine designed for free text search, structured search, and "conversational speed" data exploration across large single machine datasets. Arriba makes it easy to expose great "as you type" text search for your data on the web, to build high performance custom analytics in managed languages, or to work directly with large datasets in a managed process. Arriba has a simple, flexible [query syntax](../../wiki/Query-Syntax) which is designed to "just work" for web-style search, C#/VB/C++ expressions, and SQL WHERE clause styles. Type your query in your most familiar language and it should "just work". Arriba exposes a simple HTTP interface to allow querying and data manipulation via a service.

See the [Arriba QuickStart](https://github.com/Microsoft/elfie-arriba/wiki/Arriba-QuickStart) to get Arriba running with custom data in a few minutes!

# Elfie

Elfie is a library which makes it easy to build memory-efficient, extremely fast item sets providing search and traversal. Elfie uses the "structure of arrays" layout model for performance. A set class contains multiple columns of primitive types, enums, or a replacement string type, String8. Items of the set are structs which point to the set and a specific index. This gives the performance of structs (no allocations) with the convenience of classes (updates change all references of the item without copying). Elfie has primitives to provide text search (MemberIndex), define hierarchies (ItemTree), and define graphs (ItemMap). It also provides very fast read and write of CSV, TSV, and JSON via a consistent interface.

As an example, a set of ~5M Active Directory items with five columns and ~25M links in a graph fit in ~800MB, loads in ~600ms, and can be traversed at a rate of ~15M links per second, measured on a Surface Book i7.

## Contributing

Arriba and Elfie are not owned by a dedicated team, so while fixes and small changes are welcome, our ability to include contributions and comment on design changes is limited. For larger fixes and design change ideas, please contact us so that we can comment on the design or suggest working in another fork. 

Please:
* Follow the [.NET Foundation Coding Guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
* Sign a [Contributor License Agreement](https://cla2.dotnetfoundation.org/) so that the community may freely use your contributions
* Ensure a Release build and Unit Test run is clean
* Run the [Code Formatter](https://github.com/vScottLouvau/codeformatter) passing "/rule+:UsingOrder,BraceNewline Arriba.sln"

Arriba and Elfie performance depend on minimizing allocations, boxing, and indirect method calls, so compare performance of real-life scenarios involving your code to avoid regressions. They were created by Microsoft to enable great internal tools, and we've opened them hoping they will enable you to create great search and analytics tools in your favorite language. =)


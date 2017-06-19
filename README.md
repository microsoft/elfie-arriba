# Arriba

Arriba is a C# data engine designed for instant structured search, free text search, and data exploration across large single machine datasets. 

Give Arriba CSVs or simple C# arrays and it will automatically create tables with appropriately typed columns. 

Arriba features a simple, elegant [query syntax](../../wiki/Query-Syntax) so that users can write anything from a web-style query "louvau -Closed" to a fully structured query ([Assigned To] = "Scott Louvau" AND ([State] != "Closed" OR [Remaining Work] > 0)).

Arriba has a beautiful website to make search and exploration easy, with comprehensive query suggestions, a configurable listing, customizable item details, and a Grid for quick analytics. Query suggestions go beyond showing just column names and the search syntax by adding "Inline Insights", showing query-specific top values and distributions for columns and showing which columns word searches are matching to answer questions directly and help users construct the queries they really intend.

Arriba exposes a comprehensive HTTP service you can use to programmatically run queries and aggregations, get query suggestions, and add/decorate/update/delete rows.

You can even host the Arriba engine directly in your C# process, creating tables in-memory and making custom column types, queries, and aggregations by implementing simple interfaces.

See the [Arriba QuickStart](https://github.com/Microsoft/elfie-arriba/wiki/Arriba-QuickStart) to get Arriba and the Website running with sample CSV data in 15 minutes.

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


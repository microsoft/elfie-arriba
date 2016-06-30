# Arriba

Arriba is an in-process C# data engine designed for free text search, structured search, and "conversational speed" data exploration across large single machine datasets. Arriba makes it easy to expose great "as you type" text search for your data on the web, to build high performance custom analytics in managed languages, or to work directly with large datasets in a managed process. Arriba has a simple, flexible [query syntax](../../wiki/Query-Syntax) which is designed to "just work" for web-style search, C#/VB/C++ expressions, and SQL WHERE clause styles. Type your query in your most familiar language and it should "just work". Arriba exposes a simple HTTP interface to allow querying and data manipulation via a service.

## Architecture

Arriba stores data in tables with strongly typed columns like a relational database. Data is stored by column, in memory, in strongly typed arrays. Arriba can infer column types automatically and create columns for new data. Columns can be sorted and word indexed to provide text and structured query support, or left as-is for fast insertion speed. Data is separated into "partitions" of 64K items which are split across processor cores and queried in parallel. Data can be saved to disk and reloaded rapidly. Queries and Aggregations are implemented via a public interface, IQuery, which has direct, strongly-typed access to the underlying arrays. This allows developers to write new custom queries which work with the data as quickly as if it was in a large array directly.

## Contributing

Arriba is not owned by a dedicated team, so while fixes and small changes are welcome, our ability to include contributions and comment on design changes is limited. For larger fixes and design change ideas, please contact us so that we can comment on the design or suggest working in another fork. 

Please:
* Follow the [.NET Foundation Coding Guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
* Sign a [Contributor License Agreement](https://cla2.dotnetfoundation.org/) so that the community may freely use your contributions
* Ensure a Release build and Unit Test run is clean
* Run the [Code Formatter](https://github.com/vScottLouvau/codeformatter) passing "/rule+:UsingOrder,BraceNewline Arriba.sln"

Arriba performance depends on keeping allocations, boxing, and indirect method calls minimized, so compare performance of real-life scenarios involving your code to avoid regressions. Arriba was created by Microsoft to enable great internal tools, and we've opened it hoping it will enable you to create great search and analytics tools in your favorite language. =)

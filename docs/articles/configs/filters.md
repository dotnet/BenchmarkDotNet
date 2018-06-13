---
uid: docs.filters
name: Filters
---

# Filters

Sometimes you don't want to run all of your benchmarks.
In this case, you can *filter* some of them with the help of *filters*.

Predefined filters:

| Filter Type         | Filters benchmarks by       | Console argument | Console example                 |
|---------------------|-----------------------------|------------------|---------------------------------|
| MethodNamesFilter   | Provided method names       | method(s)        | --methods=ToStream,ToString     |
| TypeNamesFilter     | Provided type names         | class(s)         | --class=XmlSerializerBenchmarks |
| NamespacesFilter    | Provided namespaces         | namespace(s)     | --namespace=System.Memory       |
| AttributesFilter    | Provided attribute names    | attribute(s)     | --attribute=STAThread           |
| AllCategoriesFilter | All Provided category names | category(s)      | --category=Priority1            |
| AnyCategoriesFilter | Any provided category names | anycategories    | --anycategories=Json,Xml        |
| SimpleFilter        | Provided lambda predicate   | -                |                                 |
| NameFilter          | Provided lambda predicate   | -                |                                 |
| UnionFilter         | Logical AND                 | -                |                                 |
| DisjunctionFilter   | Logical OR                  | -                |                                 |

---

[!include[IntroFilters](../samples/IntroFilters.md)]

The link to this sample: @BenchmarkDotNet.Samples.IntroFilters

---

[!include[IntroCategories](../samples/IntroCategories.md)]

The link to this sample: @BenchmarkDotNet.Samples.IntroCategories

---

[!include[IntroJoin](../samples/IntroJoin.md)]

The link to this sample: @BenchmarkDotNet.Samples.IntroJoin
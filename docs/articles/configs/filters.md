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
| MethodNamesFilter   | Provided method names       | method           | --methods=ToStream,ToString     |
| TypeNamesFilter     | Provided type names         | class            | --class=XmlSerializerBenchmarks |
| NamespacesFilter    | Provided namespaces         | namespace        | --namespace=System.Memory       |
| AttributesFilter    | Provided attribute names    | attribute        | --attribute=STAThread           |
| AllCategoriesFilter | All Provided category names | categories       | --categories=Priority1          |
| AnyCategoriesFilter | Any provided category names | anycategories    | --anycategories=Json,Xml        |
| SimpleFilter        | Provided lambda predicate   | -                |                                 |
| NameFilter          | Provided lambda predicate   | -                |                                 |
| UnionFilter         | Logical AND                 | -                |                                 |
| DisjunctionFilter   | Logical OR                  | -                |                                 |

---

[!include[IntroFilters](../samples/IntroFilters.md)]

[!include[IntroCategories](../samples/IntroCategories.md)]

[!include[IntroJoin](../samples/IntroJoin.md)]
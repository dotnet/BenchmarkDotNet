---
uid: docs.orderers
name: Orderers
---

# Orderers

Orderers allows customizing the order of benchmark results in the summary table.

The following built-in order policies are available.
- <xref:BenchmarkDotNet.Order.SummaryOrderPolicy>
- <xref:BenchmarkDotNet.Order.MethodOrderPolicy>
- <xref:BenchmarkDotNet.Order.JobOrderPolicy>

It can also use custom orderer by implemented <xref:BenchmarkDotNet.Order.IOrderer> interface.

---

[!include[IntroOrderAttr](../samples/IntroOrderAttr.md)]

[!include[IntroOrderManual](../samples/IntroOrderManual.md)]
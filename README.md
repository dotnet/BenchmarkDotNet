Light .NET-framework for benchmarks.

Usage example:

    new Benchmark().Run(() => IncrementBenchmark.After());

Result:

	WarmUp:
	Ticks: 1208680 ms: 563
	Ticks: 1208053 ms: 563
	Ticks: 1187362 ms: 553
	Ticks: 1106776 ms: 516
	Ticks: 965251 ms: 450
	Ticks: 882379 ms: 411
	Ticks: 831836 ms: 388
	Ticks: 761670 ms: 355
	Ticks: 723164 ms: 337
	Ticks: 689253 ms: 321
	Ticks: 560881 ms: 261
	Ticks: 496224 ms: 231
	Ticks: 495499 ms: 231
	Ticks: 496597 ms: 231
	Ticks: 496276 ms: 231
	Ticks: 495410 ms: 231
	Ticks: 495504 ms: 231
	Ticks: 495006 ms: 230
	Ticks: 495736 ms: 231
	Ticks: 497054 ms: 231
	TickStats: Min=495006, Max=1208680, Avr=729430, Diff=144,17%
	MsStats: Min=230, Max=563, Avr=339

	Result:
	Ticks: 497259 ms: 231
	Ticks: 495126 ms: 230
	Ticks: 497055 ms: 231
	Ticks: 496984 ms: 231
	Ticks: 496218 ms: 231
	Ticks: 496864 ms: 231
	Ticks: 497765 ms: 232
	Ticks: 495813 ms: 231
	Ticks: 497294 ms: 231
	Ticks: 495919 ms: 231
	Ticks: 496531 ms: 231
	Ticks: 495492 ms: 231
	Ticks: 496701 ms: 231
	Ticks: 496980 ms: 231
	Ticks: 496150 ms: 231
	Ticks: 495864 ms: 231
	Ticks: 496777 ms: 231
	Ticks: 497466 ms: 232
	Ticks: 496409 ms: 231
	Ticks: 497261 ms: 231
	TickStats: Min=495126, Max=497765, Avr=496596, Diff=00,53%
	MsStats: Min=230, Max=232, Avr=231
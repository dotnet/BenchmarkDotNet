// BenchmarkDotNet.IntegrationTests.Native.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <stdio.h>


extern "C"
{
	typedef struct _Point {
		int X;
		int Y;
	}Point;

	__declspec(dllexport) void DisplayHelloFromDLL()
	{
		printf("Hello from DLL !\n");
	}

	__declspec(dllexport) int* AllocateArrayOfInt(int size)
	{
		int* a = new int[size];
		for (int i = 0; i < size; i++) {
			a[i] = i;      // Initialize all elements to zero.
		}
		return a;
	}

	__declspec(dllexport) void DeallocateArrayOfInt(int* ptr)
	{
		delete[] ptr;
	}

	__declspec(dllexport) Point* AllocateArrayOfPoint(int size)
	{
		Point* a = new Point[size];    // Pointer to int, initialize to nothing.

		for (int i = 0; i < size; i++) {
			a[i].X = i;      // Initialize all elements to zero.
			a[i].Y = i + 1;
		}
		return a;
	}

	__declspec(dllexport) void DeallocateArrayOfPoint(Point* ptr)
	{
		delete[] ptr;
	}
}
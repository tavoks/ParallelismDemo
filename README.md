This example demonstrates five different techniques for parallelism and asynchronous programming in .NET 9:

Task.WhenAny: Allows working with the first task to complete (useful for redundancy or timeouts).

Task.WhenAll: Runs multiple tasks in parallel and waits for all of them to complete.

IAsyncEnumerable: Enables asynchronous data streaming, processing items as they become available.

Parallel.ForEach: CPU-based parallelism for intensive processing.

Task Cancellation: Demonstrates how to cancel ongoing asynchronous operations.

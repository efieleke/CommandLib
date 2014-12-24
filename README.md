CommandLib
=========

CommandLib is a C# library that simplifies coordination of synchronous and asynchronous activities. The library is built upon class Command, which represents an action. A Command may be run synchronously or asynchronously, and may be aborted.

ParallelCommands, itself a Command, executes a collection of commands concurrently, and SequentialCommands executes its commands in sequence. Using these classes, it's possible to create a deep nesting of coordinated actions. For example, SequentialCommands can hold instances of ParallelCommands, SequentialCommands, and any other Command-derived object.

PeriodicCommand repeats its action at a given interval, ScheduledCommand runs once at a specific time, and RecurringCommand runs at times that are provided via a callback.

RetryableCommand provides the option to keep retrying a failed command until the caller decides enough is enough, and TimeLimitedCommand fails with a timeout exception if a given duration elapses before the command finishes execution.

All of the above Command classes are simply containers for other Command objects that presumably do something of interest. CommandLib includes a few Command classes that might be commonly useful, including PauseCommand, DownloadFileCommand, DownloadStringCommand and UploadDataCommand, but it is expected that users of this library will create their own Command-derived classes.

Help File Documentation
----
Documentation for CommandLib is provided in CommandLib.chm. It was generated from source code comments using Sandcastle Help File Builder (https://shfb.codeplex.com).

Diagnostics
----
The Command class provides a static ICommandMonitor property. If set to a CommandTracer, diagnostic output is written to the debug stream. If set to a CommandLogger, diagnostic output is written to file (and optionally the debug stream as well). Using the provided CommandLogViewer app, it is possible to see the status of all command executions, including their parent/child relationships. This viewer is not polished; it is meant for developers and testers, not end users.

Build
----
Included is a solution file that contains four projects: CommandLib itself, a unit test project, a log file viewer (for logs generated using CommandLogger) and a project demonstrating example usage. All projects target .NET 4.0, but I suspect they would build without issue against other versions of .NET as well. The solution and project files were created using Microsoft Visual Studio 2013.

Unit Tests
----
Code coverage of CommandLib would be at 100%, except that some test methods were disabled in the interest of not being reliant upon a test web server. If you want to run them (and have the test coverage back to 100%) modify the URLs that the tests use and remove the comments that precede the TestMethod attribute for those tests. The disabled tests reside within DownloadFileCommandTests.cs, DownloadStringCommandTests.cs and UploadDataCommandTests.cs.

Example Usage
----
A sample project is included that moves two simulated robots. It demonstrates how to author a naturally asynchronous Command, and makes use of ParallelCommands, SequentialCommands, PeriodicCommand, TimeLimitedCommand and RetryableCommand.

Future plans
----
Ports to C++ and Java are forthcoming.

Author
----
Eric Fieleke

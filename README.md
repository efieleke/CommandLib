CommandLib
=========

CommandLib is a C# library that simplifies coordination of synchronous and asynchronous activities. The library is built upon class Command, which represents an action. A Command may be run synchronously or asynchronously, and may be aborted.

Two collection classes add depth and flexibility. ParallelCommands, itself a Command, executes its collection of commands concurrently, and SequentialCommands executes its commands in sequence. Using these classes, it's possible to create a deep nesting of coordinated activities. For example, SequentialCommands can hold instances of ParallelCommands, SequentialCommands, and any other Command-derived object.

PeriodicCommand repeats its action at a given interval, ScheduledCommand runs once at a specific time, and RecurringCommand runs at times that are provided via a callback.

RetryableCommand provides the option to keep retrying a failed command until the caller decides enough is enough, and TimeLimitedCommand fails with a timeout exception if a given duration elapses before the command finishes execution.

All of the above Command classes are simply containers for other Command objects that presumably do something of interest. CommandLib includes a few Command classes that might be commonly useful, including PauseCommand, DownloadFileCommand, DownloadStringCommand and UploadDataCommand, but it is expected that users of this library will create their own Command-derived classes.

Help File Documentation
----
Documentation for CommandLib is provided in CommandLib.chm. It was generated from source code comments using Sandcastle Help File Builder (https://shfb.codeplex.com).

Build
----
Included is a solution file that contains three projects: CommandLib itself, a unit test project, and a project demonstrating example usage. All projects target .NET 4.0, but I suspect they would build without issue against other versions of .NET as well. The solution and project files were created using Microsoft Visual Studio 2013.

Unit Tests
----
Code coverage of CommandLib would be at 100%, except that some test methods were disabled in the interest of not being reliant upon a test web server. If you want to run them (and have the test coverage back to 100%) modify the URLs that the tests use and remove the comments that precede the TestMethod attribute for those tests. The disabled tests reside within DownloadFileCommandTests.cs, DownloadStringCommandTests.cs and UploadDataCommandTests.cs.

Example Usage
----
A sample project is included that chronicles the story of two lovestruck robots attempting to meet each other at the origin. Besides ethos, it demonstrates how to author a naturally asynchronous Command, and makes use of ParallelCommands, SequentialCommands, PeriodicCommand, TimeLimitedCommand and RetryableCommand.

Contributions
----
Bug reports, requests and comments are always welcome. If you would like to make a code contribution, please make sure that public and protected methods are well-documented, in similar fashion to existing comments, so that I can auto-generate an updated help file. Also, code must compile without warnings, and unit test coverage must remain at 100%.

Author
----
Eric Fieleke

Future plans
----
I intend to port this over to C++ and/or Java. When I will get around to doing so, I don't know; that depends in part upon interest.
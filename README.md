CommandLib
=========

CommandLib is a C# library that simplifies coordination of synchronous and asynchronous activities. Verions for C++ and Java exist at https://github.com/efieleke/CommandLibForCPP.git and https://github.com/efieleke/CommandLibForJava.git. The library is built upon class Command, which represents an action. A Command may be run synchronously or asynchronously, and may be aborted.

ParallelCommands, itself a Command, executes a collection of commands concurrently, and SequentialCommands executes its commands in sequence. Using these classes, it's possible to create a deep nesting of coordinated actions. For example, SequentialCommands can hold instances of ParallelCommands, SequentialCommands, and any other Command-derived object.

PeriodicCommand repeats its action at a given interval, ScheduledCommand runs once at a specific time, and RecurringCommand runs at times that are provided via a callback.

RetryableCommand provides the option to keep retrying a failed command until the caller decides enough is enough, and TimeLimitedCommand fails with a timeout exception if a given duration elapses before the command finishes execution.

All of the above Command classes are simply containers for other Command objects that presumably do something of interest. CommandLib includes a few Command classes that might be commonly useful, including PauseCommand, DownloadFileCommand, DownloadStringCommand and UploadDataCommand, but it is expected that users of this library will create their own Command-derived classes.

Help File Documentation
----
Documentation for CommandLib is provided in CommandLib.chm. It was generated from source code comments using Sandcastle Help File Builder (https://shfb.codeplex.com).

Diagnostics
----
The Command class allows registration of ICommandMonitor objects. CommandTracer will write diagnostic output to the debug stream, and CommandLogger will write diagnostic output to file. Using the provided CommandLogViewer app, it is possible to see the status of all command executions, including their parent/child relationships.

Build
----
Included is a solution file that contains four projects: CommandLib itself, a unit test project, a log file viewer (for logs generated using CommandLogger) and a project demonstrating example usage. All projects target .NET 4.0, but I suspect they would build without issue against other versions of .NET as well. The solution and project files were created using Microsoft Visual Studio 2013.

Example Usage
----
A sample project is included that moves a simulated robot arm that tries to pick up a toy and drop it down the chute. It demonstrates how to author a naturally asynchronous Command, and makes use of ParallelCommands, SequentialCommands, PeriodicCommand and RetryableCommand.

Author
----
Eric Fieleke

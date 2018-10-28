Commands
=========

Commands is a C# library that simplifies coordination of asynchronous and synchronous activities. It works with tasks and with non-task operations, and offers features beyond what are available with tasks. The library is built upon class Command, which represents an action. A Command may be run synchronously or asynchronously, and may be aborted.

ParallelCommands, itself a Command, executes a collection of commands concurrently (in parallel), and SequentialCommands executes its commands in sequence. Using these classes, it's possible to create a deep nesting of coordinated actions. For example, SequentialCommands can hold instances of ParallelCommands, SequentialCommands, and any other Command-derived object.

PeriodicCommand repeats its action at a given interval, ScheduledCommand runs once at a specific time, and RecurringCommand runs at times that are provided via a callback.

RetryableCommand provides the option to keep retrying a failed command until the caller decides enough is enough, and TimeLimitedCommand fails with a timeout exception if a given duration elapses before the command finishes execution.

All of the above Command classes are simply containers for other Command objects that presumably do something of interest. They can be combined in ways that offer a lot of customization. For example, to make an HttpRequest at a given time, with a timeout and a configurable number of retries, you could create a ScheduledCommand containing a RetryableCommand containing a TimeLimitedCommand containing an HttpRequestCommand.

TaskCommand, DelegateCommand and Command.AsTask() offer easy integration with tasks and delegates.

Guidelines for developing your own Command-derived class:

- If the implementation of your command is naturally synchronous, inherit from SyncCommand

- If the implementation of your command is naturally asynchronous and makes use of tasks (i.e. the Task class), inherit from TaskCommand

- If the implementation of your command is naturally asynchronous but does not make use of tasks, inherit from AsyncCommand

- Make your implementation responsive to abort requests. To do this, make ocassional calls to Command.CheckAbortFlag() or Command.AbortRequested.

Versions for C++ and Java exist at https://github.com/efieleke/CommandLibForCPP.git and https://github.com/efieleke/CommandLibForJava.git.

Complete documentation is checked in to the Commands project source directory as file Commands.chm.

Diagnostics
----
The Command class allows registration of ICommandMonitor objects. CommandTracer will write diagnostic output to the debug stream, and CommandLogger will write diagnostic output to file. Using the provided CommandLogViewer app, it is possible to see the status of all command executions, including their parent/child relationships.

Source
----
Included is a solution file that contains four projects: Commands itself, a unit test project, a log file viewer (for logs generated using CommandLogger) and a project demonstrating example usage. The solution and project files were created using Microsoft Visual Studio 2017.

Example Usage
----
A sample project is included that prepares dinner.

Author
----
Eric Fieleke

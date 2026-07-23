# Windows Process Creation

## Scope

This document records the Windows desktop process-creation facts required by
Remote Voice Split. The supported product target is Windows 10 or later.

## Desktop shell identity

[`GetShellWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getshellwindow)
returns the Shell desktop window, or null when no Shell process is present.
[`GetWindowThreadProcessId`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid)
maps that window to its owning process identifier.

A process identifier alone is not an executable identity. Code that depends on
the Windows Explorer shell must open that process and verify its full image
path against the Windows installation's `explorer.exe`.

## Explicit parent process

[`InitializeProcThreadAttributeList`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-initializeprocthreadattributelist)
creates the extended attribute list used by `STARTUPINFOEX`.
[`UpdateProcThreadAttribute`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute)
accepts `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS` to select a process other than
the caller as the new process's parent. The selected parent handle requires
`PROCESS_CREATE_PROCESS` access. Windows derives the child process's inherited
device map, affinity, priority, quotas, token, and job from that selected
parent; some attributes still come from the creating process.

[`CreateProcessW`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw)
requires `EXTENDED_STARTUPINFO_PRESENT` when passed a `STARTUPINFOEX`. Its
Unicode command-line buffer must be writable. Passing the exact executable path
as `lpApplicationName` avoids the ambiguous executable search that occurs when
the application name is null. Process and thread handles returned in
`PROCESS_INFORMATION` must be closed.

An explicit parent controls the Windows process-tree relationship, but it does
not prove that a later named-pipe peer is the launched executable. Applications
that make a security or capture decision from this relationship must still
verify the actual peer PID, image path, and ancestry after connection.

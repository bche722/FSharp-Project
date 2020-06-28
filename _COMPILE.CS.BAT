if exist A1C.EXE del A1C.EXE
csc  -r:mscorlib.dll,netstandard.dll,System.Threading.Channels.dll,System.Threading.Tasks.Extensions.dll,System.Runtime.CompilerServices.Unsafe.dll A1C.cs

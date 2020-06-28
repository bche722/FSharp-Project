if exist A1C.EXE del A1C.EXE

csc  -r:mscorlib.dll,netstandard.dll,System.Threading.Channels.dll,System.Threading.Tasks.Extensions.dll,System.Runtime.CompilerServices.Unsafe.dll A1C.cs

A1C.EXE /S1:C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\A1\Inputs\in1.txt /S2:C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\A1\Inputs\in2.txt /CSP:3,3,0
pause
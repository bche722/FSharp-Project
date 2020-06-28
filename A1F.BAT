if exist A1F.EXE del A1F.EXE

fsc -r:Hopac.dll -r:Hopac.Core.dll A1F.fs

A1F.EXE /S1:C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\Part1\A1\Inputs\input1.txt /S2:C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\Part1\A1\Inputs\input2.txt /CSP:2,3,1
pause
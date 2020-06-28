SET PROMPT=$G$S
echo. > _sizes.log
for /L %%L in (100, 100, 1000) do (
echo. >> _sizes.log
ECHO ... sizes %%L*100,%%L*100 >> _sizes.log
@IF EXIST inputs.txt DEL inputs.txt
@for /L %%I in (1, 1, %%L) do @(
@ECHO 01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567 >> inputs.txt
)
A1F.EXE /S1:inputs.txt /S2:inputs.txt /ACT:500,500,0 >> _sizes.log 2>&1
timeout 2
A1F.EXE /S1:inputs.txt /S2:inputs.txt /CSP:500,500,0 >> _sizes.log 2>&1
timeout 5
)
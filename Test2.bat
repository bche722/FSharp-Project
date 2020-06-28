SET PROMPT=$G$S
echo. > _bands.log
for /L %%B in (500, 100, 2000) do (
echo. >> _bands.log
ECHO ... bands %%B,%%B >> _bands.log
A1F.EXE /S1:input1.txt /S2:input2.txt /ACT:%%B,%%B,0 >> _bands.log 2>&1
timeout 2
A1F.EXE /S1:input1.txt /S2:input2.txt /CSP:%%B,%%B,0 >> _bands.log 2>&1
timeout 5
)
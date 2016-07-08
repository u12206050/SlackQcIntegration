Available commands:
!def <defect id>    information about defect
!def -d <developer qc name>    list of "Not Closed" and "Not Obsoleted" defects assigned with devlead = developer qc name
!def -ql <qa qc name>    list of "Not Closed" and "Not Obsoleted" defects assigned with qalead = qa qc name

!req <requirement id>    shows information about requirement
!req -d <developer qc name> -s <sprint number>    list of "Not Done" US assigned to developer on selected sprint
!req -ql <qa qc name> -s <sprint number>    list of "Not Done" US assigned to qa on selected sprint

!note <some text>    adds note to database
!note -l    list of notes from database
!note -d <note id>    deletes note from database
!note -da    deletes all notes from database

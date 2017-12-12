read WebRequest
join [ServerName] WebServer.Big [ServerName] ""
columns [ID], [EventTime], [TimeTakenMs], [ResponseBytes]
assertCount 116

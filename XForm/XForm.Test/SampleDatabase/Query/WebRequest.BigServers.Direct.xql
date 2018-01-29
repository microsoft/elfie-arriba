read WebRequest
join [ServerName] WebServer.Big [ServerName] ""
select [ID], [EventTime], [TimeTakenMs], [ResponseBytes]
assertCount 116

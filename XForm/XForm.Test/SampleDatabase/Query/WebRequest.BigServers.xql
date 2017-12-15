read WebRequest
join [ServerName] WebServer [ServerName] ""
select [ID], [EventTime], [TimeTakenMs], [ResponseBytes], [ServerRam]
assertCount 1000
where [ServerRam] > 8000
assertCount 116
read WebRequest
select [ID] [EventTime] [UserName] [RequestBytes] [IsPremiumUser] [DaysSinceJoined]
cast [RequestBytes] int32
cast [IsPremiumUser] boolean           
cast [DaysSinceJoined] int32
cast [ID] int32
where [UserName] != ""
assert none                    
    where [DaysSinceJoined] = null
    end
assert none
    where [IsPremiumUser] = null
    end
-- [Shift][Cmd]'E' to execute from VS-Code / SQL Plugin
Select 
    -- queries with "CREATED" status
    (Select count(*) from dbo.ActivityQueriesDB WHERE [Status]=0) AS "Remaining",
    -- athlete count
    (Select count(*) from dbo.AthleteShortDB) AS "Athletes",
    -- athlete count in activities
    (Select count(Distinct AthleteId) from dbo.ActivityShortDB) AS "Athletes in activities",
    -- ativities count
    (Select count(*) from dbo.ActivityShortDB ) AS "Activities",
    -- ativities count
    (Select count(*) from dbo.ActivityShortDB Where ActivityType='6') AS "Ski"
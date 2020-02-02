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

Select count(*) from dbo.ActivityQueriesDB WHERE [DateFrom] <= CONVERT([datetime], '1990-01-01')
-- delete from dbo.ActivityQueriesDB WHERE [Status]=0 AND [DateFrom] <= CONVERT([datetime], '1990-01-01')
-- select * from dbo.AthleteShortDB where AthleteName like '%name%'
-- select * from dbo.ActivityShortDB WHERE AthleteId=1234 ORDER BY ActivityDate
-- select count(*) from dbo.ActivityShortDB WHERE AthleteId=1234
